// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEventProducer.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Execution
{
    using System.Threading.Tasks;

    using EventJournal.Journal;

    /// <summary>
    /// The EventProducer interface.
    /// </summary>
    /// <typeparam name="TInterface">
    /// The interface which this producer produces events for.
    /// </typeparam>
    public interface IEventProducer<out TInterface>
    {
        /// <summary>
        /// Gets or sets the journal.
        /// </summary>
        IJournal Journal { get; set; }

        /// <summary>
        /// Gets or sets the id of the next event to be written.
        /// </summary>
        long NextEventId { get; set; }

        /// <summary>
        /// Gets the event producer interface implementation.
        /// </summary>
        TInterface Interface { get; }

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
        Task WriteEvent(string type, params object[] args);
    }
}