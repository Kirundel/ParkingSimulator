using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using static Infrastructure.Constants;

namespace ViewModels.Generators
{
    public class UniformGenerator : CarGeneratorBase
    {
        private const int PERIOD_LEFT_BORDER = 10 * 1000;
        private const int PERIOD_RIGHT_BORDER = 3600 * 1000;

        private double _difference;

        public UniformGenerator(double left, double right)
        {
            CheckValue(left, PERIOD_LEFT_BORDER, PERIOD_RIGHT_BORDER, "нижняя граница интервала");
            CheckValue(right, PERIOD_LEFT_BORDER, PERIOD_RIGHT_BORDER, "верхняя граница интервала");
            if (right < left)
                throw new IncorrectGeneratorParameterException("", "", IncorrectGeneratorParameterExceptionType.MIN_MORE_MAX);
            Left = left;
            Right = right;
            _difference = 0;
        }

        public double Left { get; }
        public double Right { get; }

        public override List<Car> Generate(int difference)
        {
            _difference -= difference;
            List<Car> result = new List<Car>();
            if (_difference < 0)
            {
                result.Add(new Car());
                var tmp = difference;
                _difference = RND.NextDouble() * (Right - Left) + Left;
                _difference += tmp;
            }
            return result;
        }
    }
}
