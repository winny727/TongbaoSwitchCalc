using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.View
{
    public class ComboBoxItem<T>
    {
        public string Key { get; set; }
        public T Value { get; set; }

        public ComboBoxItem(string key, T value)
        {
            Key = key;
            Value = value;
        }
    }
}
