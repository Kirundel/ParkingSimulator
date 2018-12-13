using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Coord
    {
        public int X, Y;

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class StopCoord : Coord
    {
        public double StopTime;

        public StopCoord(int x, int y, int stopTime): base(x, y)
        {
            StopTime = stopTime;
        }
    }
}
