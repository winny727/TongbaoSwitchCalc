using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl
{
    public class RandomGenerator : IRandomGenerator
    {
        private readonly Random mRandom = new Random();

        public virtual int Next(int minValue, int maxValue)
        {
            return mRandom.Next(minValue, maxValue);
        }

        public virtual double NextDouble()
        {
            return mRandom.NextDouble();
        }
    }
}
