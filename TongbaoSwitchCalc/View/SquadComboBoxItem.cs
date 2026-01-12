using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc
{
    public class SquadComboBoxItem
    {
        public string Key { get; set; }
        public SquadType Value { get; set; }

        public SquadComboBoxItem(SquadType type)
        {
            Key = Define.GetSquadName(type);
            Value = type;
        }
    }
}
