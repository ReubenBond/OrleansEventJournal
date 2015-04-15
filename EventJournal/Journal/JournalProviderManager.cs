// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JournalProviderManager.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Journal
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;

    using Microsoft.Azure;

    /// <summary>
    /// The journal provider manager.
    /// </summary>
    public class JournalProviderManager
    {
        /// <summary>
        /// The default provider name.
        /// </summary>
        private const string DefaultProviderName = "Default";

        /// <summary>
        /// The cache of grain journal providers.
        /// </summary>
        private readonly ConcurrentDictionary<Type, string> providerNames;

        /// <summary>
        /// The cache of journal providers.
        /// </summary>
        private readonly ConcurrentDictionary<Type, IJournalProvider> providers;

        /// <summary>
        /// Initializes static members of the <see cref="JournalProviderManager"/> class.
        /// </summary>
        static JournalProviderManager()
        {
            Manager = new JournalProviderManager();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JournalProviderManager"/> class.
        /// </summary>
        public JournalProviderManager()
        {
            this.providerNames = new ConcurrentDictionary<Type, string>();
            this.providers = new ConcurrentDictionary<Type, IJournalProvider>();
            this.GetSettingDelegate = CloudConfigurationManager.GetSetting;
            this.GetProviderDelegate = (_, __) =>
            {
                throw new InvalidOperationException("JournalProviderManager.GetProviderDelegate must be specified.");
            };
        }

        /// <summary>
        /// Gets or sets the manager.
        /// </summary>
        public static JournalProviderManager Manager { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to retrieve settings given a provider name.
        /// </summary>
        /// <remarks>The default value is <see cref="CloudConfigurationManager.GetSetting"/>.</remarks>
        public Func<string, string> GetSettingDelegate { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to get a provider given an actor type and a provider name.
        /// </summary>
        public Func<Type, string, IJournalProvider> GetProviderDelegate { get; set; }

        /// <summary>
        /// Returns the <see cref="IJournalProvider"/> for the provided <paramref name="grainType"/>.
        /// </summary>
        /// <param name="grainType">
        /// The grain type
        /// </param>
        /// <returns>
        /// The <see cref="IJournalProvider"/> for the provided <paramref name="grainType"/>.
        /// </returns>
        public IJournalProvider GetProvider(Type grainType)
        {
            IJournalProvider result;
            if (!this.providers.TryGetValue(grainType, out result))
            {
                var providerName = this.providerNames.GetOrAdd(grainType, GetJournalProviderForGrainType);
                result = this.GetProviderDelegate(grainType, providerName);
                result = this.providers.GetOrAdd(grainType, _ => result);
            }

            return result;
        }

        /// <summary>
        /// Returns the <see cref="IJournalProvider"/> for the provided <paramref name="grainType"/>.
        /// </summary>
        /// <param name="grainType">
        /// The grain type
        /// </param>
        /// <returns>
        /// The <see cref="IJournalProvider"/> for the provided <paramref name="grainType"/>.
        /// </returns>
        private static string GetJournalProviderForGrainType(Type grainType)
        {
            var attr = grainType.GetCustomAttribute<JournalProviderAttribute>();
            if (attr == null)
            {
                return DefaultProviderName;
            }

            return attr.ProviderName;
        }
    }
}