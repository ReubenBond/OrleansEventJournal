using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actor.Implementations
{
    using Actor.Interfaces;

    using Orleans;

    public class CalculatorActor : Grain, ICalculatorActor
    {
        private decimal val;
        public Task<decimal> Set(decimal number)
        {
            this.val = number;
            return this.Get();
        }

        public Task<decimal> Add(decimal number)
        {
            this.val += number;
            return this.Get();
        }

        public Task<decimal> Multiply(decimal number)
        {
            this.val *= number;
            return this.Get();
        }

        public Task<decimal> Divide(decimal number)
        {
            this.val /= number;
            return this.Get();
        }

        public Task<decimal> Reset()
        {
            this.val = 0;
            return this.Get();
        }

        public Task<decimal> Get()
        {
            return Task.FromResult(this.val);
        }
    }
}
