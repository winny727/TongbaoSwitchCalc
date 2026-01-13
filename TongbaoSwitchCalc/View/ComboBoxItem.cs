using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc
{
    public class ComboBoxItem<T>
    {
        public string Key { get; set; }
        public T Value { get; set; }

        public ComboBoxItem(string key, T type)
        {
            Key = key;
            Value = type;
        }
    }
}
