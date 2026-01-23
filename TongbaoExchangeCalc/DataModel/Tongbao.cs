using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel
{
    [Serializable]
    public class TongbaoConfig
    {
        public int Id;
        public string Name;
        public string Description;
        public string ImgPath;
        public TongbaoType Type;
        public int ExchangeInPool; //交换前池子ID
        public List<int> ExchangeOutPools; //交换后池子ID列表
        public bool IsUpgrade; //是否升级通宝
        public ResType ExtraResType; //通宝自带效果
        public int ExtraResCount;

        private static readonly Dictionary<int, TongbaoConfig> mTongbaoConfigDict = new Dictionary<int, TongbaoConfig>();

        public static void AddTongbaoConfig(TongbaoConfig config)
        {
            if (config == null)
            {
                return;
            }

            mTongbaoConfigDict[config.Id] = config;
            ExchangePool.SetupTongbaoExchangePool(config);
        }

        public static TongbaoConfig GetTongbaoConfigById(int id)
        {
            if (mTongbaoConfigDict.ContainsKey(id))
            {
                return mTongbaoConfigDict[id];
            }
            return null;
        }

        public static void ClearTongbaoConfig()
        {
            mTongbaoConfigDict.Clear();
            ExchangePool.Clear();
        }

        public static IReadOnlyDictionary<int, TongbaoConfig> GetAllTongbaoConfigs()
        {
            return mTongbaoConfigDict;
        }
    }

    public class Tongbao
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string ImgPath { get; private set; }
        public TongbaoType Type { get; private set; }
        public int ExchangeInPool { get; private set; } //交换前池子ID
        public List<int> ExchangeOutPools { get; private set; } //交换后池子ID列表
        public bool IsUpgrade { get; private set; } //是否升级通宝
        public ResType ExtraResType { get; private set; } //通宝自带效果
        public int ExtraResCount { get; private set; }
        public ResType RandomResType { get; private set; } //品相效果
        public int RandomResCount { get; private set; }

        public bool CanExchange()
        {
            return ExchangeInPool > 0;
        }

        // 不传IRandomGenerator则不生成随机品相效果
        public static void InitTongbao(ref Tongbao tongbao, int id, IRandomGenerator random = null)
        {
            if (tongbao == null)
            {
                return;
            }

            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config == null)
            {
                return;
            }

            tongbao.Id = config.Id;
            tongbao.Name = config.Name;
            tongbao.Description = config.Description;
            tongbao.ImgPath = config.ImgPath;
            tongbao.Type = config.Type;
            tongbao.ExchangeInPool = config.ExchangeInPool;
            tongbao.ExchangeOutPools = config.ExchangeOutPools;
            tongbao.IsUpgrade = config.IsUpgrade;
            tongbao.ExtraResType = config.ExtraResType;
            tongbao.ExtraResCount = config.ExtraResCount;
            tongbao.SetupRandomRes(random);
        }

        public void ApplyRandomRes(ResType resType, int recCount)
        {
            RandomResType = resType;
            RandomResCount = recCount;
        }

        private void SetupRandomRes(IRandomGenerator random)
        {
            ApplyRandomRes(ResType.None, 0);
            if (random == null)
            {
                return;
            }

            float randomValue = (float)random.NextDouble();
            float cumulativeProbability = 0f;
            RandomResDefine randomRes = null;
            for (int i = 0; i < Define.RandomResDefines.Count; i++)
            {
                var define = Define.RandomResDefines[i];
                cumulativeProbability += define.Probability;
                if (cumulativeProbability > randomValue)
                {
                    randomRes = define;
                    break;
                }
            }
            if (randomRes != null)
            {
                ApplyRandomRes(randomRes.ResType, randomRes.ResCount);
            }
        }
    }
}
