// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JournalProviderAttributeMissingException.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal
{
    using System;

    /// <summary>
    /// The journal provider attribute missing exception.
    /// </summary>
    [Serializable]
    public class JournalProviderAttributeMissingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JournalProviderAttributeMissingException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public JournalProviderAttributeMissingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Returns a new instance of the <see cref="JournalProviderAttributeMissingException"/> class.
        /// </summary>
        /// <param name="type">
        /// The type which is missing the attribute.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="JournalProviderAttributeMissingException"/> class.
        /// </returns>
        public static JournalProviderAttributeMissingException Create(Type type)
        {
            return new JournalProviderAttributeMissingException("No JournalProvider attribute is present on type " + type.FullName + ".");
        }
    }
}