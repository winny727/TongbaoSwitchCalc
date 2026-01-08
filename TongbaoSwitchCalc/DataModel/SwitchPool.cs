using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    internal static class SwitchPool
    {
        private static readonly Dictionary<int, List<int>> mSwitchOutPools = new Dictionary<int, List<int>>(); // <poolId, <tongbaoId>>

        internal static void SetupTongbaoSwitchPool(TongbaoConfig config)
        {
            if (config == null || config.SwitchOutPools == null)
            {
                return;
            }

            foreach (int poolId in config.SwitchOutPools)
            {
                if (poolId <= 0) continue; // 大于0的才有效
                if (!mSwitchOutPools.ContainsKey(poolId))
                {
                    mSwitchOutPools[poolId] = new List<int>();
                }
                mSwitchOutPools[poolId].Add(config.Id);
            }
        }

        internal static int SwitchTongbao(IRandomGenerator random, PlayerData playerData, Tongbao tongbao)
        {
            if (random == null || playerData == null || tongbao == null)
            {
                return -1;
            }

            int poolId = tongbao.SwitchInPool;
            if (poolId <= 0 || !mSwitchOutPools.ContainsKey(poolId))
            {
                return -1; // 不可交换
            }

            List<int> validTongbaoList = new List<int>();
            foreach (int tongbaoId in mSwitchOutPools[poolId])
            {
                if (!playerData.IsTongbaoExist(tongbaoId))
                {
                    validTongbaoList.Add(tongbaoId);
                }
            }

            // 随机
            int index = random.Next(0, validTongbaoList.Count);
            return validTongbaoList[index];
        }
    }
}
