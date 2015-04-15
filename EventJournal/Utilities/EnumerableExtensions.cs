// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableExtensions.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Utilities
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The enumerable extensions.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Batches the provided collection.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="size">
        /// The batch size.
        /// </param>
        /// <typeparam name="TSource">
        /// The underlying source type.
        /// </typeparam>
        /// <returns>
        /// The collection of batches.
        /// </returns>
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(
                     this IEnumerable<TSource> source, int size)
        {
            List<TSource> bucket = null;
            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new List<TSource>();
                }

                bucket.Add(item);
                if (bucket.Count == size)
                {
                    yield return bucket;
                    bucket = null;
                }
            }

            if (bucket != null && bucket.Count > 0)
            {
                yield return bucket;
            }
        }
    }
}
