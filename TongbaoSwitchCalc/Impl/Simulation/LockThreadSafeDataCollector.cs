using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class LockThreadSafeDataCollector : DataCollector, IThreadSafeDataCollector<SimulateContext>
    {
        private readonly object mTongbaoRecordsLock = new object();
        private readonly object mResValueRecordsLock = new object();
        private readonly object mSwitchStepResultsLock = new object();
        private readonly object mSimulateStepResultsLock = new object();

        public override void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            lock (mSimulateStepResultsLock)
            {
                mSimulateStepResults[context.SimulationStepIndex] = result;
            }
        }

        public override void OnSwitchStepBegin(in SimulateContext context)
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

            lock (mTongbaoRecordsLock)
            {
                mTongbaoRecords[indexes] = new TongbaoRecordValue
                {
                    SlotIndex = context.SlotIndex,
                    BeforeId = tongbaoId,
                    AfterId = tongbaoId,
                };
            }

            foreach (var item in context.PlayerData.ResValues)
            {
                var key = new ResRecordKey
                {
                    Indexes = indexes,
                    ResType = item.Key,
                };

                lock (mResValueRecordsLock)
                {
                    mResValueRecords[key] = new ResRecordValue
                    {
                        BeforeValue = item.Value,
                        AfterValue = item.Value,
                    };
                }
            }
        }

        public override void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
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

            lock (mSwitchStepResultsLock)
            {
                mSwitchStepResults[indexes] = result;
            }

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            lock (mTongbaoRecordsLock)
            {
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
            }

            lock (mResValueRecordsLock)
            {
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
        }

        public override void ClearData()
        {
            TotalSimulateStep = 0;
            ExecSimulateStep = 0;
            TotalSimulateTime = 0;
            EstimatedSwitchStep = 0;
            lock (mTongbaoRecordsLock)
            {
                mTongbaoRecords.Clear();
            }
            lock (mResValueRecordsLock)
            {
                mResValueRecords.Clear();
            }
            lock (mSimulateStepResultsLock)
            {
                mSimulateStepResults.Clear();
            }
            lock (mSwitchStepResultsLock)
            {
                mSwitchStepResults.Clear();
            }
        }
    }
}
