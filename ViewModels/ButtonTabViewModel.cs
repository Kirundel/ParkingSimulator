using Infrastructure;
using System;

namespace ViewModels
{
    public class ButtonTabViewModel : ViewModelBase
    {
        private bool _isSelected;

        public ButtonTabViewModel(bool isSelected, string text, ProgramState state, Action<ProgramState> selectedAction)
        {
            IsSelected = isSelected;
            Text = text;
            State = state;
            ClickCommand = new RelayCommand(() => selectedAction(State));
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string Text { get; }
        public ProgramState State { get; }
        public RelayCommand ClickCommand { get; }
    }
}
