using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using static Infrastructure.Constants;

namespace ViewModels.Generators
{
    public class NormalGenerator : CarGeneratorBase
    {
        private const int M_LEFT_BORDER = 10 * 1000;
        private const int M_RIGHT_BORDER = 3600 * 1000;

        private const int D_LEFT_BORDER = 1000;
        private const int D_RIGHT_BORDER = 600 * 1000;

        private double _difference;

        public NormalGenerator(double m, double d)
        {
            CheckValue(m, M_LEFT_BORDER, M_RIGHT_BORDER, "M");
            CheckValue(d, D_LEFT_BORDER, D_RIGHT_BORDER, "D");
            M = m;
            D = d;
            _difference = 0;
        }

        public double M { get; }
        public double D { get; }

        public override List<Car> Generate(int difference)
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
                _difference = Math.Max(0, _difference);
                _difference += tmp;
            }
            return result;
        }
    }
}
