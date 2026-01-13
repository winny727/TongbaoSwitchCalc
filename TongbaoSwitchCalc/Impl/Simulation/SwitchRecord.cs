using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public readonly struct SwitchRecord
    {
        public readonly int SlotIndex;
        public readonly int TongbaoIdBefore;
        public readonly int TongbaoIdAfter;
        public readonly ResType RandomResType;
        public readonly int RandomResCount;
    }
}
