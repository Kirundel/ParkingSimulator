﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using static Infrastructure.Constants;

namespace ViewModels.Generators
{
    public class UniformGenerator : ICarGenerator
    {
        private double _difference;

        public UniformGenerator(double left, double right)
        {
            Left = left;
            Right = right;
            _difference = 0;
        }

        public double Left { get; }
        public double Right { get; }

        public List<Car> Generate(int difference)
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
