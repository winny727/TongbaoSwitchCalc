using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class DataCollector : IDataCollector<SimulateContext>
    {
        public bool RecordEverySwitch { get; set; } = true;
        public int TotalSimulateStep { get; protected set; }
        public int ExecSimulateStep { get; protected set; }
        public float TotalSimulateTime { get; protected set; }
        public int TotalSwitchStep => mSwitchStepResults.Count;
        public int EstimatedSwitchStep { get; protected set; }

        // TODO capacity
        protected readonly Dictionary<StepIndexes, TongbaoRecordValue> mTongbaoRecords = new Dictionary<StepIndexes, TongbaoRecordValue>();
        protected readonly Dictionary<ResRecordKey, ResRecordValue> mResValueRecords = new Dictionary<ResRecordKey, ResRecordValue>();
        protected readonly Dictionary<StepIndexes, SwitchStepResult> mSwitchStepResults = new Dictionary<StepIndexes, SwitchStepResult>();
        protected readonly Dictionary<int, SimulateStepResult> mSimulateStepResults = new Dictionary<int, SimulateStepResult>();


        public IReadOnlyDictionary<StepIndexes, TongbaoRecordValue> TongbaoRecords => mTongbaoRecords;
        public IReadOnlyDictionary<ResRecordKey, ResRecordValue> ResValueRecords => mResValueRecords;
        public IReadOnlyDictionary<StepIndexes, SwitchStepResult> SwitchStepResults => mSwitchStepResults;
        public IReadOnlyDictionary<int, SimulateStepResult> SimulateStepResults => mSimulateStepResults;

        public virtual void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            ClearData();
            TotalSimulateStep = totalSimStep;
        }

        public virtual void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            ExecSimulateStep = executedSimStep;
            TotalSimulateTime = simCostTimeMS;
        }

        public virtual void OnSimulateParallel(int estimatedLeftSwitchStep, int remainSimStep)
        {
            EstimatedSwitchStep = estimatedLeftSwitchStep;
        }

        public virtual void OnSimulateStepBegin(in SimulateContext context)
        {

        }

        public virtual void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            mSimulateStepResults[context.SimulationStepIndex] = result;
        }

        public virtual void OnSwitchStepBegin(in SimulateContext context)
        {
            if (!RecordEverySwitch)
            {
                return;
            }

            var indexes = new StepIndexes
            {
                SimulateStepIndex = context.SimulationStepIndex,
                SwitchStepIndex = context.SwitchStepIndex,
            };

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            mTongbaoRecords[indexes] = new TongbaoRecordValue
            {
                SlotIndex = context.SlotIndex,
                BeforeId = tongbaoId,
                AfterId = tongbaoId,
            };

            foreach (var item in context.PlayerData.ResValues)
            {
                var key = new ResRecordKey
                {
                    Indexes = indexes,
                    ResType = item.Key,
                };

                mResValueRecords[key] = new ResRecordValue
                {
                    BeforeValue = item.Value,
                    AfterValue = item.Value,
                };
            }
        }

        public virtual void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            if (!RecordEverySwitch)
            {
                return;
            }

            var indexes = new StepIndexes
            {
                SimulateStepIndex = context.SimulationStepIndex,
                SwitchStepIndex = context.SwitchStepIndex,
            };

            mSwitchStepResults[indexes] = result;

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            int beforeId = -1;
            if (mTongbaoRecords.TryGetValue(indexes, out var record))
            {
                beforeId = record.BeforeId;
            }

            mTongbaoRecords[indexes] = new TongbaoRecordValue
            {
                SlotIndex = context.SlotIndex,
                BeforeId = beforeId,
                AfterId = tongbaoId,
            };

            foreach (var item in context.PlayerData.ResValues)
            {
                var key = new ResRecordKey
                {
                    Indexes = indexes,
                    ResType = item.Key,
                };

                int beforeValue = 0;
                if (mResValueRecords.TryGetValue(key, out var resRecord))
                {
                    beforeValue = resRecord.BeforeValue;
                }

                mResValueRecords[key] = new ResRecordValue
                {
                    BeforeValue = beforeValue,
                    AfterValue = item.Value,
                };
            }
        }

        public virtual void ClearData()
        {
            TotalSimulateStep = 0;
            ExecSimulateStep = 0;
            TotalSimulateTime = 0;
            EstimatedSwitchStep = 0;
            mTongbaoRecords.Clear();
            mResValueRecords.Clear();
            mSimulateStepResults.Clear();
            mSwitchStepResults.Clear();
        }
    }
}
