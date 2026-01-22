using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.Impl
{
    public class LockThreadSafeRandomGenerator : RandomGenerator
    {
        private readonly Random mRandom = new Random();
        private readonly object mLock = new Random();

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
    }
}
