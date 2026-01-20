using System;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public struct StepIndexes : IEquatable<StepIndexes>
    {
        public int SimulateStepIndex;
        public int SwitchStepIndex;

        public bool Equals(StepIndexes other)
        {
            return SimulateStepIndex == other.SimulateStepIndex && SwitchStepIndex == other.SwitchStepIndex;
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + SimulateStepIndex;
                hash = hash * 31 + SwitchStepIndex;
                return hash;
            }
        }

    }

    public struct ResRecordKey : IEquatable<ResRecordKey>
    {
        public StepIndexes Indexes;
        public ResType ResType;

        public bool Equals(ResRecordKey other)
        {
            return Indexes.Equals(other.Indexes) && ResType == other.ResType;
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Indexes.GetHashCode();
                hash = hash * 31 + (int)ResType;
                return hash;
            }
        }
    }

    public struct ResRecordValue
    {
        public int BeforeValue;
        public int AfterValue;
    }

    public struct TongbaoRecordValue
    {
        public int SlotIndex;
        public int BeforeId;
        public int AfterId;
    }
}
