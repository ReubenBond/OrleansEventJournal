// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventProducerBase.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Execution
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using EventJournal.Journal;

    using Orleans.Runtime;
    
    /// <summary>
    /// The event producer base.
    /// </summary>
    /// <typeparam name="TInterface">
    /// The interface for which events are being emitted.
    /// </typeparam>
    public abstract class EventProducerBase<TInterface> : IEventProducer<TInterface>
        where TInterface : class
    {
        /// <summary>
        /// The interface implementation.
        /// </summary>
        private readonly TInterface @interface;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventProducerBase{TInterface}"/> class.
        /// </summary>
        /// <exception cref="InvalidCastException">
        /// The subtype does not implement <typeparamref name="TInterface"/>.
        /// </exception>
        protected EventProducerBase()
        {
            this.@interface = this as TInterface;
            if (this.@interface == null)
            {
                throw new InvalidCastException(
                    string.Format(
                        "All sub-classes of {0} must implement {1} but {2} does not", 
                        typeof(EventProducerBase<TInterface>), 
                        typeof(TInterface), 
                        this.GetType()));
            }
        }

        /// <summary>
        /// Gets or sets the journal.
        /// </summary>
        public IJournal Journal { get; set; }

        /// <summary>
        /// Gets or sets the next event id.
        /// </summary>
        public long NextEventId { get; set; }

        /// <summary>
        /// Gets the event producer interface implementation.
        /// </summary>
        public TInterface Interface
        {
            get
            {
                return this.@interface;
            }
        }

        /// <summary>
        /// Appends the specified event to the journal.
        /// </summary>
        /// <param name="type">
        /// The event type.
        /// </param>
        /// <param name="args">
        /// The event arguments.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public async Task WriteEvent(string type, params object[] args)
        {
            var ev = new Event
            {
                Id = this.NextEventId, 
                Type = type, 
                Arguments = args, 
                Time = DateTime.UtcNow, 
                CorrelationId = Trace.CorrelationManager.ActivityId,
                UserId = RequestContext.Get("uid") as Guid?
            };

            await this.Journal.Append(ev);
            this.NextEventId = ev.Id + 1;
        }
    }
}