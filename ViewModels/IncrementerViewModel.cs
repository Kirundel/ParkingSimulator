﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure;

namespace ViewModels
{
    public class IncrementerControlViewModel : ViewModelBase
    {
        private int _value;
        private Func<Task<bool>> _valueChanged;

        public IncrementerControlViewModel(string text, int value, int minValue, int maxValue, Func<Task<bool>> valueChanged, int step = 1)
        {
            MaxValue = maxValue;
            MinValue = minValue;
            Value = value;
            _valueChanged = valueChanged;
            IncreaseValue = new RelayCommand(() => SetValue(Value + step), () => Value + step <= MaxValue);
            DecreaseValue = new RelayCommand(() => SetValue(Value - step), () => Value - step >= MinValue);
            Text = text;
        }

        public int MinValue { get; } 
        public int MaxValue { get; }

        public string Text { get; }

        public int Value
        {
            get => _value;
            private set
            {
                _value = value;
                NotifyOfPropertyChanged(nameof(Value));
            }
        }

        public RelayCommand IncreaseValue { get; }
        public RelayCommand DecreaseValue { get; }

        private async void SetValue(int value)
        {
            var prevValue = _value;
            Value = value;
            if (!await _valueChanged())
                Value = prevValue;
        }
    }
}
