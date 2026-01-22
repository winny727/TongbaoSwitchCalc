using System;
using System.Collections.Generic;
using System.Threading;

namespace TongbaoSwitchCalc.Impl
{
    public class ThreadSafeRandomGenerator : RandomGenerator
    {
        private readonly ThreadLocal<Random> mRandom = new ThreadLocal<Random>(() => new Random());

        public override int Next(int minValue, int maxValue)
        {
            return mRandom.Value.Next(minValue, maxValue);
        }

        public override double NextDouble()
        {
            return mRandom.Value.NextDouble();
        }
    }
}
