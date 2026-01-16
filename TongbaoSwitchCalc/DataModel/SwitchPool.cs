using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    internal static class SwitchPool
    {
        private static readonly Dictionary<int, List<int>> mSwitchOutPools = new Dictionary<int, List<int>>(); // <poolId, <out tongbaoId>>
        private static readonly List<int> mValidTongbaoTempList = new List<int>();

        internal static void SetupTongbaoSwitchPool(TongbaoConfig config)
        {
            if (config == null || config.SwitchOutPools == null)
            {
                return;
            }

            foreach (var poolId in config.SwitchOutPools)
            {
                if (poolId <= 0) continue; // 大于0的才有效
                if (!mSwitchOutPools.ContainsKey(poolId))
                {
                    mSwitchOutPools[poolId] = new List<int>();
                }
                mSwitchOutPools[poolId].Add(config.Id);
            }
        }

        internal static void Clear()
        {
            mSwitchOutPools.Clear();
            mValidTongbaoTempList.Clear();
        }

        internal static IReadOnlyList<int> GetSwitchOutTongbaoIds(int id)
        {
            if (mSwitchOutPools.TryGetValue(id, out var tongbaoIds))
            {
                return tongbaoIds;
            }
            return null;
        }

        internal static IReadOnlyList<int> SwitchTongbao(IRandomGenerator random, PlayerData playerData, Tongbao tongbao)
        {
            mValidTongbaoTempList.Clear();
            if (random == null || playerData == null || tongbao == null)
            {
                return mValidTongbaoTempList;
            }

            int poolId = tongbao.SwitchInPool;
            if (poolId <= 0 || !mSwitchOutPools.ContainsKey(poolId))
            {
                return mValidTongbaoTempList; // 不可交换
            }

            foreach (var tongbaoId in mSwitchOutPools[poolId])
            {
                TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(tongbaoId);
                if (config == null)
                {
                    continue;
                }

                // 排除钱盒里的（除了自身）
                if (playerData.IsTongbaoExist(tongbaoId) && tongbaoId != tongbao.Id)
                {
                    continue;
                }

                // 排除钱盒里该通宝升级后的通宝
                if (config.IsUpgrade)
                {
                    bool isExistUpgrade = false;
                    foreach (var upgradeTongbaoId in mSwitchOutPools[config.SwitchInPool])
                    {
                        if (playerData.IsTongbaoExist(upgradeTongbaoId))
                        {
                            isExistUpgrade = true;
                            break;
                        }
                    }
                    if (isExistUpgrade)
                    {
                        continue;
                    }
                }

                // 排除商店锁定的
                if (playerData.IsTongbaoLocked(tongbaoId))
                {
                    continue;
                }

                mValidTongbaoTempList.Add(tongbaoId);
            }

            if (mValidTongbaoTempList.Count <= 0)
            {
                return mValidTongbaoTempList; // 无可交换通宝
            }

            if (tongbao.IsUpgrade)
            {
                return mValidTongbaoTempList;
            }
            else
            {
                // 随机
                int index = random.Next(0, mValidTongbaoTempList.Count);
                int result = mValidTongbaoTempList[index];
                mValidTongbaoTempList.Clear();
                mValidTongbaoTempList.Add(result);
                return mValidTongbaoTempList;
            }
        }
    }
}
