using System;
using System.Collections.Generic;
using System.Text;
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

        private readonly StringBuilder mSwitchResultSB = new StringBuilder();
        private readonly StringBuilder mResChangedTempSB = new StringBuilder();
        public string LastSwitchResult => mSwitchResultSB.ToString();

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

            ClearTongbao();
            TongbaoBox = new Tongbao[MaxTongbaoCount];

            mSwitchResultSB.Clear();
            mResChangedTempSB.Clear();
            InitResValues(resValues);

            if (GetResValue(ResType.LifePoint) <= 0)
            {
                SetResValue(ResType.LifePoint, 1); // 默认1血
            }
        }

        public void InitResValues(Dictionary<ResType, int> resValues)
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
            SquadType = squadType;
            mSquadDefine = Define.SquadDefines[squadType];
            if (mSquadDefine.MaxTongbaoCount != MaxTongbaoCount)
            {
                MaxTongbaoCount = mSquadDefine.MaxTongbaoCount;
                Tongbao[] newTongbaoBox = new Tongbao[MaxTongbaoCount];

                if (TongbaoBox != null)
                {
                    for (int i = 0; i < TongbaoBox.Length && i < newTongbaoBox.Length; i++)
                    {
                        newTongbaoBox[i] = TongbaoBox[i];
                    }
                    for (int i = newTongbaoBox.Length; i < TongbaoBox.Length; i++)
                    {
                        TongbaoBox[i]?.Recycle();
                    }
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
            if (TongbaoBox == null)
            {
                return null;
            }
            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                return TongbaoBox[posIndex];
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
            if (TongbaoBox == null)
            {
                return;
            }

            if (tongbao == null)
            {
                return;
            }

            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                TongbaoBox[posIndex]?.Recycle();
                TongbaoBox[posIndex] = tongbao;
                // 添加通宝自带效果
                if (tongbao.ExtraResType != ResType.None && tongbao.ExtraResCount > 0)
                {
                    mResChangedTempSB.Append("[")
                                     .Append(tongbao.Name)
                                     .Append("]效果");
                    OnSwitchResValueChanged(tongbao.ExtraResType, tongbao.ExtraResCount);
                }
                // 添加通宝品相效果
                if (tongbao.RandomResType != ResType.None && tongbao.RandomResCount > 0)
                {
                    OnSwitchResValueChanged(tongbao.RandomResType, tongbao.RandomResCount);
                }
                // 福祸相依
                if (HasSpecialCondition(SpecialConditionFlag.Collectible_Fortune))
                {
                    if (tongbao.Type == TongbaoType.Risk)
                    {
                        OnSwitchResValueChanged(ResType.Coupon, 1);
                    }
                }
            }
        }

        public void RemoveTongbaoAt(int posIndex)
        {
            if (TongbaoBox == null)
            {
                return;
            }

            if (posIndex >= 0 && posIndex < TongbaoBox.Length)
            {
                TongbaoBox[posIndex]?.Recycle();
                TongbaoBox[posIndex] = null;
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
                    TongbaoBox[i]?.Recycle();
                    TongbaoBox[i] = null;
                    return;
                }
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

        public void ClearTongbao()
        {
            if (TongbaoBox == null)
            {
                return;
            }

            for (int i = 0; i < TongbaoBox.Length; i++)
            {
                TongbaoBox[i]?.Recycle();
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

        private void OnSwitchResValueChanged(ResType type, int value)
        {
            if (type == ResType.None || value == 0)
            {
                return;
            }

            if (mResChangedTempSB.Length > 0)
            {
                mResChangedTempSB.Append("，");
            }

            int oldValue = GetResValue(type);
            AddResValue(type, value);
            int newValue = GetResValue(type);

            mResChangedTempSB.Append(Define.GetResName(type));

            if (value > 0)
            {
                mResChangedTempSB.Append('+');
            }
            mResChangedTempSB.Append(value);

            mResChangedTempSB.Append(": ")
                             .Append(oldValue)
                             .Append("->")
                             .Append(newValue);
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
            if (mSquadDefine == null)
            {
                return true;
            }
            int costLifePoint = mSquadDefine.GetCostLifePoint(SwitchCount);
            return GetResValue(ResType.LifePoint) > costLifePoint;
        }

        public int GetNextSwitchCostLifePoint()
        {
            if (mSquadDefine == null)
            {
                return 0;
            }
            return mSquadDefine.GetCostLifePoint(SwitchCount);
        }

        // 避免大量模拟时字符串拼接导致频繁GC，用StringBuilder
        public bool SwitchTongbao(int posIndex, bool force = false)
        {
            mSwitchResultSB.Clear();
            mResChangedTempSB.Clear();
            Tongbao tongbao = GetTongbao(posIndex);
            if (tongbao == null)
            {
                mSwitchResultSB.Append("交换失败，选中的位置[")
                               .Append(posIndex)
                               .Append("]上的通宝为空");
                return false;
            }

            if (!tongbao.CanSwitch())
            {
                mSwitchResultSB.Append("交换失败，通宝[")
                               .Append(tongbao.Name)
                               .Append("]不可交换");
                return false;
            }

            int costLifePoint = GetNextSwitchCostLifePoint();
            int lifePoint = GetResValue(ResType.LifePoint);
            if (lifePoint > costLifePoint || force)
            {
                int newTongbaoId = SwitchPool.SwitchTongbao(mRandom, this, tongbao);
                Tongbao newTongbao = Tongbao.CreateTongbao(newTongbaoId, mRandom);
                if (newTongbao != null)
                {
                    string oldName = tongbao.Name;
                    InsertTongbao(newTongbao, posIndex);

                    SwitchCount++;
                    OnSwitchResValueChanged(ResType.LifePoint, -costLifePoint);

                    //无GC写法
                    mSwitchResultSB.Append("将位置[")
                                   .Append(posIndex)
                                   .Append("]上的[")
                                   .Append(oldName)
                                   .Append("]交换为[")
                                   .Append(newTongbao.Name)
                                   .Append("] (")
                                   .Append(mResChangedTempSB)
                                   .Append(')');
                    return true;
                }
            }

            mSwitchResultSB.Append("交换失败，交换所需生命值不足 (")
                           .Append(costLifePoint)
                           .Append(")");
            return false;
        }
    }
}
