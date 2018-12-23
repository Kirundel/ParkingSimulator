using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Input;

namespace Infrastructure
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canChangeValue;

        public RelayCommand(Action execute, Func<bool> canChangeValue = null)
        {
            _execute = execute;
            if (canChangeValue == null)
                canChangeValue = () => true;
            _canChangeValue = canChangeValue;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canChangeValue();
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke();
        }
    }
}
