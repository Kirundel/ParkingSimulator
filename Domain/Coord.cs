using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Coord : IEquatable<Coord>
    {
        public int X, Y;

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Coord other)
        {
            return X == other?.X && Y == other?.Y;
        }

        public static bool operator ==(Coord first, Coord second)
        {
            return first?.Equals(second) ?? second?.X == null;
        }

        public static bool operator !=(Coord first, Coord second)
        {
            return !(first == second);
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
