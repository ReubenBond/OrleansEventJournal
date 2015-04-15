// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureTableServiceExtensions.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Journal.AzureTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Extension methods for the Azure Table Service.
    /// </summary>
    internal static class AzureTableServiceExtensions
    {
        /// <summary>
        /// Executes the provided <paramref name="query"/> against he provided <paramref name="table"/>, invoking
        /// <paramref name="onNext"/> on each entity.
        /// </summary>
        /// <param name="table">
        /// The table to execute the provided <paramref name="query"/> on.
        /// </param>
        /// <param name="query">
        /// The query to execute.
        /// </param>
        /// <param name="onNext">
        /// The method to execute on each entity.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <typeparam name="TEntity">
        /// The table entity type.
        /// </typeparam>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public static async Task ForEach<TEntity>(
            this CloudTable table,
            TableQuery<TEntity> query,
            Func<TEntity, Task> onNext,
            CancellationToken cancellationToken) where TEntity : ITableEntity, new()
        {
            var segment = default(TableQuerySegment<TEntity>);
            do
            {
                var continuationToken = segment == null ? null : segment.ContinuationToken;
                segment = await table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);
                foreach (var result in segment.Results)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("Cancellation requested.", cancellationToken);
                    }

                    await onNext(result);
                }
            }
            while (segment.ContinuationToken != null);
        }

        /// <summary>
        /// Executes the provided <paramref name="query"/> against he provided <paramref name="table"/>, invoking
        /// <paramref name="onNext"/> on each entity.
        /// </summary>
        /// <param name="table">
        /// The table to execute the provided <paramref name="query"/> on.
        /// </param>
        /// <param name="query">
        /// The query to execute.
        /// </param>
        /// <param name="onNext">
        /// The method to execute on each entity.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <param name="maxResults">
        /// The maximum number of results to return. -1 will return all results.
        /// </param>
        /// <typeparam name="TEntity">
        /// The table entity type.
        /// </typeparam>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public static async Task ForEachBatch<TEntity>(
            this CloudTable table,
            TableQuery<TEntity> query,
            Func<IEnumerable<TEntity>, Task> onNext,
            CancellationToken cancellationToken,
            int maxResults = -1) where TEntity : ITableEntity, new()
        {
            var segment = default(TableQuerySegment<TEntity>);
            var remainingResults = maxResults;
            var takeAll = maxResults < 0;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Cancellation requested.", cancellationToken);
                }

                var continuationToken = segment == null ? null : segment.ContinuationToken;
                segment = await table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);
                await onNext(segment.Results);

                remainingResults -= segment.Results.Count;
                if (!takeAll && remainingResults <= 0)
                {
                    break;
                }
            }
            while (segment.ContinuationToken != null);
        }
    }
}