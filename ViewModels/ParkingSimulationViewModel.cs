using Models;
using Infrastructure;
using Domain;
using System;
using System.Collections.Generic;

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
        private readonly List<ButtonTabViewModel> _tabButtons;
        private ParkingSimulationModel _parkingSimulationModel;
        private CellType _selectedType = CellType.Empty;
        private ProgramState _programState = ProgramState.Constructor;
        private bool[,] _availableCells;

        public ParkingSimulationViewModel()
        {
            _parkingSimulationModel = IoC.GetModel<ParkingSimulationModel>();
            AsphaltCommand = new RelayCommand(() => SelectedType = CellType.Parking);
            ParkingSpaceCommand = new RelayCommand(() => SelectedType = CellType.ParkingSpace);
            EntryCommand = new RelayCommand(() => SelectedType = CellType.Entry);
            ExitCommand = new RelayCommand(() => SelectedType = CellType.Exit);
            int xl = 21;
            int yl = 20;
            //int xl = 500;
            //int yl = 500;
            _parkingSimulationModel.GenerateCells(xl, yl, 1, xl - 2, 1, yl - 1);
            _availableCells = new bool[Cells.GetLength(0), Cells.GetLength(1)];
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

        public bool AsphaltSelected => SelectedType == CellType.Parking;
        public bool ParkingSpaceSelected => SelectedType == CellType.ParkingSpace;
        public bool EntrySelected => SelectedType == CellType.Entry;
        public bool ExitSelected => SelectedType == CellType.Exit;

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

                CalculateAvailableCells();
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

        private void CalculateAvailableCells()
        {
            SetAvailable(false);
            switch (SelectedType)
            {
                case CellType.Entry:
                case CellType.Exit:
                    for (int i = 1; i < _availableCells.GetLength(0) - 1; i++)
                        _availableCells[i, 1] = true;
                    break;
                case CellType.Parking:
                case CellType.ParkingSpace:
                    SetAvailable(true);
                    break;
                default:
                    break;
            }
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
        }
    }
}
