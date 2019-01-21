using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using Domain;
using ViewModels;
using ViewModels.Generators;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.System;
using static System.Math;

namespace Views
{
    public sealed partial class ParkingSimulationView : Page
    {
        private readonly Color Filter = Color.FromArgb(255, 155, 0, 0);
        private readonly Color YellowFilter = Color.FromArgb(255, 155, 255, 0);
        private readonly Uri _carImageUri = new Uri("ms-appx:///Views/Assets/car.png");
        private readonly Uri _entryImageUri = new Uri("ms-appx:///Views/Assets/entry.png");
        private readonly Uri _exitImageUri = new Uri("ms-appx:///Views/Assets/exit.png");
        private readonly Uri _cashboxImageUri = new Uri("ms-appx:///Views/Assets/cashbox.png");


        private bool _canDraw = true;

        private CanvasBitmap _carImage;
        private CanvasBitmap _entryImage;
        private CanvasBitmap _exitImage;
        private CanvasBitmap _cashboxImage;

        private object _locker = new object();

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
            mainDisplay.Invalidate();
            RadioButton_Checked(DeterminateRadioButton, null);
            _vm.GetCarGenerator += GetCarGenerator;
            _vm.RefreshSimulationMenuEnabling += SetEnabling;
        }

        private async void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_vm == null || !_canDraw)
                return;

            if (_carImage == null && _canDraw)
            {
                _canDraw = false;
                _carImage = await CanvasBitmap.LoadAsync(sender.Device, _carImageUri);
                _entryImage = await CanvasBitmap.LoadAsync(sender.Device, _entryImageUri);
                _exitImage = await CanvasBitmap.LoadAsync(sender.Device, _exitImageUri);
                _cashboxImage = await CanvasBitmap.LoadAsync(sender.Device, _cashboxImageUri);
                _canDraw = true;
                sender.Invalidate();
                return;
            }

            //var cbmp = await CanvasBitmap.LoadAsync(sender.Device, _carImageUri);
            lock (_locker)
            {
                var drawingSession = args.DrawingSession;

                if (drawingSession == null)
                    return;

                drawingSession?.Clear(Colors.White);

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

                int maxx = cells.GetLength(0);
                int maxy = cells.GetLength(1);

                for (int x = 0; x < maxx; x++)
                {
                    for (int y = 0; y < maxy; y++)
                    {
                        DrawCell(cells[x, y], new Rect(
                            scalex * x + cellthickness + shiftx,
                            scaley * y + cellthickness + shifty,
                            scalex - cellthickness * 2,
                            scaley - cellthickness * 2), drawingSession, availableCells[x, y]);
                    }
                }

                if (_vm.IsSimulationState)
                {
                    var cars = _vm.Cars;
                    double carWidth = 0.5 * scalex;
                    double carHeight = 0.5 * scaley;
                    foreach (var car in cars)
                    {
                        int x = car.Coordinates.X, y = car.Coordinates.Y;
                        var shifts = DirectionTransformation.TransformStepToCoord(car.Step, car.From, car.To);
                        shifts.x = (shifts.x + 1.0) / 2;
                        shifts.y = (shifts.y + 1.0) / 2;
                        var tr1 = new Transform2DEffect
                        {
                            Source = _carImage,
                            TransformMatrix = Matrix3x2.CreateTranslation(new Vector2(-100, -100))
                        };
                        var tr2def = new Transform2DEffect
                        {
                            Source = tr1,
                            TransformMatrix = Matrix3x2.CreateRotation((float)shifts.angle),
                        };
                        var sizeconst = car.IsTruck ? 100 : 200;
                        var tr2deft = new Transform2DEffect
                        {
                            Source = tr2def,
                            TransformMatrix = Matrix3x2.CreateScale((float)(carWidth / sizeconst))
                        };
                        drawingSession.DrawImage(
                            tr2deft,
                            new Vector2(
                                (float)(scalex * (x + shifts.x) + shiftx),
                                (float)(scaley * (y + shifts.y) + shifty)));
                        /*drawingSession.FillEllipse(
                            new Vector2((float)(scalex * (x + shifts.x) + shiftx), (float)(scaley * (y + shifts.y) + shifty)),
                            7, 7, car.CarColor);*/
                    }
                }
            }
        }

        private void DrawCell(CellType type, Rect rect, CanvasDrawingSession drawingSession, bool needApplyFilter)
        {
            needApplyFilter &= _vm.IsConstructorState;
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
                case CellType.Entry:
                case CellType.Exit:
                case CellType.CashBox:
                    resultColor = Colors.Gray;
                    break;
            }
            if (needApplyFilter)
            {
                if (type == CellType.ParkingSpace)
                {
                    resultColor = Colors.Orange;
                }
                else
                {
                    resultColor = ApplyFilter(resultColor, Filter);
                }
            }
            drawingSession.FillRectangle(rect, resultColor);

            switch(type)
            {
                case CellType.Entry:
                    drawingSession.DrawImage(_entryImage, rect);
                    break;
                case CellType.Exit:
                    drawingSession.DrawImage(_exitImage, rect);
                    break;
                case CellType.CashBox:
                    drawingSession.DrawImage(_cashboxImage, rect);
                    break;
            }
        }

        private void CanvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_vm.IsConstructorState)
                return;

            var canv = sender as CanvasControl;
            var width = canv.ActualWidth;
            var height = canv.ActualHeight;
            var cells = _vm.Cells;
            var availableCells = _vm.AvailableCells;
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
            if (InRectangle(cells, x2, y2) && availableCells[x2, y2])
            {
                if (_vm.SelectedType == CellType.Exit || _vm.SelectedType == CellType.Entry || _vm.SelectedType == CellType.CashBox)
                    for (int i = 0; i < cells.GetLength(0); i++)
                    {
                        for (int j = 0; j < cells.GetLength(1); j++)
                        {
                            if (cells[i, j] == _vm.SelectedType 
                                || (_vm.SelectedType == CellType.Exit 
                                    && cells[i, j] == CellType.CashBox))
                                cells[i, j] = CellType.Parking;
                        }
                    }
                cells[x2, y2] = _vm.SelectedType;
                if (_vm.SelectedType == CellType.Exit)
                {
                    var tmpCells = (CellType[,])cells.Clone();

                    cells[x2 + _vm.CheckCorrectCellForExit(tmpCells, new Coord(x2, y2)), y2] = CellType.CashBox;
                }
                _vm.IsCellsChanged = true;
                _vm.RecalculateAvailableCells();
                canv.Invalidate();
            }
        }

        private bool InRectangle(CellType[,] cells, int x, int y)
        {
            return x > 0 && y > 0 && x < cells.GetLength(0) - 1 && y < cells.GetLength(1);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ExponentialParameter1 == null)
                return;
            #region TextBoxes Disablings
            DeterminateParameter1.IsEnabled = false;
            UniformParameter1.IsEnabled = false;
            UniformParameter2.IsEnabled = false;
            NormalParameter1.IsEnabled = false;
            NormalParameter2.IsEnabled = false;
            ExponentialParameter1.IsEnabled = false;
            RandomRadioButton.IsEnabled = true;
            DeterminateRadioButton.IsEnabled = true;
            #endregion

            #region RadioButton and TextBoxes Enablings
            if (DeterminateRadioButton.IsChecked ?? false)
            {
                DeterminateParameter1.IsEnabled = true;
                UniformRadioButton.IsEnabled = false;
                NormalRadioButton.IsEnabled = false;
                ExponentialRadioButton.IsEnabled = false;
            }
            else
            {
                UniformRadioButton.IsEnabled = true;
                NormalRadioButton.IsEnabled = true;
                ExponentialRadioButton.IsEnabled = true;
                if (NormalRadioButton.IsChecked ?? false)
                {
                    NormalParameter1.IsEnabled = true;
                    NormalParameter2.IsEnabled = true;
                }
                if (UniformRadioButton.IsChecked ?? false)
                {
                    UniformParameter1.IsEnabled = true;
                    UniformParameter2.IsEnabled = true;
                }
                if (ExponentialRadioButton.IsChecked ?? false)
                {
                    ExponentialParameter1.IsEnabled = true;
                }
            }
            #endregion
        }

        private void SetEnabling(bool res)
        {
            if (res)
            {
                #region DISABLINGS radiobuttons
                DeterminateRadioButton.IsEnabled = false;
                RandomRadioButton.IsEnabled = false;
                UniformRadioButton.IsEnabled = false;
                NormalRadioButton.IsEnabled = false;
                ExponentialRadioButton.IsEnabled = false;
                #endregion

                #region DISABLINGS parameters
                DeterminateParameter1.IsEnabled = false;
                UniformParameter1.IsEnabled = false;
                UniformParameter2.IsEnabled = false;
                NormalParameter1.IsEnabled = false;
                NormalParameter2.IsEnabled = false;
                ExponentialParameter1.IsEnabled = false;
                #endregion

                #region DISABLINGS incrementercontrols
                DayRateIncrementerControl.IsEnabled = false;
                NightRateIncrementerControl.IsEnabled = false;
                #endregion
            }
            else
            {
                RadioButton_Checked(null, null);

                #region ENABLINGS incrementercontrols
                DayRateIncrementerControl.IsEnabled = true;
                NightRateIncrementerControl.IsEnabled = true;
                #endregion
            }
        }

        private CarGeneratorBase GetCarGenerator()
        {
            Func<string, double> parser = s => double.Parse(s.Replace('.', ',')) * 1000;
            if (DeterminateRadioButton?.IsChecked ?? false)
                return new DeterminateGenerator(parser(DeterminateParameter1.Text));
            if (UniformRadioButton?.IsChecked ?? false)
                return new UniformGenerator(parser(UniformParameter1.Text), parser(UniformParameter2.Text));
            if (NormalRadioButton?.IsChecked ?? false)
                return new NormalGenerator(parser(NormalParameter1.Text), parser(NormalParameter2.Text));
            if (ExponentialRadioButton?.IsChecked ?? false)
                return new ExponentialGenerator(parser(ExponentialParameter1.Text) / (1000 * 1000));
            return null;
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("ms-appx-web:///Views/Assets/help.html");
            HelpView.Visibility = Visibility.Visible;
            //Launcher(uri);
        }

        private void CloseWebViewButton_Click(object sender, RoutedEventArgs e)
        {
            HelpView.Visibility = Visibility.Collapsed;
        }
    }
}
