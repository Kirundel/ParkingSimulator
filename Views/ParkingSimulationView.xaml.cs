using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Domain;
using ViewModels;
using static System.Math;

namespace Views
{
    public sealed partial class ParkingSimulationView : Page
    {
        private readonly Color Filter = Color.FromArgb(255, 155, 0, 0);

        private ParkingSimulationViewModel _vm;

        public ParkingSimulationView()
        {
            this.InitializeComponent();
        }

        private static byte ApplyFilterToComponent(byte color, byte filter)
        {
            return (byte)(Min(255.0, color + ((255 - color) / 255.0) * filter));
        }

        private static Color ApplyFilter(Color color, Color filter)
        {
            return Color.FromArgb(
                            ApplyFilterToComponent(color.A, filter.A),
                            ApplyFilterToComponent(color.R, filter.R),
                            ApplyFilterToComponent(color.G, filter.G),
                            ApplyFilterToComponent(color.B, filter.B));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = new ParkingSimulationViewModel();
            _vm.InvalidateView += mainDisplay.Invalidate;
            this.DataContext = _vm;
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var drawingSession = args.DrawingSession;
            drawingSession.Clear(Colors.White);

            var width = sender.ActualWidth;
            var height = sender.ActualHeight;
            var cells = _vm.Cells;
            var availableCells = _vm.AvailableCells;
            var scalex = width / cells.GetLength(0);
            var scaley = height / cells.GetLength(1);
            scalex = Min(scalex, scaley);
            scaley = scalex;

            var shiftx = (width - scalex * cells.GetLength(0)) / 2;
            var shifty = (height - scaley * cells.GetLength(1)) / 2;

            var cellthickness = 0.5;
            //var cellthickness = 0;

            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    DrawCell(cells[x, y], new Rect(
                        scalex * x + cellthickness + shiftx, 
                        scaley * y + cellthickness + shifty, 
                        scalex - cellthickness * 2, 
                        scaley - cellthickness * 2), drawingSession, availableCells[x, y]);
                }
            }
        }

        private void DrawCell(CellType type, Rect rect, CanvasDrawingSession drawingSession, bool needApplyFilter)
        {
            Color resultColor = Colors.White;
            switch (type)
            {
                case CellType.Road:
                    resultColor = Colors.LightGray;
                    break;
                case CellType.ParkingSpace:
                    resultColor = Colors.Yellow;
                    break;
                case CellType.Parking:
                    resultColor = Colors.Gray;
                    break;
                case CellType.Entry:
                    resultColor = Colors.Khaki;
                    break;
                case CellType.Exit:
                    resultColor = Colors.Green;
                    break;
            }
            if (needApplyFilter)
                resultColor = ApplyFilter(resultColor, Filter);
            drawingSession.FillRectangle(rect, resultColor);
        }

        private void CanvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var canv = sender as CanvasControl;
            var width = canv.ActualWidth;
            var height = canv.ActualHeight;
            var cells = _vm.Cells;
            var availableCelss = _vm.AvailableCells;
            var scalex = width / cells.GetLength(0);
            var scaley = height / cells.GetLength(1);
            scalex = Min(scalex, scaley);
            scaley = scalex;

            var shiftx = (width - scalex * cells.GetLength(0)) / 2;
            var shifty = (height - scaley * cells.GetLength(1)) / 2;

            var coord = e.GetCurrentPoint(canv);

            var x1 = coord.Position.X - shiftx;
            var y1 = coord.Position.Y - shifty;
            x1 /= scalex;
            y1 /= scaley;
            var x2 = (int)x1;
            var y2 = (int)y1;
            if (InRectangle(cells, x2, y2) && availableCelss[x2, y2])
            {
                cells[x2, y2] = _vm.SelectedType;
                canv.Invalidate();
            }
        }

        private bool InRectangle(CellType[,] cells, int x, int y)
        {
            return x > 0 && y > 0 && x < cells.GetLength(0) - 1 && y < cells.GetLength(1);
        }
    }
}
