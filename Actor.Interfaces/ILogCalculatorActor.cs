namespace Actor.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using EventJournal;

    using Orleans;

    public interface ILogCalcActor : IGrainWithGuidKey
    {
        Task<decimal> Set(decimal number);

        Task<decimal> Add(decimal number);

        Task<decimal> Multiply(decimal number);

        Task<decimal> Divide(decimal number);

        Task<decimal> Reset();

        Task<decimal> Get();

        /// <summary>
        /// Returns all events in the journal.
        /// </summary>
        /// <returns>
        /// All events in this actor's event journal.
        /// </returns>
        Task<IList<Event>> GetHistory();
    }
}
