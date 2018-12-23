using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using static Infrastructure.Constants;

namespace ViewModels.Generators
{
    public class NormalGenerator : ICarGenerator
    {
        private double _difference;

        public NormalGenerator(double m, double d)
        {
            M = m;
            D = d;
            _difference = 0;
        }

        public double M { get; }
        public double D { get; }

        public List<Car> Generate(int difference)
        {
            _difference -= difference;
            List<Car> result = new List<Car>();
            if (_difference < 0)
            {
                result.Add(new Car());
                var tmp = difference;
                _difference = 0;
                for (int i = 0; i < 12; i++)
                    _difference += RND.NextDouble();
                _difference -= 6;
                _difference = _difference * D + M;
                _difference += tmp;
            }
            return result;
        }
    }
}
