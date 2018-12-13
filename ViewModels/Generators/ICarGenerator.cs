using System.Collections.Generic;
using Domain;

namespace ViewModels.Generators
{
    interface ICarGenerator
    {
        List<Car> Generate(int difference);
    }
}
