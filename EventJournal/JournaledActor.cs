// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JournaledActor.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using EventJournal.Execution;
    using EventJournal.Journal;
    using EventJournal.Utilities;

    using Orleans;
    using Orleans.Runtime;

    /// <summary>
    /// The journal writer interface.
    /// </summary>
    /// <typeparam name="TActorInterface">
    /// The actor interface.
    /// </typeparam>
    /// <typeparam name="TActorState">
    /// The type of the actor state.
    /// </typeparam>
    public abstract class JournaledActor<TActorInterface, TActorState> : Grain<TActorState>, IGrain
        where TActorState : class, IGrainState, IJournaledState
        where TActorInterface : class, IGrain
    {
        /// <summary>
        /// The journal provider.
        /// </summary>
        private static readonly IJournalProvider JournalProvider = JournalProviderManager.Manager.GetProvider(typeof(TActorInterface));

        /// <summary>
        /// The journal provider.
        /// </summary>
        private static readonly Lazy<IEventInvoker<TActorInterface>> Invoker =
            new Lazy<IEventInvoker<TActorInterface>>(EventInvokerGenerator.Create<TActorInterface>);

        /// <summary>
        /// The actor instance.
        /// </summary>
        private readonly TActorInterface actor;

        /// <summary>
        /// Whether or not this instance is resetting.
        /// </summary>
        private bool resetting;

        /// <summary>
        /// Whether or not an event expression is currently being evaluated.
        /// </summary>
        private bool evaluatingEvent;

        /// <summary>
        /// The event journal.
        /// </summary>
        private IJournal journal;

        /// <summary>
        /// The event producer, which writes to the event journal.
        /// </summary>
        private IEventProducer<TActorInterface> eventProducer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JournaledActor{TActorInterface,TActorState}"/> class.
        /// </summary>
        protected JournaledActor()
        {
            this.actor = this as TActorInterface;
            if (this.actor == null)
            {
                throw new InvalidCastException(
                    string.Format(
                        "All sub-classes of {0} must implement {1} but {2} does not",
                        typeof(JournaledActor<TActorInterface, TActorState>),
                        typeof(TActorInterface),
                        this.GetType()));
            }
        }

        /// <summary>
        /// The validator delegate.
        /// </summary>
        protected delegate void Validator();

        /// <summary>
        /// The asynchronous validator delegate.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        protected delegate Task AsyncValidator();

        /// <summary>
        /// The apply delegate.
        /// </summary>
        protected delegate void Applier();

        /// <summary>
        /// The asynchronous apply delegate.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        protected delegate Task AsyncApplier();

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the journal is being replayed.
        /// </summary>
        public bool IsBeingReplayed { get; set; }

        /// <summary>
        /// Gets the next event id.
        /// </summary>
        private long NextEventId
        {
            get
            {
                return this.eventProducer != null ? this.eventProducer.NextEventId : 1;
            }
        }

        /// <summary>
        /// Activates this instance.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        public override async Task OnActivateAsync()
        {
            this.IsBeingReplayed = false;
            this.evaluatingEvent = false;
            this.resetting = false;

            this.Logger = this.GetLogger(string.Format("{0}/{1}", this.GetKind(), this.GetPrimaryKey().ToString("N")));
            await base.OnActivateAsync();

            // Create the journal.
            this.journal = await JournalProvider.Create(this);

            // Read & apply all unapplied events.
            await this.ReplayUnappliedEvents();

            // Create the event producer with the journal.
            this.eventProducer = EventProducerGenerator.Create<TActorInterface>(this.journal, this.State.LastAppliedEventId + 1);
        }

        /// <summary>
        /// Handles deactivation.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();

            // If this instance is resetting, we should not write a snapshot or unapplied events will not be applied.
            if (!this.resetting)
            {
                await this.WriteSnapshot();
            }
        }

        /// <summary>
        /// Returns a collection of events from the journal.
        /// </summary>
        /// <param name="after">
        /// The event id preceding the first event returned.
        /// </param>
        /// <param name="maxResults">
        /// The maximum number of results.
        /// </param>
        /// <returns>
        /// A collection of events from this actor's event journal.
        /// </returns>
        public async Task<IList<Event>> GetHistory(long after, int maxResults)
        {
            var results = new List<Event>();
            await this.journal.BatchReadFrom(
                after,
                batch =>
                {
                    foreach (var @event in batch)
                    {
                        var ev = @event;
                        var types = Invoker.Value.GetArgumentTypes(ev.Type);
                        if (types != null && types.Length > 0)
                        {
                            var args = new List<object>(ev.Arguments.Length);
                            for (var i = 0; i < ev.Arguments.Length; i++)
                            {
                                args.Add(ev.Arg(types[i], i));
                            }

                            ev.Arguments = args.ToArray();
                        }

                        results.Add(ev);
                    }

                    return Task.FromResult(0);
                },
                CancellationToken.None,
                maxResults);
            return results;
        }

        /// <summary>
        /// Returns the last event id in this actor's history.
        /// </summary>
        /// <returns>
        /// The last event id in this actor's history.
        /// </returns>
        public Task<long> GetLastEventId()
        {
            return Task.FromResult(this.NextEventId - 1);
        }

        /// <summary>Deletes this actor's journal.</summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        public virtual Task ClearJournal()
        {
            return this.journal.Clear(long.MaxValue);
        }

        /// <summary>
        /// Deletes this actor's state snapshot.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        public virtual Task ClearSnapshot()
        {
            return this.State.ClearStateAsync();
        }

        /// <summary>
        /// Writes and applies an event.
        /// </summary>
        /// <param name="validate">
        /// The validation method.
        /// </param>
        /// <param name="write">
        /// The event expression.
        /// </param>
        /// <param name="apply">
        /// The apply method.
        /// </param>
        /// <returns>
        /// The result of applying the event.
        /// </returns>
        protected Task Event(Validator validate, Func<TActorInterface, Task> write, Applier apply)
        {
            return this.Event(
                validate,
                write,
                () =>
                {
                    apply();
                    return Task.FromResult(0);
                });
        }

        /// <summary>
        /// Writes and applies an event.
        /// </summary>
        /// <param name="validate">
        /// The validation method.
        /// </param>
        /// <param name="write">
        /// The event expression.
        /// </param>
        /// <param name="apply">
        /// The apply method.
        /// </param>
        /// <typeparam name="T">
        /// The underlying return type of the expression.
        /// </typeparam>
        /// <returns>
        /// The result of applying the event.
        /// </returns>
        protected Task<T> Event<T>(Validator validate, Func<TActorInterface, Task<T>> write, Func<T> apply)
        {
            return this.Event(validate, write, () => Task.FromResult(apply()));
        }

        /// <summary>
        /// Writes and applies an event.
        /// </summary>
        /// <param name="validate">
        /// The validation method.
        /// </param>
        /// <param name="write">
        /// The event expression.
        /// </param>
        /// <param name="apply">
        /// The apply method.
        /// </param>
        /// <typeparam name="T">
        /// The underlying return type of the expression.
        /// </typeparam>
        /// <returns>
        /// The result of applying the event.
        /// </returns>
        protected async Task<T> Event<T>(Validator validate, Func<TActorInterface, Task<T>> write, Func<Task<T>> apply)
        {
            var valid = false;
            try
            {
                if (this.evaluatingEvent)
                {
                    throw new InvalidOperationException(
                        "An event expression is already being evaluated. Likely cause: the expression references 'this' instead of the interface argument.");
                }

                this.evaluatingEvent = true;

                var eventId = this.NextEventId;
                if (!this.IsBeingReplayed)
                {
                    validate();
                    valid = true;

                    // Execute the event emitter expression, passing in the emitter which will persist the event.
                    await write(this.eventProducer.Interface);
                    Debug.Assert(
                        this.eventProducer.NextEventId != eventId,
                        "Event was passed an expression which did not produce an event.");
                }

                var result = await apply();
                if (!this.IsBeingReplayed)
                {
                    this.State.LastAppliedEventId = eventId;
                }

                return result;
            }
            catch
            {
                if (valid || this.IsBeingReplayed)
                {
                    this.resetting = true;
                    this.DeactivateOnIdle();
                }

                throw;
            }
            finally
            {
                this.evaluatingEvent = false;
            }
        }

        /// <summary>
        /// Writes and applies an event.
        /// </summary>
        /// <param name="validate">
        /// The validation method.
        /// </param>
        /// <param name="write">
        /// The event expression.
        /// </param>
        /// <param name="apply">
        /// The apply method.
        /// </param>
        /// <returns>
        /// The result of applying the event.
        /// </returns>
        protected async Task Event(Validator validate, Func<TActorInterface, Task> write, AsyncApplier apply)
        {
            var valid = false;
            try
            {
                if (this.evaluatingEvent)
                {
                    throw new InvalidOperationException(
                        "An event expression is already being evaluated. Likely cause: the expression references 'this' instead of the interface argument.");
                }

                this.evaluatingEvent = true;

                var eventId = this.NextEventId;
                if (!this.IsBeingReplayed)
                {
                    validate();
                    valid = true;

                    // Execute the event emitter expression, passing in the emitter which will persist the event.
                    await write(this.eventProducer.Interface);
                    Debug.Assert(
                        this.eventProducer.NextEventId != eventId,
                        "Event was passed an expression which did not produce an event.");
                }

                await apply();

                if (!this.IsBeingReplayed)
                {
                    this.State.LastAppliedEventId = eventId;
                }
            }
            catch (Exception exception)
            {
                if (valid || this.IsBeingReplayed)
                {
                    this.resetting = true;
                    this.DeactivateOnIdle();
                }

                this.Logger.Error(-1, "Exception handling event.", exception);
                throw;
            }
            finally
            {
                this.evaluatingEvent = false;
            }
        }

        /// <summary>
        /// Snapshot the state of the grain.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        protected virtual async Task WriteSnapshot()
        {
            if (!this.resetting)
            {
                await this.State.WriteStateAsync();
            }
        }

        /// <summary>
        /// Replay all events since the last snapshot.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        protected async Task ReplayUnappliedEvents()
        {
            if (!this.IsBeingReplayed)
            {
                try
                {
                    var cancellation = new CancellationTokenSource();
                    this.IsBeingReplayed = true;
                    var currentEventId = this.State.LastAppliedEventId;
                    await this.journal.ReadFrom(
                        currentEventId,
                        async @event =>
                        {
                            try
                            {
                                // Apply the event.
                                await Invoker.Value.Invoke(this.actor, @event);

                                // Update internal state.
                                if (@event.Id > this.State.LastAppliedEventId)
                                {
                                    this.State.LastAppliedEventId = @event.Id;
                                }
                            }
                            catch
                            {
                                this.DeactivateOnIdle();
                                cancellation.Cancel();
                                throw;
                            }
                        },
                        cancellation.Token);
                }
                catch (Exception exception)
                {
                    this.resetting = true;
                    this.DeactivateOnIdle();
                    this.Logger.Error(-1, "Exception replaying events.", exception);
                    throw;
                }
                finally
                {
                    this.IsBeingReplayed = false;
                }
            }
            else
            {
                throw new InvalidOperationException("Already being replayed.");
            }
        }

        /// <summary>
        /// Deletes all state associated with this instance.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the work performed.</returns>
        protected virtual async Task ClearAllState()
        {
            var lastId = this.State.LastAppliedEventId;
            await this.State.ClearStateAsync();
            await this.journal.Clear(lastId + 1);
        }
    }
}