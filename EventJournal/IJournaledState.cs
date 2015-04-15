// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IJournaledState.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal
{
    /// <summary>
    /// The journal state interface.
    /// </summary>
    public interface IJournaledState
    {
        /// <summary>
        /// Gets or sets the id of the most recent event.
        /// </summary>
        long LastAppliedEventId { get; set; }
    }
}