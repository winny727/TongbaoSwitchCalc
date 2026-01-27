using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;

namespace TongbaoExchangeCalc.Impl
{
    public class RandomGenerator : IRandomGenerator
    {
        // 保证多线程下同一时间创建的随机数生成器是不同种子
        private readonly Random mRandom = new Random(Guid.NewGuid().GetHashCode());

        public int Next(int minValue, int maxValue)
        {
            return mRandom.Next(minValue, maxValue);
        }

        public double NextDouble()
        {
            return mRandom.NextDouble();
        }

        public object Clone()
        {
            return new RandomGenerator();
        }
    }
}
