// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JournalProviderConfigurationNotFoundException.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal
{
    using System;

    /// <summary>
    /// The journal provider not found exception.
    /// </summary>
    [Serializable]
    public class JournalProviderConfigurationNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JournalProviderConfigurationNotFoundException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public JournalProviderConfigurationNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Returns a new instance of the <see cref="JournalProviderConfigurationNotFoundException"/> class.
        /// </summary>
        /// <param name="providerName">
        /// The provider name.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="JournalProviderConfigurationNotFoundException"/> class.
        /// </returns>
        public static JournalProviderConfigurationNotFoundException Create(string providerName)
        {
            return new JournalProviderConfigurationNotFoundException(string.Format("Journal provider \"{0}\" was not found.", providerName));
        }
    }
}