// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventEntity.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Journal.AzureTable
{
    using System.Globalization;

    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// The event entity.
    /// </summary>
    public class EventEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventEntity"/> class.
        /// </summary>
        public EventEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventEntity"/> class.
        /// </summary>
        /// <param name="partition">
        /// The partition.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        public EventEntity(string partition, long id)
        {
            this.PartitionKey = partition;
            this.RowKey = id.ToString("X16");
        }

        /// <summary>
        /// Gets or sets the event.
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Gets the event id.
        /// </summary>
        [IgnoreProperty]
        public long Id
        {
            get
            {
                return long.Parse(this.RowKey, NumberStyles.HexNumber);
            }
        }
    }
}