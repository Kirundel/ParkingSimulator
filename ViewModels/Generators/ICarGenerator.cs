using System.Collections.Generic;
using Domain;

namespace ViewModels.Generators
{
    public interface ICarGenerator
    {
        List<Car> Generate(int difference);
    }
}
