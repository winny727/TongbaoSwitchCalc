using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.View
{
    public class ComboBoxItem<T>
    {
        public string Key { get; set; }
        public T Value { get; set; }
        public object[] Args { get; set; }

        public ComboBoxItem(string key, T value, params object[] args)
        {
            Key = key;
            Value = value;
            Args = args;
        }
    }
}
