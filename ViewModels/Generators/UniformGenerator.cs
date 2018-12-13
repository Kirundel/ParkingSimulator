using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;

namespace ViewModels.Generators
{
    class UniformGenerator: ICarGenerator
    {
        private int _step;

        public UniformGenerator(int period)
        {
            Period = period;
            _step = 0;
        }

        public int Period { get; }

        public List<Car> Generate(int difference)
        {
            _step += difference;
            int count = _step / Period;
            _step %= Period;
            List<Car> result = new List<Car>();
            for (int i = 0; i < count; i++)
                result.Add(new Car());
            return result;
        }
    }
}
