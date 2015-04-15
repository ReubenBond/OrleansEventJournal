// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IJournalProvider.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal
{
    using System.Threading.Tasks;

    using Orleans;

    /// <summary>
    /// Interface for <see cref="IJournal"/> factories.
    /// </summary>
    public interface IJournalProvider
    {
        /// <summary>
        /// Returns a new <see cref="IJournal"/>, initialized with the provided <paramref name="actor"/>.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// A new <see cref="IJournal"/>, initialized for the provided <paramref name="actor"/>.
        /// </returns>
        Task<IJournal> Create(IGrain actor);
    }
}