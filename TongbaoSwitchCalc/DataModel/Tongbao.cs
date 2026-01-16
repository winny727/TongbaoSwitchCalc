using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    [Serializable]
    public class TongbaoConfig
    {
        public int Id;
        public string Name;
        public string Description;
        public string ImgPath;
        public TongbaoType Type;
        public int SwitchInPool; //交换前池子ID
        public List<int> SwitchOutPools; //交换后池子ID列表
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
            SwitchPool.Clear();
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
        public int SwitchInPool { get; private set; } //交换前池子ID
        public List<int> SwitchOutPools { get; private set; } //交换后池子ID列表
        public bool IsUpgrade { get; private set; } //是否升级通宝
        public ResType ExtraResType { get; private set; } //通宝自带效果
        public int ExtraResCount { get; private set; }
        public ResType RandomResType { get; private set; } //品相效果
        public int RandomResCount { get; private set; }

        // ConcurrentDictionary保证线程安全+回收时不重复
        private static readonly ConcurrentDictionary<Tongbao, bool> mPool = new ConcurrentDictionary<Tongbao, bool>();

        //线程安全
        public static Tongbao Allocate()
        {
            if (TryPop(out var result))
            {
                return result;
            }
            return new Tongbao();
        }

        private static bool TryPop(out Tongbao result)
        {
            result = default;

            // 没有栈顶元素可弹出
            if (mPool.IsEmpty)
                return false;

            // 获取任意元素，类似栈操作
            foreach (var key in mPool.Keys)
            {
                if (mPool.TryRemove(key, out _))
                {
                    result = key;
                    return true;
                }
            }

            return false;
        }

        //线程安全
        public void Recycle()
        {
            mPool.TryAdd(this, true);
        }

        public void CopyFrom(Tongbao tongbao)
        {
            if (tongbao == null)
            {
                Id = default;
                Name = default;
                Description = default;
                ImgPath = default;
                Type = default;
                SwitchInPool = default;
                SwitchOutPools = default;
                ExtraResType = default;
                ExtraResCount = default;
                RandomResType = default;
                RandomResCount = default;
                return;
            }

            Id = tongbao.Id;
            Name = tongbao.Name;
            Description = tongbao.Description;
            ImgPath = tongbao.ImgPath;
            Type = tongbao.Type;
            SwitchInPool = tongbao.SwitchInPool;
            SwitchOutPools = tongbao.SwitchOutPools;
            ExtraResType = tongbao.ExtraResType;
            ExtraResCount = tongbao.ExtraResCount;
            RandomResType = tongbao.RandomResType;
            RandomResCount = tongbao.RandomResCount;
        }

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

            Tongbao tongbao = Allocate();

            tongbao.Id = config.Id;
            tongbao.Name = config.Name;
            tongbao.Description = config.Description;
            tongbao.ImgPath = config.ImgPath;
            tongbao.Type = config.Type;
            tongbao.SwitchInPool = config.SwitchInPool;
            tongbao.SwitchOutPools = config.SwitchOutPools;
            tongbao.IsUpgrade = config.IsUpgrade;
            tongbao.ExtraResType = config.ExtraResType;
            tongbao.ExtraResCount = config.ExtraResCount;
            tongbao.SetupRandomRes(random);

            return tongbao;
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
}
