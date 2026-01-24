using System;
using TongbaoExchangeCalc.DataModel;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    //public struct StepIndexes : IEquatable<StepIndexes>
    //{
    //    public int SimulateStepIndex;
    //    public int ExchangeStepIndex;

    //    public bool Equals(StepIndexes other)
    //    {
    //        return SimulateStepIndex == other.SimulateStepIndex && ExchangeStepIndex == other.ExchangeStepIndex;
    //    }

    //    public override readonly int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            int hash = 17;
    //            hash = hash * 31 + SimulateStepIndex;
    //            hash = hash * 31 + ExchangeStepIndex;
    //            return hash;
    //        }
    //    }
    //}

    //public struct ResRecordKey : IEquatable<ResRecordKey>
    //{
    //    public StepIndexes Indexes;
    //    public ResType ResType;

    //    public bool Equals(ResRecordKey other)
    //    {
    //        return Indexes.Equals(other.Indexes) && ResType == other.ResType;
    //    }

    //    public override readonly int GetHashCode()
    //    {
    //        unchecked
    //        {
    //            int hash = 17;
    //            hash = hash * 31 + Indexes.GetHashCode();
    //            hash = hash * 31 + (int)ResType;
    //            return hash;
    //        }
    //    }
    //}

    public struct ResRecordValue
    {
        public int BeforeValue; // <100000
        public int AfterValue; // <100000
    }

    public struct TongbaoRecordValue
    {
        public short BeforeId; // <10000,2B
        public short AfterId; // <10000,2B
        public byte SlotIndex; // 0~12,1B
    }

    public struct TongbaoRecord
    {
        public int SlotIndex;
        public int BeforeTongbaoId;
        public int AfterTongbaoId;
    }

    public struct ResValueRecord
    {
        public ResType ResType;
        public int BeforeValue;
        public int AfterValue;
        public readonly int ChangedValue => AfterValue - BeforeValue;
    }
}
