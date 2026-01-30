using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel
{
    public static class ExchangePool
    {
        private static readonly Dictionary<int, List<int>> mExchangeOutPools = new Dictionary<int, List<int>>(); // <poolId, <out tongbaoId>>
        private static readonly Dictionary<int, List<int>> mMutexTongbaoIds = new Dictionary<int, List<int>>(); // <poolId, <out tongbaoId>>

        internal static void SetupTongbaoConfig(TongbaoConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (config.MutexGroup > 0)
            {
                if (!mMutexTongbaoIds.ContainsKey(config.MutexGroup))
                {
                    mMutexTongbaoIds[config.MutexGroup] = new List<int>();
                }
                mMutexTongbaoIds[config.MutexGroup].Add(config.Id);
            }

            if (config.ExchangeOutPools == null)
            {
                return;
            }

            foreach (var poolId in config.ExchangeOutPools)
            {
                if (poolId <= 0) continue; // 大于0的才有效
                if (!mExchangeOutPools.ContainsKey(poolId))
                {
                    mExchangeOutPools[poolId] = new List<int>();
                }
                mExchangeOutPools[poolId].Add(config.Id);
            }
        }

        internal static void Clear()
        {
            mExchangeOutPools.Clear();
        }

        public static IReadOnlyList<int> GetExchangeOutTongbaoIds(int poolId)
        {
            if (mExchangeOutPools.TryGetValue(poolId, out var tongbaoIds))
            {
                return tongbaoIds;
            }
            return null;
        }

        public static IReadOnlyList<int> GetMutexTongbaoIds(int mutexId)
        {
            if (mMutexTongbaoIds.TryGetValue(mutexId, out var tongbaoIds))
            {
                return tongbaoIds;
            }
            return null;
        }

        internal static void ExchangeTongbao(IRandomGenerator random, PlayerData playerData, Tongbao tongbao, List<int> outResults)
        {
            if (outResults == null)
            {
                return;
            }

            outResults.Clear();
            if (random == null || playerData == null || tongbao == null)
            {
                return;
            }

            int poolId = tongbao.ExchangeInPool;
            if (poolId <= 0 || !mExchangeOutPools.ContainsKey(poolId))
            {
                return; // 不可交换
            }

            for (int i = 0; i < mExchangeOutPools[poolId].Count; i++)
            {
                int tongbaoId = mExchangeOutPools[poolId][i];
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

                // 排除钱盒里的互斥通宝
                if (config.MutexGroup > 0)
                {
                    bool isMutexExist = false;
                    var mutexTongbaoIds = GetMutexTongbaoIds(config.MutexGroup);
                    for (int j = 0; j < mutexTongbaoIds.Count; j++)
                    {
                        int mutexTongbaoId = mutexTongbaoIds[j];
                        if (mutexTongbaoId == tongbao.Id || mutexTongbaoId == tongbaoId)
                        {
                            continue;
                        }
                        if (playerData.IsTongbaoExist(mutexTongbaoId))
                        {
                            isMutexExist = true;
                            break;
                        }
                    }
                    if (isMutexExist)
                    {
                        continue;
                    }
                }

                // 排除商店锁定的
                if (playerData.IsTongbaoLocked(tongbaoId))
                {
                    continue;
                }

                outResults.Add(tongbaoId);
            }

            if (outResults.Count <= 0)
            {
                return; // 无可交换通宝
            }

            if (tongbao.IsUpgrade)
            {
                return;
            }
            else
            {
                // 随机
                int index = random.Next(0, outResults.Count);
                int result = outResults[index];
                outResults.Clear();
                outResults.Add(result);
                return;
            }
        }
    }
}
