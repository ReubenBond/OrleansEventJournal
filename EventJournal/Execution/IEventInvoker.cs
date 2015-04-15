// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEventInvoker.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Execution
{
    using System;
    using System.Threading.Tasks;

    using Orleans;

    /// <summary>
    /// Invoke events on a given actor.
    /// </summary>
    /// <typeparam name="TActor">
    /// The actor type.
    /// </typeparam>
    public interface IEventInvoker<in TActor> where TActor : IGrain
    {
        /// <summary>
        /// Invoke the provided event to the provided actor.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <param name="ev">
        /// The event.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        Task Invoke(TActor actor, Event ev);

        /// <summary>
        /// Returns the ordered arguments types for the provided event type.
        /// </summary>
        /// <param name="type">
        /// The event type.
        /// </param>
        /// <returns>
        /// The ordered arguments types for the provided event type.
        /// </returns>
        Type[] GetArgumentTypes(string type);
    }
}