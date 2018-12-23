using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;

namespace ViewModels.Generators
{
    public class DeterminateGenerator : ICarGenerator
    {
        private double _step;

        public DeterminateGenerator(double period)
        {
            Period = period;
            _step = period - 1;
        }

        public double Period { get; }

        public List<Car> Generate(int difference)
        {
            _step += difference;
            int count = _step >= Period ? 1 : 0;
            _step %= Period;
            List<Car> result = new List<Car>();
            for (int i = 0; i < count; i++)
                result.Add(new Car());
            return result;
        }
    }
}
