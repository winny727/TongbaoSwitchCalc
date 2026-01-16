using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl
{
    public class ThreadSafeRandomGenerator : RandomGenerator
    {
        private Random mRandom = new Random();
        private object mLock = new Random();

        public override int Next(int minValue, int maxValue)
        {
            lock (mLock)
            {
                return mRandom.Next(minValue, maxValue);
            }
        }

        public override double NextDouble()
        {
            lock (mLock)
            {
                return mRandom.NextDouble();
            }
        }

        public override void SetSeed(int seed)
        {
            lock (mLock)
            {
                mRandom = new Random(seed);
            }
        }
    }
}
