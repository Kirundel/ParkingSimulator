using Infrastructure;
using System.Windows;

namespace Domain
{
    public class GridCell
    {
        public GridCell(Pair<int> coordinates, CellType cellType)
        {
            Coordinates = coordinates;
        }

        public Pair<int> Coordinates { get; }
        public CellType CellType { get; }
    }
}
