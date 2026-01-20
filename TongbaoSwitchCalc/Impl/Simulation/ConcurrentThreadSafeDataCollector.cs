using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class ConcurrentThreadSafeDataCollector : IThreadSafeDataCollector<SimulateContext>
    {
        public bool RecordEverySwitch { get; set; } = true;
        public int TotalSimulateStep { get; private set; }
        public int ExecSimulateStep { get; private set; }
        public float TotalSimulateTime { get; private set; }
        public int TotalSwitchStep => mSwitchStepResults.Count;
        public int EstimatedSwitchStep { get; private set; }

        // TODO capacity
        private readonly ConcurrentDictionary<StepIndexes, TongbaoRecordValue> mTongbaoRecords = new ConcurrentDictionary<StepIndexes, TongbaoRecordValue>();
        private readonly ConcurrentDictionary<ResRecordKey, ResRecordValue> mResValueRecords = new ConcurrentDictionary<ResRecordKey, ResRecordValue>();
        private readonly ConcurrentDictionary<StepIndexes, SwitchStepResult> mSwitchStepResults = new ConcurrentDictionary<StepIndexes, SwitchStepResult>();
        private readonly ConcurrentDictionary<int, SimulateStepResult> mSimulateStepResults = new ConcurrentDictionary<int, SimulateStepResult>();

        public IReadOnlyDictionary<StepIndexes, TongbaoRecordValue> TongbaoRecords => mTongbaoRecords;
        public IReadOnlyDictionary<ResRecordKey, ResRecordValue> ResValueRecords => mResValueRecords;
        public IReadOnlyDictionary<StepIndexes, SwitchStepResult> SwitchStepResults => mSwitchStepResults;
        public IReadOnlyDictionary<int, SimulateStepResult> SimulateStepResults => mSimulateStepResults;

        public void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            ClearData();
            TotalSimulateStep = totalSimStep;
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            ExecSimulateStep = executedSimStep;
            TotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateParallel(int estimatedLeftSwitchStep, int remainSimStep)
        {
            EstimatedSwitchStep = estimatedLeftSwitchStep;
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {

        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            mSimulateStepResults.TryAdd(context.SimulationStepIndex, result);
        }

        public void OnSwitchStepBegin(in SimulateContext context)
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

            mTongbaoRecords.TryAdd(
                indexes,
                new TongbaoRecordValue
                {
                    SlotIndex = context.SlotIndex,
                    BeforeId = tongbaoId,
                    AfterId = tongbaoId,
                }
            );

            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords.TryAdd(
                    new ResRecordKey
                    {
                        Indexes = indexes,
                        ResType = item.Key,
                    },
                    new ResRecordValue
                    {
                        BeforeValue = item.Value,
                        AfterValue = item.Value,
                    }
                );
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
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

            mSwitchStepResults.TryAdd(indexes, result);

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            mTongbaoRecords.AddOrUpdate(indexes,
                new TongbaoRecordValue
                {
                    SlotIndex = context.SlotIndex,
                    BeforeId = -1,
                    AfterId = tongbaoId,
                },
                (_, old) => new TongbaoRecordValue
                {
                    SlotIndex = old.SlotIndex,
                    BeforeId = old.BeforeId,
                    AfterId = tongbaoId,
                });

            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords.AddOrUpdate(
                    new ResRecordKey
                    {
                        Indexes = indexes,
                        ResType = item.Key,
                    },
                    new ResRecordValue
                    {
                        BeforeValue = 0,
                        AfterValue = item.Value,
                    },
                    (_, old) => new ResRecordValue
                    {
                        BeforeValue = old.BeforeValue,
                        AfterValue = item.Value,
                    });
            }
        }

        public void ClearData()
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
