// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryJournal.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using EventJournal.Utilities;

    using Orleans.Serialization;

    /// <summary>
    /// The in-memory journal.
    /// </summary>
    public class InMemoryJournal : IJournal, IEnumerable<Event>
    {
        /// <summary>
        /// The events.
        /// </summary>
        private readonly Dictionary<long, Event> events;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryJournal"/> class.
        /// </summary>
        public InMemoryJournal()
        {
            this.events = new Dictionary<long, Event>();
        }

        /// <summary>
        /// Gets the number of events.
        /// </summary>
        public int Count
        {
            get
            {
                return this.events.Count;
            }
        }

        /// <summary>
        /// Append the provided <paramref name="event"/> to the journal.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task Append(Event @event)
        {
            this.events.Add(@event.Id, (Event)SerializationManager.DeepCopy(@event));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Reads the journal, beginning with the event with the lowest identifier greater than <paramref name="id"/>,
        /// calling <paramref name="onNext"/> for each item.
        /// </summary>
        /// <param name="id">
        /// The event id to begin enumeration at. Enumeration will begin at the first event with an identifier
        ///     greater than <paramref name="id"/>.
        /// </param>
        /// <param name="onNext">
        /// The callback to handle each item in the journal.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public async Task ReadFrom(long id, Func<Event, Task> onNext, CancellationToken cancellationToken)
        {
            foreach (var ev in this.events.Values.Where(_ => _.Id > id).OrderBy(_ => _.Id))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Cancelled by cancellation token.", cancellationToken);
                }

                await onNext(ev);
            }
        }

        /// <summary>
        /// Reads the journal, beginning with the event with the lowest identifier greater than <paramref name="id"/>,
        /// calling <paramref name="onNext"/> for each item.
        /// </summary>
        /// <param name="id">
        /// The event id to begin enumeration at. Enumeration will begin at the first event with an identifier
        ///     greater than <paramref name="id"/>.
        /// </param>
        /// <param name="onNext">
        /// The callback to handle each batch of items in the journal.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to signal cancellation.
        /// </param>
        /// <param name="maxResults">
        /// The number of results to read.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public async Task BatchReadFrom(long id, Func<IEnumerable<Event>, Task> onNext, CancellationToken cancellationToken, int maxResults = 100)
        {
            foreach (var ev in this.events.Values.Where(_ => _.Id > id).OrderBy(_ => _.Id).Take(maxResults).Batch(100))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Cancelled by cancellation token.", cancellationToken);
                }

                await onNext(ev);
            }
        }

        /// <summary>
        /// Clears the journal up to and including, the provided <paramref name="id"/>.
        /// </summary>
        /// <param name="id">
        /// The id to clear up to.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task Clear(long id)
        {
            foreach (var key in this.events.Keys.Where(_ => _ <= id).ToList())
            {
                this.events.Remove(key);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Event> GetEnumerator()
        {
            return this.events.Values.OrderBy(_ => _.Id).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}