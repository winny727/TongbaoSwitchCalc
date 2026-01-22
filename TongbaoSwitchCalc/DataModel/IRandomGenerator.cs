using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    public interface IRandomGenerator
    {
        int Next(int minValue, int maxValue); // [minValue, maxValue)
        double NextDouble();
    }
}
