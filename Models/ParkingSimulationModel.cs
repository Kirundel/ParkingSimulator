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
            for (int x = beginN; x <= endN; x++)
            {
                for (int y = beginM; y <= endM; y++)
                {
                    _cells[x, y] = CellType.Parking;
                }
            }
            _cells[beginN, beginM] = CellType.Entry;
            _cells[endN, beginM] = CellType.Exit;
        }

        public CellType[,] Cells => _cells;
    }
}
