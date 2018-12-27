using Domain;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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

        public event Func<CellType[,], bool> CheckCorrectField;

        public CellType[,] Cells => _cells;

        public int Width => Cells.GetLength(0) - 2;

        public int Height => Cells.GetLength(1) - 1;

        public void GenerateCells(int n, int m, int beginN, int endN, int beginM, int endM)
        {
            _cells = GenerateDefaultCells(n, m, beginN, endN, beginM, endM);
            _cells[beginN, beginM] = CellType.Entry;
            _cells[endN - 1, beginM] = CellType.CashBox;
            _cells[endN, beginM] = CellType.Exit;
        }

        private CellType[,] GenerateDefaultCells(int n, int m, int beginN, int endN, int beginM, int endM)
        {
            var cells = new CellType[n, m];
            for (int i = 0; i < n; i++)
                cells[i, beginM - 1] = CellType.Road;
            long centerN = (beginN + endN) / 2;
            long centerM = (beginM + endM) / 2;
            for (int x = beginN; x <= endN; x++)
            {
                for (int y = beginM; y <= endM; y++)
                {
                    cells[x, y] = CellType.Parking;
                }
            }
            return cells;
        }

        public async Task<bool> SaveToFile(StorageFile storageFile)
        {
            var output = "";
            output += Width + " " + Height + Environment.NewLine;

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
                output += Environment.NewLine;
            }
            try
            {
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

        public async Task<bool> ReadFromFile(StorageFile storageFile)
        {
            try
            {
                if (storageFile == null)
                    return false;
                var lines = new List<string>(await FileIO.ReadLinesAsync(storageFile));
                var arr = lines[0].Split();
                var n = int.Parse(arr[0]);
                var m = int.Parse(arr[1]);
                if (n < 5 || n > 20 || m < 5 || m > 20)
                    return false;
                var cells = GenerateDefaultCells(n + 2, m + 1, 1, n, 1, m);
                for (int x = 1; x <= n; x++)
                {
                    for (int y = 1; y <= m; y++)
                    {
                        switch(lines[x][y - 1])
                        {
                            case 'P':
                                cells[x, y] = CellType.Parking;
                                break;
                            case 'S':
                                cells[x, y] = CellType.ParkingSpace;
                                break;
                            case 'I':
                                cells[x, y] = CellType.Entry;
                                break;
                            case 'O':
                                cells[x, y] = CellType.Exit;
                                break;
                            case 'C':
                                cells[x, y] = CellType.CashBox;
                                break;
                            default:
                                return false;
                        }
                    }
                }
                if (CheckCorrect(cells))
                {
                    _cells = cells;
                    return true;
                }
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool CheckCorrect(CellType[,] cells)
        {
            int[] celltypesCount = new int[10];
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    celltypesCount[(int)cells[i, j]]++;
                }
            }
            return celltypesCount[(int)CellType.Entry] == 1
                && celltypesCount[(int)CellType.Exit] == 1
                && celltypesCount[(int)CellType.CashBox] == 1
                && CheckCorrectField(cells);
        }
    }
}
