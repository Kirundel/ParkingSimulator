using Models;
using Infrastructure;
using Domain;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Windows.UI.Xaml;
using ViewModels.Generators;

namespace ViewModels
{
    public enum ProgramState
    {
        Constructor,
        Simulation,
        Help
    }

    public class ParkingSimulationViewModel : ViewModelBase
    {
        private const double CAR_SPEED = 0.05;

        private readonly int[] dx = new int[] { 1, -1, 0, 0 };
        private readonly int[] dy = new int[] { 0, 0, 1, -1 };

        private Dictionary<CellType, HashSet<CellType>> _allowedStep =
            new Dictionary<CellType, HashSet<CellType>>
            {
                { CellType.Empty, new HashSet<CellType>() },
                { CellType.Entry, new HashSet<CellType>{CellType.Exit, CellType.Parking} },
                { CellType.Exit, new HashSet<CellType>{CellType.Entry, CellType.Road, CellType.Road} },
                { CellType.Parking, new HashSet<CellType>{CellType.Exit, CellType.Parking} },
                { CellType.ParkingSpace, new HashSet<CellType>{CellType.Entry, CellType.Exit, CellType.Parking} },
                { CellType.Road, new HashSet<CellType>{CellType.Road, CellType.Entry} }
            };

        private DateTime _lastUpdate;
        private DispatcherTimer _timer;
        private ICarGenerator _carGenerator = new UniformGenerator(5000);

        private readonly List<ButtonTabViewModel> _tabButtons;
        private ParkingSimulationModel _parkingSimulationModel;
        private CellType _selectedType = CellType.Empty;
        private ProgramState _programState = ProgramState.Constructor;
        private bool[,] _availableCells;
        private bool[,] _lockedCell;

        private List<Car> _cars = new List<Car>();
        private object _carsLocker = new object();

        public ParkingSimulationViewModel()
        {
            _parkingSimulationModel = IoC.GetModel<ParkingSimulationModel>();

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 6);
            _timer.Tick += TimerTick;

            AsphaltCommand = new RelayCommand(() => SelectedType = CellType.Parking);
            ParkingSpaceCommand = new RelayCommand(() => SelectedType = CellType.ParkingSpace);
            EntryCommand = new RelayCommand(() => SelectedType = CellType.Entry);
            ExitCommand = new RelayCommand(() => SelectedType = CellType.Exit);
            StartSimulationCommand = new RelayCommand(StartSimulation);
            StopSimulationCommand = new RelayCommand(StopSimulation);

            int xl = 11;
            int yl = 10;
            //int xl = 500;
            //int yl = 500;
            _parkingSimulationModel.GenerateCells(xl, yl, 1, xl - 2, 1, yl - 1);
            _availableCells = new bool[Cells.GetLength(0), Cells.GetLength(1)];
            _lockedCell = new bool[Cells.GetLength(0), Cells.GetLength(1)];
            _tabButtons = new List<ButtonTabViewModel>();
            _tabButtons.Add(new ButtonTabViewModel(true, "Конструктор", ProgramState.Constructor, OnTabSelected));
            _tabButtons.Add(new ButtonTabViewModel(false, "Визуализатор", ProgramState.Simulation, OnTabSelected));
            _tabButtons.Add(new ButtonTabViewModel(false, "Справка", ProgramState.Help, OnTabSelected));
            //SelectedType = CellType.Parking;
        }

        public event Action InvalidateView;

        public RelayCommand AsphaltCommand { get; }
        public RelayCommand ParkingSpaceCommand { get; }
        public RelayCommand EntryCommand { get; }
        public RelayCommand ExitCommand { get; }

        public RelayCommand StartSimulationCommand { get; }
        public RelayCommand StopSimulationCommand { get; }

        public bool AsphaltSelected => SelectedType == CellType.Parking;
        public bool ParkingSpaceSelected => SelectedType == CellType.ParkingSpace;
        public bool EntrySelected => SelectedType == CellType.Entry;
        public bool ExitSelected => SelectedType == CellType.Exit;

        public bool IsConstructorState => ProgramStateValue == ProgramState.Constructor;
        public bool IsSimulationState => ProgramStateValue == ProgramState.Simulation;
        public bool IsHelpState => ProgramStateValue == ProgramState.Help;

        public CellType[,] Cells => _parkingSimulationModel.Cells;

        public bool[,] AvailableCells => _availableCells;

        public List<ButtonTabViewModel> Tabs => _tabButtons;

        public CellType SelectedType
        {
            get => _selectedType;
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                }
                else
                {
                    _selectedType = CellType.Empty;
                }

                RecalculateAvailableCells();
                InvalidateView?.Invoke();
                NotifyAboutSelectedTypeChanges();
            }
        }

        public ProgramState ProgramStateValue
        {
            get => _programState;
            set
            {
                if (_programState != value)
                {
                    _programState = value;
                    foreach (var item in Tabs)
                        if (item.State == _programState)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                    NotifyOfPropertyChanged(nameof(ProgramStateValue));
                }
            }
        }

        public List<Car> Cars
        {
            get
            {
                lock (_carsLocker)
                {
                    return new List<Car>(_cars);
                }
            }
        }

        public void RecalculateAvailableCells()
        {
            SetAvailable(false);
            switch (SelectedType)
            {
                case CellType.Entry:
                case CellType.Exit:
                    for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
                    {
                        if (Cells[i, 1] == CellType.Entry || Cells[i, 1] == CellType.Exit)
                            continue;
                        _availableCells[i, 1] = true;
                    }
                    break;
                case CellType.Parking:
                    {
                        SetAvailable(true);
                        var entry = GetFirstElement(Cells, CellType.Entry);
                        var exit = GetFirstElement(Cells, CellType.Exit);
                        _availableCells[entry.X, entry.Y] = false;
                        _availableCells[exit.X, exit.Y] = false;
                    }
                    break;
                case CellType.ParkingSpace:
                    {
                        var cells = (CellType[,])Cells.Clone();
                        var entry = GetFirstElement(cells, CellType.Entry);
                        var exit = GetFirstElement(cells, CellType.Exit);
                        for (int i = 1; i < cells.GetLength(0) - 1; i++)
                        {
                            for (int j = 1; j < cells.GetLength(1); j++)
                            {
                                if (cells[i, j] == CellType.Parking)
                                {
                                    cells[i, j] = CellType.ParkingSpace;
                                    if (CheckCorrect(cells, entry) && CheckCorrect(cells, exit))
                                        _availableCells[i, j] = true;
                                    cells[i, j] = CellType.Parking;
                                }
                            }
                        }
                        //SetAvailable(true);
                    }
                    break;
                default:
                    break;
            }
        }

        public void StartSimulation()
        {
            _lastUpdate = DateTime.Now;
            _timer.Start();
        }

        public void StopSimulation()
        {
            _timer.Stop();
            _cars.Clear();
            _lockedCell = new bool[Cells.GetLength(0), Cells.GetLength(1)];
            InvalidateView?.Invoke();
        }

        private void TimerTick(object sender, object parameter)
        {
            Debug.WriteLine("TimerTick begin");

            var now = DateTime.Now;
            var difference = (int)Math.Round((now - _lastUpdate).TotalMilliseconds);
            _lastUpdate = now;
            foreach (var car in _cars)
            {
                car.Move(difference);
            }

            lock (_carsLocker)
            {
                foreach (var car in _carGenerator.Generate(difference))
                {
                    Debug.WriteLine("NEW CAR");
                    _cars.Add(car);
                    var parkingSpace = FindFreeParkingSpace();
                    if (parkingSpace == null)
                    {
                        car.GenerateWay(new Queue<Coord>(DoUNoDeWay(new Coord(0, 0), new Coord(Cells.GetLength(0), 0))));
                    }
                    else
                    {
                        _lockedCell[parkingSpace.X, parkingSpace.Y] = true;
                        var way1 = DoUNoDeWay(new Coord(0, 0), parkingSpace);
                        var way2 = DoUNoDeWay(parkingSpace, new Coord(Cells.GetLength(0), 0));
                        var result = way1.Take(way1.Count - 1);
                        result = result.Append(new StopCoord(parkingSpace.X, parkingSpace.Y, 10000));
                        result = result.Concat(way2.Skip(1));
                        var q = new Queue<Coord>(result);
                        car.GenerateWay(q);
                        car.UnlockParkingSpace += () => _lockedCell[parkingSpace.X, parkingSpace.Y] = false;
                    }
                    car.Speed = CAR_SPEED;
                }
                _cars.RemoveAll(x => x.NeedDelete);
            }
            InvalidateView?.Invoke();
        }

        private List<Coord> DoUNoDeWay(Coord from, Coord to)
        {
            Debug.WriteLine("DO U NO DE WAY");

            int[,] used = new int[Cells.GetLength(0), Cells.GetLength(1)];
            for (int x = 0; x < used.GetLength(0); x++)
                for (int y = 0; y < used.GetLength(1); y++)
                    used[x, y] = int.MaxValue;
            used[from.X, from.Y] = 0;
            Queue<Coord> q = new Queue<Coord>();
            q.Enqueue(from);
            int result = int.MaxValue;
            while (q.Count > 0)
            {
                var top = q.Dequeue();
                int way = used[top.X, top.Y] + 1;
                bool f = false;
                for (int i = 0; i < dx.Length; i++)
                {
                    int newx = top.X + dx[i];
                    int newy = top.Y + dy[i];

                    if (newx == to.X && newy == to.Y)
                    {
                        result = way;
                        f = true;
                        break;
                    }
                    if (!InRectangle(newx, newy))
                        continue;
                    if (used[newx, newy] > way
                        && _allowedStep[Cells[top.X, top.Y]].Contains(Cells[newx, newy]))
                    {
                        used[newx, newy] = way;
                        q.Enqueue(new Coord(newx, newy));
                    }
                }
                if (f)
                    break;
            }
            int nowx = to.X;
            int nowy = to.Y;
            List<Coord> answer = new List<Coord>();
            while (result > 0)
            {
                for (int i = 0; i < dx.Length; i++)
                {
                    int newx = nowx + dx[i];
                    int newy = nowy + dy[i];
                    if (!InRectangle(newx, newy))
                        continue;
                    if (used[newx, newy] == result - 1)
                    {
                        answer.Add(new Coord(nowx, nowy));
                        nowx = newx;
                        nowy = newy;
                        result--;
                        break;
                    }
                }
            }
            answer.Add(new Coord(nowx, nowy));
            answer.Reverse();
            return answer;
        }

        private Coord FindFreeParkingSpace()
        {
            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                for (int y = 0; y < Cells.GetLength(1); y++)
                {
                    if (Cells[x, y] == CellType.ParkingSpace && !_lockedCell[x, y])
                        return new Coord(x, y);
                }
            }
            return null;
        }

        private bool CheckCorrect(CellType[,] cells, Coord begin)
        {
            bool[,] used = new bool[cells.GetLength(0), cells.GetLength(1)];
            used[begin.X, begin.Y] = true;
            Queue<Coord> q = new Queue<Coord>();
            q.Enqueue(begin);
            while (q.Count > 0)
            {
                var top = q.Dequeue();
                if (cells[top.X, top.Y] == CellType.ParkingSpace)
                    continue;
                for (int i = 0; i < dx.Length; i++)
                {
                    int newx = top.X + dx[i];
                    int newy = top.Y + dy[i];
                    if (!InRectangle(newx, newy))
                        continue;
                    if (!used[newx, newy]
                        && cells[newx, newy] != CellType.Empty
                        && cells[newx, newy] != CellType.Road)
                    {
                        used[newx, newy] = true;
                        q.Enqueue(new Coord(newx, newy));
                    }
                }
            }
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    if (cells[x, y] == CellType.ParkingSpace && !used[x, y])
                        return false;
                }
            }
            return true;
        }

        private Coord GetFirstElement(CellType[,] cells, CellType type)
        {
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                    if (cells[i, j] == type)
                        return new Coord(i, j);
            }
            return new Coord(-1, -1);
        }


        private void SetAvailable(bool value)
        {
            for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
            {
                for (int j = 1; j < _availableCells.GetLength(1); j++)
                {
                    _availableCells[i, j] = value;
                }
            }
        }

        private void NotifyAboutSelectedTypeChanges()
        {
            NotifyOfPropertyChanged(nameof(SelectedType));
            NotifyOfPropertyChanged(nameof(AsphaltSelected));
            NotifyOfPropertyChanged(nameof(ParkingSpaceSelected));
            NotifyOfPropertyChanged(nameof(ExitSelected));
            NotifyOfPropertyChanged(nameof(EntrySelected));
        }

        private void OnTabSelected(ProgramState state)
        {
            ProgramStateValue = state;
            NotifyOfPropertyChanged(nameof(IsConstructorState));
            NotifyOfPropertyChanged(nameof(IsSimulationState));
            NotifyOfPropertyChanged(nameof(IsHelpState));
            InvalidateView?.Invoke();
        }

        private bool InRectangle(int newx, int newy)
        {
            return !(newx < 0 || newx >= Cells.GetLength(0) || newy < 0 || newy >= Cells.GetLength(1));
        }
    }
}
