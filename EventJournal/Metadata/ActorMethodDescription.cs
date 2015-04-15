// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="ActorMethodDescription.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Metadata
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;

    /// <summary>
    /// The actor method description.
    /// </summary>
    public class ActorMethodDescription
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the return type.
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the args.
        /// </summary>
        public List<ActorMethodArgumentDescription> Args { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this method is externally visible.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Gets or sets the method metadata.
        /// </summary>
        [JsonIgnore]
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            var returnType = string.IsNullOrWhiteSpace(this.ReturnType) ? "void" : this.ReturnType;
            var args = string.Join(", ", this.Args ?? Enumerable.Empty<ActorMethodArgumentDescription>());
            return string.Format("{0} {1}({2})", returnType, this.Name, args);
        }
    }
}