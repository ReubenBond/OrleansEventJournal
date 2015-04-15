// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="ProxyGenerationUtility.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Utilities
{
    using System;
    using System.Reflection;

    using ActorMetadataAttributes;

    /// <summary>
    /// Utilities for use during proxy generation.
    /// </summary>
    public static class ProxyGenerationUtility
    {
        /// <summary>
        /// Returns a value indicating whether or not a server-side dispatcher should be generated for the provided <paramref name="method"/>.
        /// </summary>
        /// <param name="containingType">
        /// The containing type.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// A value indicating whether or not a server-side dispatcher should be generated for the provided <paramref name="method"/>.
        /// </returns>
        public static bool IsVisible(Type containingType, MethodInfo method)
        {
            var typeLevelAttribute = method.DeclaringType.GetCustomAttribute<VisibleAttribute>()
                                     ?? containingType.GetCustomAttribute<VisibleAttribute>();
            var hasTypeOverride = typeLevelAttribute != null && typeLevelAttribute.Visible.HasValue;
            var typeVisibility = typeLevelAttribute != null && typeLevelAttribute.Visible.HasValue
                                 && typeLevelAttribute.Visible.Value;

            var methodLevelAttribute = method.GetCustomAttribute<VisibleAttribute>();
            var hasMethodOverride = methodLevelAttribute != null && methodLevelAttribute.Visible.HasValue;
            var methodVisibility = methodLevelAttribute != null && methodLevelAttribute.Visible.HasValue
                                   && methodLevelAttribute.Visible.Value;

            if (hasMethodOverride)
            {
                return methodVisibility;
            }

            if (hasTypeOverride)
            {
                return typeVisibility;
            }

            return true;
        }

        /// <summary>
        /// Returns the canonical symbol name for the provided <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">
        /// The symbol name.
        /// </param>
        /// <returns>
        /// The canonical symbol name for the provided <paramref name="symbol"/>.
        /// </returns>
        public static string ToCanonicalName(string symbol)
        {
            return symbol.Substring(0, 1).ToLowerInvariant() + symbol.Substring(1);
        }
    }
}
