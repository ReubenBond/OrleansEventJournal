// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorExtensions.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Utilities
{
    using System;
    using System.Globalization;
    using System.Reflection;

    using ActorMetadataAttributes;

    using Orleans;

    /// <summary>
    ///     The actor extensions.
    /// </summary>
    public static class ActorExtensions
    {
        /// <summary>
        /// The get kind.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetKind(this IGrain actor)
        {
            return GetKind(actor.GetType());
        }

        /// <summary>
        /// The get kind.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetKind(Type type)
        {
            var attr = (ActorAttribute)type.GetCustomAttribute(typeof(ActorAttribute)) ?? new ActorAttribute(type);
            return attr.Kind;
        }

        /// <summary>
        /// Returns the id of the actor as a <see cref="string"/>.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The id of the actor as a <see cref="string"/>.
        /// </returns>
        public static string GetIdString(this IGrain actor)
        {
            var guidKey = actor as IGrainWithGuidKey;
            if (guidKey != null)
            {
                var typedActor = guidKey;
                return typedActor.GetPrimaryKey().ToString("N", CultureInfo.InvariantCulture);
            }

            var stringKey = actor as IGrainWithStringKey;
            if (stringKey != null)
            {
                var typedActor = stringKey;
                return typedActor.GetPrimaryKeyString();
            }

            var intKey = actor as IGrainWithIntegerKey;
            if (intKey != null)
            {
                var typedActor = intKey;
                return typedActor.GetPrimaryKeyLong().ToString("X16", CultureInfo.InvariantCulture);
            }

            throw new NotSupportedException("Actor key type not supported");
        }
    }
}