using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using static Infrastructure.Constants;

namespace Domain
{
    public enum Direction
    {
        Up, Down, Left, Right
    }

    public static class DirectionTransformation
    {
        public static (double x, double y, double angle) TransformStepToCoord(double step, Coord from, Coord to)
        {
            //if (from.X == to.X || from.Y == to.Y)
            {
                var tmp = step <= 50 ? from : to;
                var rtc = Abs(50 - step) / 50.0;
                var angle = step <= 50 ? GetAngleFromCoord(new Coord(-from.X, -from.Y)) : GetAngleFromCoord(to);
                return (tmp.X * rtc, tmp.Y * rtc, angle);
            }
            //else
            //{
            //    var anglex = from.X + to.X;
            //    var angley = from.Y + to.Y;
            //    var angle = PI * step / 200;
            //    var cosmultsign = Sign(from.X * to.Y - from.Y * to.X);
            //    if (cosmultsign == 1)
            //        angle = -angle;
            //    var x = anglex + (from.X - anglex) * Cos(angle) - (from.Y - angley) * Sin(angle);
            //    var y = angley + (from.Y - angley) * Cos(angle) + (from.X - anglex) * Sin(angle);
            //    return (x, y);
            //}
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

        private static double GetAngleFromCoord(Coord coord)
        {
            if (coord.X > SMALL_CONSTANT)
                return 0;
            if (coord.X < -SMALL_CONSTANT)
                return PI;
            if (coord.Y > SMALL_CONSTANT)
                return PI / 2;
            return 3 * PI / 2;
        }
    }
}
