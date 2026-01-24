using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel
{
    public class PlayerData : IReadOnlyPlayerData
    {
        public ITongbaoSelector TongbaoSelector { get; private set; }
        public IRandomGenerator Random { get; private set; }

        private SquadDefine mSquadDefine;
        private readonly Dictionary<ResType, int> mResValues = new Dictionary<ResType, int>(); // 资源数值

        public IReadOnlyDictionary<ResType, int> ResValues => mResValues; // 资源数值只读接口
        public Tongbao[] TongbaoBox { get; private set; } // 钱盒
        public SquadType SquadType { get; private set; } // 分队类型
        public int ExchangeCount { get; set; } // 已交换次数
        public SpecialConditionFlag SpecialConditionFlag { get; set; } // 特殊条件，如福祸相依（交换后的通宝如果是厉钱，则获得票券+1）
        public List<int> LockedTongbaoList { get; private set; } = new List<int>(); // 商店锁定的通宝ID列表
        public int MaxTongbaoCount => mSquadDefine.MaxTongbaoCount;
        public int NextExchangeCostLifePoint => mSquadDefine.GetCostLifePoint(ExchangeCount);
        public bool HasEnoughExchangeLife => GetResValue(ResType.LifePoint) > NextExchangeCostLifePoint;

        private readonly List<int> mTempExchangeResults = new List<int>();
        private readonly ObjectPool<Tongbao> mTongbaoPool = new ObjectPool<Tongbao>(10, 16);

        public PlayerData(ITongbaoSelector selector, IRandomGenerator random)
        {
            TongbaoSelector = selector ?? throw new ArgumentNullException(nameof(selector));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Init(default);
        }

        public void Init(SquadType squadType, Dictionary<ResType, int> resValues = null)
        {
            if (!Define.SquadDefines.ContainsKey(squadType))
            {
                throw new ArgumentException($"Unknown SquadType：{squadType}", nameof(squadType));
            }

            ExchangeCount = 0;
            SquadType = squadType;
            mSquadDefine = Define.SquadDefines[squadType]; //只读Dictionary线程安全
            SpecialConditionFlag = SpecialConditionFlag.None;
            LockedTongbaoList.Clear();

            ClearTongbao();
            TongbaoBox = new Tongbao[MaxTongbaoCount];

            InitResValues(resValues);

            if (GetResValue(ResType.LifePoint) <= 0)
            {
                SetResValue(ResType.LifePoint, 1); // 默认至少1血
            }
        }

        public void InitResValues(IReadOnlyDictionary<ResType, int> resValues)
        {
            mResValues.Clear();
            if (resValues != null)
            {
                foreach (var item in resValues)
                {
                    mResValues.Add(item.Key, item.Value);
                }
            }
        }

        public void SetSquadType(SquadType squadType)
        {
            if (!Define.SquadDefines.ContainsKey(squadType))
            {
                throw new ArgumentException($"Unknown SquadType：{squadType}", nameof(squadType));
            }

            SquadType = squadType;
            mSquadDefine = Define.SquadDefines[squadType];
            if (MaxTongbaoCount != TongbaoBox.Length)
            {
                Tongbao[] newTongbaoBox = new Tongbao[MaxTongbaoCount];

                if (TongbaoBox != null)
                {
                    for (int i = 0; i < TongbaoBox.Length && i < newTongbaoBox.Length; i++)
                    {
                        newTongbaoBox[i] = TongbaoBox[i];
                    }
                    for (int i = newTongbaoBox.Length; i < TongbaoBox.Length; i++)
                    {
                        DestroyTongbao(TongbaoBox[i]);
                    }
                }
                TongbaoBox = newTongbaoBox;
            }
        }

        public void CopyFrom(PlayerData playerData, bool isIgnoreSetting = false)
        {
            if (playerData == null)
            {
                Init(default);
                return;
            }
            if (!isIgnoreSetting)
            {
                // 可选不重置Setting类的数据
                SquadType = playerData.SquadType;
                mSquadDefine = Define.SquadDefines[SquadType];
                SpecialConditionFlag = playerData.SpecialConditionFlag;
                LockedTongbaoList.Clear();
                LockedTongbaoList.AddRange(playerData.LockedTongbaoList);
            }
            ExchangeCount = playerData.ExchangeCount;
            InitResValues(playerData.mResValues);
            ClearTongbao();
            if (TongbaoBox.Length != MaxTongbaoCount)
            {
                TongbaoBox = new Tongbao[MaxTongbaoCount];
            }
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (i < playerData.TongbaoBox.Length)
                {
                    Tongbao tongbao = playerData.TongbaoBox[i];
                    if (tongbao != null)
                    {
                        TongbaoBox[i] = CreateTongbao(tongbao.Id);
                        TongbaoBox[i].ApplyRandomRes(tongbao.RandomResType, tongbao.RandomResCount);
                    }
                }
            }
        }

        // 不传IRandomGenerator则不生成随机品相效果
        public Tongbao CreateTongbao(int id, IRandomGenerator random = null)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config == null)
            {
                return null;
            }

            Tongbao tongbao = mTongbaoPool.Allocate();
            Tongbao.InitTongbao(ref tongbao, id, random);

            return tongbao;
        }

        public void DestroyTongbao(Tongbao tongbao)
        {
            if (tongbao != null)
            {
                mTongbaoPool.Recycle(tongbao, true);
            }
        }

        public bool IsTongbaoFull()
        {
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        // 外部禁止持有，会回收
        public Tongbao GetTongbao(int slotIndex)
        {
            if (TongbaoBox == null)
            {
                return null;
            }
            if (slotIndex >= 0 && slotIndex < TongbaoBox.Length)
            {
                return TongbaoBox[slotIndex];
            }
            return null;
        }

        public void AddTongbao(Tongbao tongbao)
        {
            if (TongbaoBox == null)
            {
                return;
            }

            if (IsTongbaoFull())
            {
                return;
            }

            int slotIndex = -1;
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] == null)
                {
                    slotIndex = i;
                    break;
                }
            }

            InsertTongbao(tongbao, slotIndex);
        }

        public void InsertTongbao(Tongbao tongbao, int slotIndex)
        {
            if (TongbaoBox == null)
            {
                return;
            }

            if (tongbao == null)
            {
                return;
            }

            if (slotIndex >= 0 && slotIndex < TongbaoBox.Length)
            {
                DestroyTongbao(TongbaoBox[slotIndex]);
                TongbaoBox[slotIndex] = tongbao;
                // 添加通宝自带效果
                if (tongbao.ExtraResType != ResType.None && tongbao.ExtraResCount > 0)
                {
                    AddResValue(tongbao.ExtraResType, tongbao.ExtraResCount);
                }
                // 添加通宝品相效果
                if (tongbao.RandomResType != ResType.None && tongbao.RandomResCount > 0)
                {
                    AddResValue(tongbao.RandomResType, tongbao.RandomResCount);
                }
                // 福祸相依
                if (HasSpecialCondition(SpecialConditionFlag.Collectible_Fortune))
                {
                    if (tongbao.Type == TongbaoType.Risk)
                    {
                        AddResValue(ResType.Coupon, 1);
                    }
                }
            }
        }

        public void RemoveTongbaoAt(int slotIndex)
        {
            if (TongbaoBox == null)
            {
                return;
            }

            if (slotIndex >= 0 && slotIndex < TongbaoBox.Length)
            {
                DestroyTongbao(TongbaoBox[slotIndex]);
                TongbaoBox[slotIndex] = null;
            }
        }

        public void RemoveTongbao(Tongbao tongbao)
        {
            if (TongbaoBox == null)
            {
                return;
            }

            if (tongbao == null)
            {
                return;
            }

            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] != null && TongbaoBox[i].Id == tongbao.Id)
                {
                    DestroyTongbao(TongbaoBox[i]);
                    TongbaoBox[i] = null;
                    return;
                }
            }
        }

        public void ClearTongbao()
        {
            if (TongbaoBox == null)
            {
                return;
            }

            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                DestroyTongbao(TongbaoBox[i]);
                TongbaoBox[i] = null;
            }
        }

        public bool IsTongbaoExist(int id)
        {
            if (TongbaoBox == null)
            {
                return false;
            }

            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] != null && TongbaoBox[i].Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsTongbaoLocked(int id)
        {
            return LockedTongbaoList.Contains(id);
        }

        public void AddResValue(ResType type, int value)
        {
            if (type != ResType.None)
            {
                if (!mResValues.ContainsKey(type))
                {
                    mResValues.Add(type, 0);
                }
                mResValues[type] += value;
                if (Define.ParentResType.TryGetValue(type, out var parentResType))
                {
                    AddResValue(parentResType, value);
                }
            }
        }

        public void SetResValue(ResType type, int value)
        {
            if (type != ResType.None)
            {
                if (!mResValues.ContainsKey(type))
                {
                    mResValues.Add(type, 0);
                }
                int changedValue = value - mResValues[type];
                mResValues[type] += changedValue;
                if (Define.ParentResType.TryGetValue(type, out var parentResType))
                {
                    AddResValue(parentResType, changedValue);
                }
            }
        }

        public int GetResValue(ResType type)
        {
            if (type == ResType.None)
            {
                return 0;
            }

            if (mResValues.TryGetValue(type, out var value))
            {
                return value;
            }

            return 0;
        }

        public void SetSpecialCondition(SpecialConditionFlag specialCondition, bool enabled)
        {
            SpecialConditionFlag = enabled
                ? SpecialConditionFlag | specialCondition
                : SpecialConditionFlag & ~specialCondition;
        }

        public bool HasSpecialCondition(SpecialConditionFlag specialCondition)
        {
            return (SpecialConditionFlag & specialCondition) != 0;
        }

        public bool ExchangeTongbao(int slotIndex, bool force = false)
        {
            Tongbao tongbao = GetTongbao(slotIndex);
            if (tongbao == null)
            {
                return false;
            }

            if (!tongbao.CanExchange())
            {
                return false;
            }

            int costLifePoint = NextExchangeCostLifePoint;
            int lifePoint = GetResValue(ResType.LifePoint);
            if (lifePoint > costLifePoint || force)
            {
                ExchangePool.ExchangeTongbao(Random, this, tongbao, mTempExchangeResults);
                int newTongbaoId = TongbaoSelector.SelectTongbao(mTempExchangeResults);
                Tongbao newTongbao;
                if (tongbao.IsUpgrade)
                {
                    // 升级通宝保留品相重复触发一次
                    newTongbao = CreateTongbao(newTongbaoId);
                    newTongbao.ApplyRandomRes(tongbao.RandomResType, tongbao.RandomResCount);
                }
                else
                {
                    newTongbao = CreateTongbao(newTongbaoId, Random);
                }
                if (newTongbao != null)
                {
                    InsertTongbao(newTongbao, slotIndex);

                    ExchangeCount++;
                    AddResValue(ResType.LifePoint, -costLifePoint);

                    return true;
                }
            }
            return false;
        }
    }
}
