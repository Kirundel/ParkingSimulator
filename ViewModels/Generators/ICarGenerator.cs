using System.Collections.Generic;
using Domain;

namespace ViewModels.Generators
{
    public abstract class CarGeneratorBase
    {
        public abstract List<Car> Generate(int difference);

        protected void CheckValue(double value, int left, int right, string name)
        {
            if (value < left)
                throw new IncorrectGeneratorParameterException(name, (left / 1000).ToString(), IncorrectGeneratorParameterExceptionType.MIN);
            if (value > right)
                throw new IncorrectGeneratorParameterException(name, (right / 1000).ToString(), IncorrectGeneratorParameterExceptionType.MAX);
        }

        protected void CheckValue(double value, double left, double right, string name)
        {
            if (value < left)
                throw new IncorrectGeneratorParameterException(name, (left * 1000).ToString("0.000#"), IncorrectGeneratorParameterExceptionType.MIN);
            if (value > right)
                throw new IncorrectGeneratorParameterException(name, (right * 1000).ToString("0.#"), IncorrectGeneratorParameterExceptionType.MAX);
        }
    }
}
