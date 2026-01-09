using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TongbaoSwitchCalc.DataModel
{
    public class PlayerData
    {
        private readonly IRandomGenerator mRandom;
        private readonly Dictionary<ResType, int> mResValues = new Dictionary<ResType, int>(); // 资源数值

        public Tongbao[] TongbaoBox { get; private set; }
        public SquadType SquadType { get; private set; }
        private SquadDefine mSquadDefine;
        public int SwitchCount { get; set; } // 已交换次数
        public int MaxTongbaoCount { get; private set; } // 最大通宝数量
        public SpecialConditionFlag SpecialConditionFlag { get; set; } // 福祸相依（交换后的通宝如果是厉钱，则获得票券+1）

        public event Action<ResType, int> OnResValueChanged;

        public PlayerData(IRandomGenerator random)
        {
            mRandom = random ?? throw new ArgumentNullException(nameof(random));
            Init(default);
        }

        public void Init(SquadType squadType, Dictionary<ResType, int> resValues = null)
        {
            SquadType = squadType;
            mSquadDefine = Define.SquadDefines[squadType];
            SwitchCount = 0;
            MaxTongbaoCount = mSquadDefine.MaxTongbaoCount;
            TongbaoBox = new Tongbao[MaxTongbaoCount];

            mResValues.Clear();
            if (resValues != null)
            {
                foreach (var item in resValues)
                {
                    mResValues.Add(item.Key, item.Value);
                    OnResValueChanged?.Invoke(item.Key, item.Value);
                }
            }

            if (GetResValue(ResType.LifePoint) <= 0)
            {
                SetResValue(ResType.LifePoint, 1); // 默认1血
            }
        }

        public void SetSquadType(SquadType squadType)
        {
            SquadType = squadType;
            mSquadDefine = Define.SquadDefines[squadType];
            SwitchCount = 0;
            if (mSquadDefine.MaxTongbaoCount != MaxTongbaoCount)
            {
                MaxTongbaoCount = mSquadDefine.MaxTongbaoCount;
                Tongbao[] newTongbaoBox = new Tongbao[MaxTongbaoCount];

                for (int i = 0; i < TongbaoBox.Length && i < newTongbaoBox.Length; i++)
                {
                    newTongbaoBox[i] = TongbaoBox[i];
                }
                TongbaoBox = newTongbaoBox;
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

        public Tongbao GetTongbao(int posIndex)
        {
            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                return TongbaoBox[posIndex];
            }
            return null;
        }

        public void AddTongbao(Tongbao tongbao)
        {
            if (IsTongbaoFull())
            {
                return;
            }

            int posIndex = -1;
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] == null)
                {
                    posIndex = i;
                    break;
                }
            }

            InsertTongbao(tongbao, posIndex);
        }

        public void InsertTongbao(Tongbao tongbao, int posIndex)
        {
            if (tongbao == null)
            {
                return;
            }

            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                TongbaoBox[posIndex] = tongbao;
                if (tongbao.ExtraResType != ResType.None && tongbao.ExtraResCount > 0)
                {
                    AddResValue(tongbao.ExtraResType, tongbao.ExtraResCount);
                }
            }
        }

        public void RemoveTongbaoAt(int posIndex)
        {
            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                TongbaoBox[posIndex] = null;
            }
        }

        public void RemoveTongbao(Tongbao tongbao)
        {
            if (tongbao == null)
            {
                return;
            }

            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] != null && TongbaoBox[i].Id == tongbao.Id)
                {
                    TongbaoBox[i] = null;
                    return;
                }
            }
        }

        public bool IsTongbaoExist(int id)
        {
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                if (TongbaoBox[i] != null && TongbaoBox[i].Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        public void ClearTongbao()
        {
            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                TongbaoBox[i] = null;
            }
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
                OnResValueChanged?.Invoke(type, mResValues[type]);
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
                mResValues[type] = value;
                OnResValueChanged?.Invoke(type, value);
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

        public bool HasEnoughSwitchLife()
        {
            int costLifePoint = mSquadDefine.GetCostLifePoint(SwitchCount);
            return GetResValue(ResType.LifePoint) > costLifePoint;
        }

        public bool SwitchTongbao(int posIndex, bool force = false)
        {
            if (mSquadDefine == null)
            {
                return false;
            }

            Tongbao tongbao = GetTongbao(posIndex);
            if (tongbao == null || !tongbao.CanSwitch())
            {
                // 当前通宝不可交换
                return false;
            }

            int costLifePoint = mSquadDefine.GetCostLifePoint(SwitchCount);
            if (GetResValue(ResType.LifePoint) > costLifePoint || force)
            {
                int newTongbaoId = SwitchPool.SwitchTongbao(mRandom, this, tongbao);
                Tongbao newTongbao = Tongbao.CreateTongbao(newTongbaoId, mRandom);
                if (newTongbao != null)
                {
                    InsertTongbao(newTongbao, posIndex);
                    SwitchCount++;
                    AddResValue(ResType.LifePoint, -costLifePoint);
                    AddResValue(newTongbao.RandomResType, newTongbao.RandomResCount);

                    // 福祸相依
                    if (HasSpecialCondition(SpecialConditionFlag.Collectible_Fortune))
                    {
                        if (newTongbao.Type == TongbaoType.Risk)
                        {
                            AddResValue(ResType.Coupon, 1);
                        }
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
