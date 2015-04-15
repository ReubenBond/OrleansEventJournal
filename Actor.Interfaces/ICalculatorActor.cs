namespace Actor.Interfaces
{
    using System.Threading.Tasks;

    using Orleans;

    public interface ICalculatorActor : IGrainWithGuidKey
    {
        Task<decimal> Set(decimal number);

        Task<decimal> Add(decimal number);

        Task<decimal> Multiply(decimal number);

        Task<decimal> Divide(decimal number);

        Task<decimal> Reset();

        Task<decimal> Get();
    }
}
