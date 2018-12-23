using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using static Infrastructure.Constants;
using static System.Math;

namespace ViewModels.Generators
{
    public class ExponentialGenerator : ICarGenerator
    {
        private double _difference;

        public ExponentialGenerator(double lambda)
        {
            Lambda = lambda;
            _difference = 0;
        }

        public double Lambda { get; }

        public List<Car> Generate(int difference)
        {
            _difference -= difference;
            List<Car> result = new List<Car>();
            if (_difference < 0)
            {
                result.Add(new Car());
                var tmp = difference;
                _difference = -Log(RND.NextDouble()) / Lambda;
                _difference += tmp;
            }
            return result;
        }
    }
}
