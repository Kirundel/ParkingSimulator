using Domain;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using static System.Math;

namespace Models
{
    public class ParkingSimulationModel
    {
        private CellType[,] _cells;

        public ParkingSimulationModel()
        {
        }

        public CellType[,] Cells => _cells;

        public int Width => Cells.GetLength(0) - 2;

        public int Height => Cells.GetLength(1) - 1;

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
            _cells[endN - 1, beginM] = CellType.CashBox;
            _cells[endN, beginM] = CellType.Exit;
        }

        public async Task<bool> SaveToFile(string fileAddress)
        {
            var output = "";
            output += Width + " " + Height;

            for (int i = 1; i < Cells.GetLength(0) - 1; i++)
            {
                for (int j = 1; j < Cells.GetLength(1); j++)
                {
                    switch (Cells[i, j])
                    {
                        case CellType.Parking:
                            output += "P";
                            break;
                        case CellType.ParkingSpace:
                            output += "S";
                            break;
                        case CellType.Entry:
                            output += "I";
                            break;
                        case CellType.Exit:
                            output += "O";
                            break;
                        case CellType.CashBox:
                            output += "C";
                            break;
                    }
                }
            }
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(fileAddress);
                if (storageFile == null)
                    return false;
                await FileIO.WriteTextAsync(storageFile, output);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReadFromFile()
        {
            return false;
        }
    }
}
