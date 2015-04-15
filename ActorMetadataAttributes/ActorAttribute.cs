// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorAttribute.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ActorMetadataAttributes
{
    using System;
    using System.Globalization;

    /// <summary>
    ///     The actor attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ActorAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActorAttribute"/> class.
        /// </summary>
        public ActorAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorAttribute"/> class.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        public ActorAttribute(string kind)
        {
            if (kind != null)
            {
                this.Kind = kind.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorAttribute"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        public ActorAttribute(Type type)
        {
            const string ActorSuffix = "actor";
            const string GrainSuffix = "grain";
            var typeName = type.Name.ToLowerInvariant();
            if (type.IsInterface)
            {
                if (typeName.StartsWith("i", true, CultureInfo.InvariantCulture))
                {
                    typeName = typeName.Substring(1);
                }
            }

            if (typeName.EndsWith(ActorSuffix, true, CultureInfo.InvariantCulture))
            {
                this.Kind = typeName.Substring(0, typeName.Length - ActorSuffix.Length);
            }
            else if (typeName.EndsWith(GrainSuffix, true, CultureInfo.InvariantCulture))
            {
                this.Kind = typeName.Substring(0, typeName.Length - GrainSuffix.Length);
            }
            else
            {
                this.Kind = typeName;
            }
        }

        /// <summary>
        ///     Gets the kind.
        /// </summary>
        public string Kind { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this actor is a singleton.
        /// </summary>
        public bool IsSingleton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this actor is an abstract base class and is not directly addressable.
        /// </summary>
        public bool IsAbstract { get; set; }
    }
}