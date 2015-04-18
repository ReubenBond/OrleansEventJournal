

namespace Actor.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using Actor.Interfaces;

    using EventJournal;

    using Orleans.Providers;

    /// <summary>
    /// The calc actor.
    /// </summary>
    [StorageProvider]
    public class LogCalcActor : JournaledActor<ILogCalcActor, ICalculatorState>, ILogCalcActor
    {
        public Task<decimal> Set(decimal number)
        {
            return this.Event(() => { }, _ => _.Set(number), () => this.State.Value = number);
        }

        public Task<decimal> Add(decimal number)
        {
            return this.Event(() => { }, _ => _.Add(number), () => this.State.Value += number);
        }

        public Task<decimal> Multiply(decimal number)
        {
            return this.Event(() => { }, _ => _.Multiply(number), () => this.State.Value *= number);
        }

        /// <summary>
        /// The divide.
        /// </summary>
        /// <param name="number">
        /// The number.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="DivideByZeroException">
        /// <paramref name="number"/> was zero.
        /// </exception>
        public Task<decimal> Divide(decimal number)
        {
            return this.Event(
                validate: () =>
                {
                    if (number == 0)
                    {
                        throw new DivideByZeroException();
                    }
                },
                write: _ => _.Divide(number),
                apply: () => this.State.Value /= number);
        }

        public Task<decimal> Reset()
        {
            return this.Event(() => { }, _ => _.Reset(), () => this.State.Value = 0);
        }

        public Task<decimal> Get()
        {
            return Task.FromResult(this.State.Value);
        }

        public Task<IList<Event>> GetHistory()
        {
            return base.GetHistory(0, int.MaxValue);
        }
    }
}
