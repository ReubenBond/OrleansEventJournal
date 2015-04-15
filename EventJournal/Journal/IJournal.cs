// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IJournal.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Journal
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes an event journal provider.
    /// </summary>
    public interface IJournal
    {
        /// <summary>
        /// Append the provided <paramref name="event"/> to the journal.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        Task Append(Event @event);

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
        Task ReadFrom(long id, Func<Event, Task> onNext, CancellationToken cancellationToken);

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
        /// The number of results to read
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        Task BatchReadFrom(long id, Func<IEnumerable<Event>, Task> onNext, CancellationToken cancellationToken, int maxResults);

        /// <summary>
        /// Clears the journal up to and including, the provided <paramref name="id"/>.
        /// </summary>
        /// <param name="id">
        /// The id to clear up to.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        Task Clear(long id);
    }
}
