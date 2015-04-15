// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureTableJournal.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using EventJournal.Journal;
    using EventJournal.Utilities;

    using Microsoft.WindowsAzure.Storage.Table;

    using Newtonsoft.Json;

    /// <summary>
    /// The Azure Table Service journal provider.
    /// </summary>
    public class AzureTableJournal : IJournal
    {
        /// <summary>
        ///     The event store.
        /// </summary>
        private readonly CloudTable store;

        /// <summary>
        ///     The partition id.
        /// </summary>
        private readonly string partitionKey;

        /// <summary>
        /// The JSON serializer settings.
        /// </summary>
        private readonly JsonSerializerSettings jsonSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableJournal"/> class.
        /// </summary>
        /// <param name="partitionKey">
        ///     The partition key.
        /// </param>
        /// <param name="store">
        ///     The event store.
        /// </param>
        /// <param name="jsonSettings">
        /// The JSON serializer settings.
        /// </param>
        public AzureTableJournal(string partitionKey, CloudTable store, JsonSerializerSettings jsonSettings)
        {
            this.jsonSettings = jsonSettings;
            this.partitionKey = partitionKey;
            this.store = store;
        }

        /// <summary>
        /// Append the provided <paramref name="event"/> to the journal.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task Append(Event @event)
        {
            var tableEvent = new EventEntity(this.partitionKey, @event.Id)
            {
                Event = JsonConvert.SerializeObject(@event, this.jsonSettings)
            };

            return this.store.ExecuteAsync(TableOperation.Insert(tableEvent));
        }

        /// <summary>
        /// Reads the journal, beginning with the event with the lowest identifier greater than <paramref name="id"/>,
        /// calling <paramref name="onNext"/> for each item.
        /// </summary>
        /// <param name="id">
        /// The event id to begin enumeration at. Enumeration will begin at the first event with an identifier
        ///     greater than <paramref name="id"/>.
        /// </param>
        /// <param name="onNext">
        /// The callback to handle each item in the journal.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task ReadFrom(long id, Func<Event, Task> onNext, CancellationToken cancellationToken)
        {
            var query = GreaterThan(this.partitionKey, id);
            return
                this.store.ForEach(
                    query, 
                    entity =>
                    onNext(JsonConvert.DeserializeObject<Event>(entity.Event, this.jsonSettings)), 
                    cancellationToken);
        }

        /// <summary>
        /// Reads the journal, beginning with the event with the lowest identifier greater than <paramref name="id"/>,
        /// calling <paramref name="onNext"/> for each item.
        /// </summary>
        /// <param name="id">
        /// The event id to begin enumeration at. Enumeration will begin at the first event with an identifier
        ///     greater than <paramref name="id"/>.
        /// </param>
        /// <param name="eventResults">
        /// The event Results.
        /// </param>
        /// <param name="onNext">
        /// The callback to handle each batch of items in the journal.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to signal cancellation.
        /// </param>
        /// <param name="maxResults">
        /// The number of results to read
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task BatchReadFrom(long id, Func<IEnumerable<Event>, Task> onNext, CancellationToken cancellationToken, int maxResults = -1)
        {
            var query = GreaterThan(this.partitionKey, id);
            if (maxResults > 0) {
                query = query.Take(maxResults);
            }
            return this.store.ForEachBatch(
                query, 
                entity =>
                onNext(
                    entity.Select(
                        value => JsonConvert.DeserializeObject<Event>(value.Event, this.jsonSettings))), 
                cancellationToken,
                maxResults);
        }

        /// <summary>
        /// Clears the journal up to and including, the provided <paramref name="id"/>.
        /// </summary>
        /// <param name="id">
        /// The id to clear up to.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public async Task Clear(long id)
        {
            // Azure Table Service supports batches of 100, so we chop the results up and batch the deletions.
            await this.BatchReadFrom(
                id, 
                async entities =>
                {
                    foreach (var batch in entities.Batch(100))
                    {
                        var deletion = new TableBatchOperation();
                        foreach (var entity in batch)
                        {
                            deletion.Add(TableOperation.Delete(new EventEntity(this.partitionKey, entity.Id)));
                        }

                        await this.store.ExecuteBatchAsync(deletion);
                    }
                }, 
                CancellationToken.None);
        }

        /// <summary>
        /// Returns the canonical <see cref="string"/> for the provided <paramref name="id"/>.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The canonical <see cref="string"/> for the provided <paramref name="id"/>.
        /// </returns>
        private static string GetIdString(long id)
        {
            return id.ToString("X16");
        }

        /// <summary>
        /// Returns a query for all entities with an id greater than to the provided <paramref name="id"/>.
        /// </summary>
        /// <param name="partition">
        /// The partition key.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// A query for all entities with an id greater than to the provided <paramref name="id"/>.
        /// </returns>
        private static TableQuery<EventEntity> GreaterThan(string partition, long id)
        {
            return
                new TableQuery<EventEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition), 
                        TableOperators.And, 
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, GetIdString(id))));
        }
    }
}