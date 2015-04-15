// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="VisibleAttribute.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ActorMetadataAttributes
{
    using System;

    /// <summary>
    /// The visibility override attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class VisibleAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleAttribute"/> class.
        /// </summary>
        /// <param name="visible">
        /// A value indicating whether or not the target is server-visible.
        /// </param>
        public VisibleAttribute(bool visible)
        {
            this.Visible = visible;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the target is server-visible.
        /// </summary>
        public bool? Visible { get; set; }
    }
}
