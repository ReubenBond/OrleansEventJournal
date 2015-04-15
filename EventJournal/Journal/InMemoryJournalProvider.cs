// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryJournalProvider.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Journal
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using EventJournal.Utilities;

    using Orleans;

    /// <summary>
    /// The in-memory journal provider.
    /// </summary>
    public class InMemoryJournalProvider : IJournalProvider
    {
        /// <summary>
        /// The journal.
        /// </summary>
        private static readonly ConcurrentDictionary<string, InMemoryJournal> Journal =
            new ConcurrentDictionary<string, InMemoryJournal>();

        /// <summary>
        /// Reset all state.
        /// </summary>
        public void Reset()
        {
            foreach (var journal in Journal.Values)
            {
                journal.Clear(long.MaxValue);
            }

            Journal.Clear();
        }

        /// <summary>
        /// Returns a new <see cref="IJournal"/>, initialized with the provided <paramref name="actor"/>.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// A new <see cref="IJournal"/>, initialized for the provided <paramref name="actor"/>.
        /// </returns>
        public Task<IJournal> Create(IGrain actor)
        {
            var id = actor.GetIdString();
            var journal = Journal.GetOrAdd(id, _ => new InMemoryJournal());
            return Task.FromResult<IJournal>(journal);
        }
    }
}