// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureTableJournalProvider.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal.AzureTable
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using EventJournal.Journal;
    using EventJournal.Utilities;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    using Newtonsoft.Json;

    using Orleans;

    /// <summary>
    /// The Azure table journal provider.
    /// </summary>
    public class AzureTableJournalProvider : IJournalProvider
    {
        /// <summary>
        /// The Azure table name suffix.
        /// </summary>
        public const string TableNameSuffix = "Journal";

        /// <summary>
        /// The clients.
        /// </summary>
        private static readonly ConcurrentDictionary<string, CloudTableClient> Clients =
            new ConcurrentDictionary<string, CloudTableClient>();
        
        /// <summary>
        /// The clients.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Task<CloudTable>> Tables = new ConcurrentDictionary<string, Task<CloudTable>>();

        /// <summary>
        /// The client.
        /// </summary>
        private readonly CloudTableClient client;

        /// <summary>
        /// The json settings.
        /// </summary>
        private readonly JsonSerializerSettings jsonSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableJournalProvider"/> class. 
        /// </summary>
        /// <param name="connectionString">
        /// The connection string.
        /// </param>
        /// <param name="jsonSettings">
        /// The JSON serializer settings.
        /// </param>
        public AzureTableJournalProvider(string connectionString, JsonSerializerSettings jsonSettings)
        {
            this.jsonSettings = jsonSettings;
            this.client = Clients.GetOrAdd(
                connectionString, 
                connString =>
                {
                    var account = CloudStorageAccount.Parse(connString);
                    return account.CreateCloudTableClient();
                });
        }

        /// <summary>
        /// Returns a new <see cref="IJournal"/>, initialized with the provided actor.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// A new <see cref="IJournal"/>, initialized with the provided actor.
        /// </returns>
        public async Task<IJournal> Create(IGrain actor)
        {
            var table = Tables.GetOrAdd(
                actor.GetKind(), 
                async _ =>
                {
                    var t = this.client.GetTableReference(actor.GetKind() + TableNameSuffix);
                    await t.CreateIfNotExistsAsync();
                    return t;
                });

            return new AzureTableJournal(actor.GetIdString(), await table, this.jsonSettings);
        }
    }
}