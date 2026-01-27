using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public struct SimulationRecord
    {
        public List<ExchangeResultRecord> ExchangeRecords; // [ExchangeStepIndex]
        public SimulateStepResult SimulateStepResult;
        public Int16 FinalExchangeStepIndex; // <=10000
    }

    public unsafe struct ExchangeResultRecord
    {
        public Int16 TongbaoId; // <10000,2B, value: tongbaoId after exchange TODO 全部ID能压缩到255以下就用sbyte
        public byte SlotIndex; // 0~12,1B
        public ExchangeStepResult ExchangeStepResult; // 1B

        // 确定数组大小，用的时候不用new
        public fixed Int16 ResRecords[(int)ResType.Count - 1]; // index: (byte)ResType-1 (排除掉None), value: resValue after exchange
    }

    public struct ExchangeRecord
    {
        public int SimulationStepIndex;
        public int ExchangeStepIndex;
        public int SlotIndex;
        public int BeforeTongbaoId;
        public int AfterTongbaoId;
        public ResValueRecord[] ResValueRecords;
        public ExchangeStepResult ExchangeStepResult;
    }

    public struct ResValueRecord
    {
        public ResType ResType;
        public int BeforeValue;
        public int AfterValue;
        public readonly int ChangedValue => AfterValue - BeforeValue;
        public readonly bool IsValueChanged => BeforeValue != AfterValue;
    }
}
