using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl
{
    public class RandomGenerator : IRandomGenerator
    {
        private Random mRandom = new Random();

        public virtual int Next(int minValue, int maxValue)
        {
            return mRandom.Next(minValue, maxValue);
        }

        public virtual double NextDouble()
        {
            return mRandom.NextDouble();
        }

        public virtual void SetSeed(int seed)
        {
            mRandom = new Random(seed);
        }
    }
}
