using Models;
using Infrastructure;
using Domain;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using ViewModels.Generators;
using Windows.UI.Popups;
using Windows.Storage.Pickers;
using static System.Math;
using static Infrastructure.Constants;

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
        private const double CAR_SPEED = 0.08;
        private const int MIN_STOP_BORDER = 60 * 1000;
        private const int MAX_STOP_BORDER = 6 * 3600 * 1000;

        private readonly int[] dx = new int[] { 1, -1, 0, 0 };
        private readonly int[] dy = new int[] { 0, 0, 1, -1 };
        private readonly DateTimeOffset BEGIN_TICK = new DateTimeOffset(new DateTime(2019, 1, 1));
        private readonly long DAY_IN_MILLISECONDS = (int)(new DateTime(2019, 1, 2) - new DateTime(2019, 1, 1)).TotalMilliseconds; 

        private Dictionary<CellType, HashSet<CellType>> _allowedStep =
            new Dictionary<CellType, HashSet<CellType>>
            {
                { CellType.Empty, new HashSet<CellType>() },
                { CellType.Entry, new HashSet<CellType>{CellType.Exit, CellType.Parking} },
                { CellType.Exit, new HashSet<CellType>{CellType.Entry, CellType.Road} },
                { CellType.Parking, new HashSet<CellType>{CellType.Exit, CellType.Parking} },
                { CellType.ParkingSpace, new HashSet<CellType>{CellType.Entry, CellType.Exit, CellType.Parking} },
                { CellType.Road, new HashSet<CellType>{CellType.Road, CellType.Entry} }
            };

        private DateTime _lastUpdate;
        private DispatcherTimer _timer;
        private CarGeneratorBase _carGenerator;

        private int _millisecondsNow = 0;

        private List<(int begin, int duration, int money)> _carsTimes = new List<(int begin, int duration, int money)>();
        private DateTimeOffset _beginDate = new DateTimeOffset(new DateTime(2019, 1, 1));
        private DateTimeOffset _endDate = new DateTimeOffset(new DateTime(2019, 1, 1));

        private readonly List<ButtonTabViewModel> _tabButtons;
        private ParkingSimulationModel _parkingSimulationModel;
        private CellType _selectedType = CellType.Empty;
        private ProgramState _programState = ProgramState.Constructor;
        private bool[,] _availableCells;
        private bool[,] _lockedCell;

        private List<Car> _cars = new List<Car>();
        private object _carsLocker = new object();

        private int _carsOnParking = 0;
        private int _trucksOnParking = 0;
        private int _moneyInCashBox = 0;
        private int _parkingspacesNum = 0;

        public ParkingSimulationViewModel()
        {
            _parkingSimulationModel = IoC.GetModel<ParkingSimulationModel>();
            _parkingSimulationModel.CheckCorrectField += CheckCorrectField;

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 9);
            _timer.Tick += TimerTick;

            AsphaltCommand = new RelayCommand(() => SelectedType = CellType.Parking);
            ParkingSpaceCommand = new RelayCommand(() => SelectedType = CellType.ParkingSpace);
            EntryCommand = new RelayCommand(() => SelectedType = CellType.Entry);
            ExitCommand = new RelayCommand(() => SelectedType = CellType.Exit);
            CashboxCommand = new RelayCommand(() => SelectedType = CellType.CashBox);
            ClearCommand = new RelayCommand(() => RegenerateCells());

            SaveCommand = new RelayCommand(OnSave);
            LoadCommand = new RelayCommand(OnLoad);

            StartSimulationCommand = new RelayCommand(StartSimulation);
            StopSimulationCommand = new RelayCommand(StopSimulation);
            PauseSimulationCommand = new RelayCommand(PauseSimulation);

            int xl = 11;
            int yl = 10;

            _tabButtons = new List<ButtonTabViewModel>();
            _tabButtons.Add(new ButtonTabViewModel(true, "Конструктор", ProgramState.Constructor, OnTabSelected));
            _tabButtons.Add(new ButtonTabViewModel(false, "Визуализатор", ProgramState.Simulation, OnTabSelected));
            _tabButtons.Add(new ButtonTabViewModel(false, "Справка", ProgramState.Help, OnTabSelected));
            WidthIncrementerViewModel = new IncrementerControlViewModel("Количество клеток по горизонтали", 10, 5, 20, OnSizeChanged);
            HeightIncrementerViewModel = new IncrementerControlViewModel("Количество клеток по вертикали", 10, 5, 20, OnSizeChanged);
            DayRateIncrementerViewModel = new IncrementerControlViewModel("Стоимость дневного тарифа (руб./ч)", 100, 10, 400, async () => true, 10);
            NightRateIncrementerViewModel = new IncrementerControlViewModel("Стоимость ночного тарифа (руб./ч)", 100, 10, 200, async () => true, 10);
            RegenerateCells();
            //SelectedType = CellType.Parking;
        }

        public event Action InvalidateView;
        public event Action<bool> RefreshSimulationMenuEnabling;
        public event Func<CarGeneratorBase> GetCarGenerator;

        public RelayCommand AsphaltCommand { get; }
        public RelayCommand ParkingSpaceCommand { get; }
        public RelayCommand EntryCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand CashboxCommand { get; }
        public RelayCommand ClearCommand { get; }

        public RelayCommand SaveCommand { get; }
        public RelayCommand LoadCommand { get; }

        public RelayCommand StartSimulationCommand { get; }
        public RelayCommand StopSimulationCommand { get; }
        public RelayCommand PauseSimulationCommand { get; }

        public bool AsphaltSelected => SelectedType == CellType.Parking;
        public bool ParkingSpaceSelected => SelectedType == CellType.ParkingSpace;
        public bool EntrySelected => SelectedType == CellType.Entry;
        public bool ExitSelected => SelectedType == CellType.Exit;
        public bool CashboxSelected => SelectedType == CellType.CashBox;

        public bool IsConstructorState => ProgramStateValue == ProgramState.Constructor;
        public bool IsSimulationState => ProgramStateValue == ProgramState.Simulation;
        public bool IsHelpState => ProgramStateValue == ProgramState.Help;
        public bool IsCanvasControlVisible => ProgramStateValue != ProgramState.Help;

        public CellType[,] Cells => _parkingSimulationModel.Cells;

        public bool[,] AvailableCells => _availableCells;

        public bool IsCellsChanged { get; set; } = false;

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
                    if (_programState == ProgramState.Simulation)
                    {
                        StopSimulation();
                    }
                    if (_programState == ProgramState.Constructor)
                    {
                        SelectedType = SelectedType;
                    }
                    _programState = value;
                    foreach (var item in Tabs)
                        if (item.State == _programState)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                    NotifyOfPropertyChanged(nameof(ProgramStateValue));
                    if (value == ProgramState.Simulation)
                    {
                        _parkingspacesNum = CountParkingSpaces();
                        NotifyOfPropertyChanged(nameof(StatisticsHeader));
                    }
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

        public string StatisticsHeader => $"Количество легковых автомобилей: {_carsOnParking}\nКоличество грузовых автомобилей: {_trucksOnParking}\nКоличество свободных мест: {_parkingspacesNum}\nВ кассе: {_moneyInCashBox} руб.";

        public string StatisticsHeader2
        {
            get
            {
                var beg = DateTimeToMilliseconds(_beginDate);
                var end = DateTimeToMilliseconds(_endDate.AddDays(1));
                List<(int begin, int duration, int money)> arr;
                lock (_carsTimes)
                {
                    arr = _carsTimes.Where(x => x.begin >= beg && x.begin + x.duration < end).ToList();
                }
                double avg_time = 0.0;
                int money = 0;
                if (arr.Count > 0)
                {
                    avg_time = ((double)arr.Sum(x => x.duration)) / (3600.0 * 1000 * arr.Count());
                    money = arr.Sum(x => x.money);
                }
                return $"Среднее время стоянки за выбранный период: {avg_time:0.00}ч\nДоход за выбранный период: {money} руб.";
            }
        }

        //public string StatisticsHeader => $"Количество легковых автомобилей: {_carsOnParking}\nКоличество грузовых автомобилей: {_trucksOnParking}\nКоличество свободных мест: {_parkingspacesNum}\nВ кассе: 0 руб.";


        public IncrementerControlViewModel WidthIncrementerViewModel { get; }
        public IncrementerControlViewModel HeightIncrementerViewModel { get; }

        public IncrementerControlViewModel DayRateIncrementerViewModel { get; }
        public IncrementerControlViewModel NightRateIncrementerViewModel { get; }

        public int TimeAcceleration { get; set; } = 1;

        public DateTimeOffset BeginDate
        {
            get => _beginDate;
            set
            {
                if (value == null || value.Year < 1000)
                {
                    _beginDate = BEGIN_TICK;
                    NotifyOfPropertyChanged(nameof(StatisticsHeader2));
                    return;
                }
                var tmp = GenerateNullDateTimeOffset(value);
                if (_beginDate != tmp)
                {
                    _beginDate = tmp;
                    NotifyOfPropertyChanged(nameof(BeginDate));
                    NotifyOfPropertyChanged(nameof(StatisticsHeader2));
                }
            }
        }

        public DateTimeOffset EndDate
        {
            get => _endDate;
            set
            {
                if (value == null || value.Year < 1000)
                {
                    _endDate = new DateTimeOffset(new DateTime(2100, 1, 1));
                    NotifyOfPropertyChanged(nameof(StatisticsHeader2));
                    return;
                }
                var tmp = GenerateNullDateTimeOffset(value);
                if (_endDate != tmp)
                {
                    _endDate = tmp;
                    NotifyOfPropertyChanged(nameof(BeginDate));
                    NotifyOfPropertyChanged(nameof(StatisticsHeader2));
                }
            }
        }

        public void RecalculateAvailableCells()
        {
            SetAllCellsAvailable(false);
            var cells = (CellType[,])Cells.Clone();
            var entry = GetFirstElement(cells, CellType.Entry);
            var exit = GetFirstElement(cells, CellType.Exit);
            var cashbox = GetFirstElement(cells, CellType.CashBox);
            switch (SelectedType)
            {
                case CellType.Entry:
                    cells[entry.X, entry.Y] = CellType.Parking;
                    for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
                    {
                        if (Cells[i, 1] == CellType.Entry || Cells[i, 1] == CellType.Exit || Cells[i, 1] == CellType.CashBox)
                            continue;
                        var prev = cells[i, 1];
                        cells[i, 1] = CellType.Entry;
                        if (CheckCorrect(cells, new Coord(i, 1)) && CheckCorrect(cells, exit))
                            _availableCells[i, 1] = true;
                        cells[i, 1] = prev;
                    }
                    break;
                case CellType.Exit:
                    cells[exit.X, exit.Y] = CellType.Parking;
                    cells[cashbox.X, cashbox.Y] = CellType.Parking;
                    for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
                    {
                        if (Cells[i, 1] == CellType.Entry 
                            || Cells[i, 1] == CellType.Exit)
                            continue;
                        if (CheckCorrectCellForExit(cells, new Coord(i, 1), entry) != 0)
                            _availableCells[i, 1] = true;
                    }
                    break;
                case CellType.CashBox:
                    cells[cashbox.X, cashbox.Y] = CellType.Parking;
                    for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
                    {
                        if ((Cells[i - 1, 1] != CellType.Exit && Cells[i + 1, 1] != CellType.Exit) || Cells[i, 1] == CellType.CashBox)
                            continue;
                        cells[i, 1] = CellType.CashBox;
                        if (CheckCorrect(cells, entry) && CheckCorrect(cells, exit))
                            _availableCells[i, 1] = true;
                        cells[i, 1] = CellType.Parking;
                    }
                    break;
                case CellType.Parking:
                    {
                        SetAllCellsAvailable(true);
                        _availableCells[entry.X, entry.Y] = false;
                        _availableCells[exit.X, exit.Y] = false;
                        _availableCells[cashbox.X, cashbox.Y] = false;
                    }
                    break;
                case CellType.ParkingSpace:
                    {
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

        public int CheckCorrectCellForExit(CellType[,] cells, Coord cell, Coord entry = null)
        {
            if (entry == null)
                entry = GetFirstElement(cells, CellType.Entry);
            var prev = cells[cell.X, cell.Y];
            cells[cell.X, cell.Y] = CellType.Exit;
            int result = 0;
            if (cells[cell.X - 1, cell.Y] == CellType.Parking)
            {
                cells[cell.X - 1, cell.Y] = CellType.CashBox;
                if (CheckCorrect(cells, cell) && CheckCorrect(cells, entry))
                    result = -1;
                cells[cell.X - 1, cell.Y] = CellType.Parking;
            }
            if (result != -1 && cells[cell.X + 1, cell.Y] == CellType.Parking)
            {
                cells[cell.X + 1, cell.Y] = CellType.CashBox;
                if (CheckCorrect(cells, cell) && CheckCorrect(cells, entry))
                    result = 1;
                cells[cell.X + 1, cell.Y] = CellType.Parking;
            }
            cells[cell.X, cell.Y] = prev;
            return result;
        }

        public async void StartSimulation()
        {
            try
            {
                _parkingspacesNum = CountParkingSpaces();
                _carGenerator = GetCarGenerator();
                _lastUpdate = DateTime.Now;
            }
            catch(IncorrectGeneratorParameterException ex)
            {
                await ShowMessage(ex.ToString());
                return;
            }
            catch
            {
                await ShowMessage("Введены некорректные параметры для генерации потока ТС");
                return;
            }
            RefreshSimulationMenuEnabling?.Invoke(true);
            _timer.Start();
        }

        public void PauseSimulation()
        {
            _timer.Stop();
        }

        public void StopSimulation()
        {
            _timer.Stop();
            _carsTimes.Clear();
            _cars.Clear();
            _lockedCell = new bool[Cells.GetLength(0), Cells.GetLength(1)];
            _carsOnParking = 0;
            _trucksOnParking = 0;
            _moneyInCashBox = 0;
            _parkingspacesNum = 0;
            NotifyOfPropertyChanged(nameof(StatisticsHeader));
            NotifyOfPropertyChanged(nameof(StatisticsHeader2));
            InvalidateView?.Invoke();
            RefreshSimulationMenuEnabling?.Invoke(false);
        }

        private int CountParkingSpaces()
        {
            int answer = 0;
            for (int i = 0; i < Cells.GetLength(0); i++)
                for (int j = 0; j < Cells.GetLength(1); j++)
                    if (Cells[i, j] == CellType.ParkingSpace)
                        answer++;
            return answer;
        }

        private void TimerTick(object sender, object parameter)
        {
            //Debug.WriteLine("TimerTick begin");

            var now = DateTime.Now;
            var difference = (int)Round((now - _lastUpdate).TotalMilliseconds);
            _lastUpdate = now;

            difference *= TimeAcceleration;

            _millisecondsNow += difference;

            foreach (var car in _cars)
            {
                car.Move(difference);
            }

            
            Func<int> GenerateStopTime = () => (int)(RND.NextDouble() * (MAX_STOP_BORDER - MIN_STOP_BORDER)) + MIN_STOP_BORDER;

            lock (_carsLocker)
            {
                foreach (var car in _carGenerator.Generate(difference))
                {
                    //Debug.WriteLine("NEW CAR");
                    _cars.Add(car);
                    if (car.IsTruck)
                    {
                        var parkingSpace = FindFreeTruckParkingSpace();
                        if (parkingSpace.first == null)
                        {
                            car.GenerateWay(new Queue<Coord>(FindWay(new Coord(0, 0), new Coord(Cells.GetLength(0), 0))));
                        }
                        else
                        {
                            _lockedCell[parkingSpace.first.X, parkingSpace.first.Y] = true;
                            _lockedCell[parkingSpace.second.X, parkingSpace.second.Y] = true;
                            _parkingspacesNum -= 2;

                            var way11 = FindWay(new Coord(0, 0), parkingSpace.first);
                            var way12 = FindWay(new Coord(0, 0), parkingSpace.second);
                            var way21 = FindWay(parkingSpace.second, new Coord(Cells.GetLength(0), 0));
                            var way22 = FindWay(parkingSpace.first, new Coord(Cells.GetLength(0), 0));

                            List<Coord> way1;
                            List<Coord> way2;
                            if (way11.Count + way21.Count < way12.Count + way22.Count)
                            {
                                way1 = way11;
                                way2 = way21;
                            }
                            else
                            {
                                way1 = way12;
                                way2 = way22;
                            }

                            IEnumerable<Coord> result = way1;
                            result = result.Append(new StopCoord(way2[0].X, way2[0].Y, GenerateStopTime()));
                            result = result.Concat(way2.Skip(1));
                            var q = new Queue<Coord>(result);
                            car.GenerateWay(q);
                            car.UnlockParkingSpace += () =>
                            {
                                _lockedCell[parkingSpace.first.X, parkingSpace.first.Y] = false;
                                _lockedCell[parkingSpace.second.X, parkingSpace.second.Y] = false;
                                _parkingspacesNum += 2;
                            };
                        }
                    }
                    else
                    {
                        var parkingSpace = FindFreeParkingSpace();
                        if (parkingSpace == null)
                        {
                            car.GenerateWay(new Queue<Coord>(FindWay(new Coord(0, 0), new Coord(Cells.GetLength(0), 0))));
                        }
                        else
                        {
                            _lockedCell[parkingSpace.X, parkingSpace.Y] = true;
                            _parkingspacesNum--;

                            var way1 = FindWay(new Coord(0, 0), parkingSpace);
                            var way2 = FindWay(parkingSpace, new Coord(Cells.GetLength(0), 0));
                            var result = way1.Take(way1.Count - 1);
                            result = result.Append(new StopCoord(parkingSpace.X, parkingSpace.Y, GenerateStopTime()));
                            result = result.Concat(way2.Skip(1));
                            var q = new Queue<Coord>(result);
                            car.GenerateWay(q);
                            car.UnlockParkingSpace += () => 
                            {
                                _lockedCell[parkingSpace.X, parkingSpace.Y] = false;
                                _parkingspacesNum++;
                            };
                        }
                    }

                    car.Speed = CAR_SPEED;

                    car.Entry = GetFirstElement(Cells, CellType.Entry);
                    car.Exit = GetFirstElement(Cells, CellType.Exit);
                    car.EntryReached += OnCarReachedEntry;
                    car.ExitReached += OnCarReacheExit;
                }
                _cars.RemoveAll(x => x.NeedDelete);
            }
            InvalidateView?.Invoke();
        }

        private List<Coord> FindWay(Coord from, Coord to)
        {
            //Debug.WriteLine("Find way");

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

            for (int y = 0; y < Cells.GetLength(1); y++)
            {
                for (int x = 0; x < Cells.GetLength(0); x++)
                {
                    if (Cells[x, y] == CellType.ParkingSpace && !_lockedCell[x, y])
                        return new Coord(x, y);
                }
            }
            return null;
        }

        private (Coord first, Coord second) FindFreeTruckParkingSpace()
        {
            for (int y = 1; y < Cells.GetLength(1); y++)
            {
                for (int x = 1; x < Cells.GetLength(0) - 1; x++)
                {
                    if (Cells[x, y] == CellType.ParkingSpace && !_lockedCell[x, y])
                    {
                        for (int i = 0; i < dx.Length; i++)
                        {
                            int x1 = x + dx[i];
                            int y1 = y + dy[i];
                            if (InRectangle(x1, y1) && Cells[x1, y1] == CellType.ParkingSpace && !_lockedCell[x1, y1])
                            {
                                return (new Coord(x, y), new Coord(x1, y1));
                            }
                        }
                    }
                }
            }
            return (null, null);
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
                        && _allowedStep[cells[top.X, top.Y]].Contains(cells[newx, newy]) || cells[newx, newy] == CellType.ParkingSpace)
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


        private void SetAllCellsAvailable(bool value)
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
            NotifyOfPropertyChanged(nameof(CashboxSelected));
        }

        private void OnTabSelected(ProgramState state)
        {
            ProgramStateValue = state;
            NotifyOfPropertyChanged(nameof(IsConstructorState));
            NotifyOfPropertyChanged(nameof(IsSimulationState));
            NotifyOfPropertyChanged(nameof(IsHelpState));
            NotifyOfPropertyChanged(nameof(IsCanvasControlVisible));
            InvalidateView?.Invoke();
        }

        private bool InRectangle(int newx, int newy)
        {
            return !(newx < 0 || newx >= Cells.GetLength(0) || newy < 0 || newy >= Cells.GetLength(1));
        }

        private async Task<bool> OnSizeChanged()
        {
            if (IsCellsChanged)
            {
                var md = new MessageDialog("При изменении размера вся топология сбросится. Вы уверены, что хотите продолжить?");
                var yesCommand = new UICommand("Да");
                var noCommand = new UICommand("Нет");
                md.Options = MessageDialogOptions.None;
                md.Commands.Add(yesCommand);
                md.Commands.Add(noCommand);
                var result = await md.ShowAsync();
                if (result == noCommand)
                    return false;
            }
            RegenerateCells();
            return true;
        }

        private void RegenerateCells(bool needGenerateCells = true)
        {
            if (needGenerateCells)
                _parkingSimulationModel?.GenerateCells(
                    WidthIncrementerViewModel.Value + 2,
                    HeightIncrementerViewModel.Value + 1,
                    1,
                    WidthIncrementerViewModel.Value,
                    1,
                    HeightIncrementerViewModel.Value);
            IsCellsChanged = false;
            _availableCells = new bool[_parkingSimulationModel.Cells.GetLength(0), _parkingSimulationModel.Cells.GetLength(1)];
            _lockedCell = new bool[_parkingSimulationModel.Cells.GetLength(0), _parkingSimulationModel.Cells.GetLength(1)];
            RecalculateAvailableCells();
            InvalidateView?.Invoke();
        }

        private void OnCarReachedEntry(Car car)
        {
            if (car.IsTruck)
                _trucksOnParking++;
            else
                _carsOnParking++;
            NotifyOfPropertyChanged(nameof(StatisticsHeader));
        }

        private void OnCarReacheExit(Car car)
        {
            if (car.IsTruck)
                _trucksOnParking--;
            else
                _carsOnParking--;

            int rate = (_millisecondsNow % DAY_IN_MILLISECONDS < DAY_IN_MILLISECONDS / 2)
                            ? DayRateIncrementerViewModel.Value
                            : NightRateIncrementerViewModel.Value;

            int addedMoney = (int)Ceiling((car.AllStep / (3600.0 * 1000)) * rate);
            _moneyInCashBox += addedMoney;

            lock (_carsTimes)
            {
                _carsTimes.Add((_millisecondsNow - (int)car.AllStep, (int)car.AllStep, addedMoney));
            }
            NotifyOfPropertyChanged(nameof(StatisticsHeader));
            NotifyOfPropertyChanged(nameof(StatisticsHeader2));
        }

        private long DateTimeToMilliseconds(DateTimeOffset date)
        {
            return (long)(date - BEGIN_TICK).TotalMilliseconds;
        }

        private DateTimeOffset GenerateNullDateTimeOffset(DateTimeOffset dt)
        {
            return new DateTimeOffset(dt.Date);
        }

        private bool CheckCorrectField(CellType[,] cells)
        {
            var entry = GetFirstElement(cells, CellType.Entry);
            var exit = GetFirstElement(cells, CellType.Exit);
            var cashbox = GetFirstElement(cells, CellType.CashBox);
            if (entry.Y != 1 || exit.Y != 1 || cashbox.Y != 1 || Abs(exit.X - cashbox.X) != 1)
                return false;
            return CheckCorrect(cells, entry) && CheckCorrect(cells, exit);
        }

        private async void OnSave()
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Parking topology", new List<string> { ".park" });
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                if (!await _parkingSimulationModel.SaveToFile(file))
                    await ShowMessage("Невозможно записать в файл");
            }
        }

        private async void OnLoad()
        {
            var loadPicker = new FileOpenPicker();
            loadPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            loadPicker.FileTypeFilter.Add(".park");
            var file = await loadPicker.PickSingleFileAsync();
            if (file != null)
            {
                if (await _parkingSimulationModel.ReadFromFile(file))
                {
                    WidthIncrementerViewModel.Value = _parkingSimulationModel.Width;
                    HeightIncrementerViewModel.Value = _parkingSimulationModel.Height;
                    RegenerateCells(false);
                }
                else
                {
                    await ShowMessage("Некорректный файл");
                }
            }
        }

        private async Task ShowMessage(string message)
        {
            var md = new MessageDialog(message);
            await md.ShowAsync();
        }
    }
}
