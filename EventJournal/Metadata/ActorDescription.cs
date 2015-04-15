// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="ActorDescription.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Metadata
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The actor description.
    /// </summary>
    public class ActorDescription
    {
        /// <summary>
        /// Gets or sets the kind.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this actor is a singleton.
        /// </summary>
        public bool IsSingleton { get; set; }

        /// <summary>
        /// Gets or sets the methods.
        /// </summary>
        public Dictionary<string, ActorMethodDescription> Methods { get; set; }

        /// <summary>
        /// Gets or sets the actor type.
        /// </summary>
        [JsonIgnore]
        public Type Type { get; set; }
    }
}