// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="ActorMethodArgumentDescription.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Metadata
{
    /// <summary>
    /// The actor method argument description.
    /// </summary>
    public class ActorMethodArgumentDescription
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", this.Type, this.Name);
        }
    }
}