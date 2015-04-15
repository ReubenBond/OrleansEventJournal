// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="IEventDispatcher.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Execution
{
    using System.Threading.Tasks;

    /// <summary>
    /// Dispatches an event directed at an actor. 
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Dispatches <paramref name="event"/>, returning the response.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        Task<object> Dispatch(Event @event);
    }
}