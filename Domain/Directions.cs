using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Domain
{
    public enum Direction
    {
        Up, Down, Left, Right
    }

    public static class DirectionTransformation
    {
        public static (double x, double y) TransformStepToCoord(double step, Coord from, Coord to)
        {
            var tmp = step <= 50 ? from : to;
            var rtc = Math.Abs(50 - step) / 50.0;
            return (tmp.X * rtc, tmp.Y * rtc);
        }

        public static Coord DirectionToCoord(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return new Coord(1, 0);
                case Direction.Down:
                    return new Coord(-1, 0);
                case Direction.Left:
                    return new Coord(0, -1);
                case Direction.Right:
                    return new Coord(0, 1);
            }
            return new Coord(0, 0);
        }
    }
}
