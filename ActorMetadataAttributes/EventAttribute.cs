// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="EventAttribute.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ActorMetadataAttributes
{
    using System;

    /// <summary>
    /// The event attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EventAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventAttribute"/> class.
        /// </summary>
        /// <param name="type">
        /// The event.
        /// </param>
        public EventAttribute(string type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets the event.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public int Version { get; set; }
    }
}