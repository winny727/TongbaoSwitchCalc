using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TongbaoSwitchCalc.DataModel
{
    [Serializable]
    public class TongbaoConfig
    {
        public int Id;
        public string Name;
        public string Description;
        public TongbaoType Type;
        public int MaxDuplicates; //最大重复数量，-1表示无限制
        public int SwitchInPool; //交换前池子ID
        public List<int> SwitchOutPools; //交换后池子ID列表
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
            SwitchPool.SetupTongbaoSwitchPool(config);
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
        }

        public static Dictionary<int, TongbaoConfig> GetAllTongbaoConfigs()
        {
            return mTongbaoConfigDict;
        }
    }

    public class Tongbao
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public TongbaoType Type { get; private set; }
        public int MaxDuplicates { get; private set; } //最大重复数量，-1表示无限制
        public int SwitchInPool { get; private set; } //交换前池子ID
        public List<int> SwitchOutPools { get; private set; } //交换后池子ID列表
        public ResType ExtraResType { get; private set; } //通宝自带效果
        public int ExtraResCount { get; private set; }
        public ResType RandomResType { get; private set; } //品相效果
        public int RandomResCount { get; private set; }

        public bool CanSwitch()
        {
            return SwitchInPool > 0;
        }

        // 不传IRandomGenerator则不生成随机品相效果
        public static Tongbao CreateTongbao(int id, IRandomGenerator random = null)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config == null)
            {
                return null;
            }

            Tongbao tongbao = new Tongbao
            {
                Id = config.Id,
                Name = config.Name,
                Description = config.Description,
                Type = config.Type,
                MaxDuplicates = config.MaxDuplicates,
                SwitchInPool = config.SwitchInPool,
                SwitchOutPools = config.SwitchOutPools,
                ExtraResType = config.ExtraResType,
                ExtraResCount = config.ExtraResCount,
            };

            if (random != null)
            {
                tongbao.SetupRandomRes(random);
            }

            return tongbao;
        }

        public void ApplyRandomRes(ResType resType, int recCount)
        {
            RandomResType = resType;
            RandomResCount = recCount;
        }

        private void SetupRandomRes(IRandomGenerator random)
        {
            if (random == null)
            {
                return;
            }

            float randomValue = (float)random.NextDouble();
            float cumulativeProbability = 0f;
            RandomRes randomRes = null;
            foreach (var item in Define.RandomResDefines)
            {
                cumulativeProbability += item.Probability;
                if (cumulativeProbability > randomValue)
                {
                    randomRes = item;
                    break;
                }
            }
            if (randomRes != null)
            {
                ApplyRandomRes(randomRes.ResType, randomRes.ResCount);
            }
        }
    }

    public enum TongbaoType
    {
        Unknown = 0,
        Balance = 1, //衡钱
        Flower = 2, //花钱
        Risk = 3, //厉钱
    }
}
