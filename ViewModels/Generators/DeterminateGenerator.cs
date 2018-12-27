using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;

namespace ViewModels.Generators
{
    public class DeterminateGenerator : CarGeneratorBase
    {
        private const int PERIOD_LEFT_BORDER = 10 * 1000;
        private const int PERIOD_RIGHT_BORDER = 3600 * 1000;
        private double _step;

        public DeterminateGenerator(double period)
        {
            CheckValue(period, PERIOD_LEFT_BORDER, PERIOD_RIGHT_BORDER, "период");
            Period = period;
            _step = period - 1;
        }

        public double Period { get; }

        public override List<Car> Generate(int difference)
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
