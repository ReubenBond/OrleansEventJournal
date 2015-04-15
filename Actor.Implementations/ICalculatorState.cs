namespace Actor.Implementations
{
    using EventJournal;

    using Orleans;

    /// <summary>
    /// The CalculatorState interface.
    /// </summary>
    public interface ICalculatorState : IGrainState, IJournaledState
    {
        /// <summary>
        /// Gets or sets the custom value.
        /// </summary>
        decimal Value { get; set; }
    }
}