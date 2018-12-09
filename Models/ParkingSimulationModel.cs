using Domain;
using System;
using static System.Math;

namespace Models
{
    public class ParkingSimulationModel
    {
        private CellType[,] _cells;

        public ParkingSimulationModel()
        {
        }

        public void GenerateCells(int n, int m, int beginN, int endN, int beginM, int endM)
        {
            _cells = new CellType[n, m];
            for (int i = 0; i < n; i++)
                _cells[i, beginM - 1] = CellType.Road;
            long centerN = (beginN + endN) / 2;
            long centerM = (beginM + endM) / 2;
            long thickness = 30;
            long radius = 185;
            //long thickness = 2;
            //long radius = 6;
            bool InCircle(long x, long y)
            {
                double GetDef(double x1)
                {
                    return Sqrt(Abs(x1 * x1 * x1 * x1 * x1));             
                }
                var r = GetDef(x - centerM) + GetDef(y - centerM);
                return r >= GetDef(radius) && r <= GetDef(radius + thickness);
            }
            for (int x = beginN; x <= endN; x++)
            {
                for (int y = beginM; y <= endM; y++)
                {
                    //_cells[x, y] = (x % 2 == 0 && y % 2 == 0) ? CellType.ParkingSpace : CellType.Parking;
                    //_cells[x, y] = (x + y) % 4 == 0 ? CellType.ParkingSpace : CellType.Parking;
                    _cells[x, y] = InCircle(x, y) ? CellType.ParkingSpace : CellType.Parking;
                }
            }
        }

        public CellType[,] Cells => _cells;
    }
}
