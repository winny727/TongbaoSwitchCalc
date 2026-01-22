using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class LockThreadSafeDataCollector : IThreadSafeDataCollector<SimulateContext>
    {
        public struct StepIndexes : IEquatable<StepIndexes>
        {
            public int SimulateStepIndex;
            public int SwitchStepIndex;

            public bool Equals(StepIndexes other)
            {
                return SimulateStepIndex == other.SimulateStepIndex && SwitchStepIndex == other.SwitchStepIndex;
            }

            public override int GetHashCode()
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

            public override int GetHashCode()
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


        public int TotalSimulateCount { get; private set; }
        public float TotalSimulateTime { get; private set; }
        public int TotalSwitchCount => mSwitchStepResults.Count;

        private readonly Dictionary<StepIndexes, TongbaoRecordValue> mTongbaoRecords = new Dictionary<StepIndexes, TongbaoRecordValue>();
        private readonly Dictionary<ResRecordKey, ResRecordValue> mResValueRecords = new Dictionary<ResRecordKey, ResRecordValue>();
        private readonly Dictionary<StepIndexes, SwitchStepResult> mSwitchStepResults = new Dictionary<StepIndexes, SwitchStepResult>();
        private readonly Dictionary<int, SimulateStepResult> mSimulateStepResults = new Dictionary<int, SimulateStepResult>();
        private readonly object mTongbaoRecordsLock = new object();
        private readonly object mResValueRecordsLock = new object();
        private readonly object mSwitchStepResultsLock = new object();
        private readonly object mSimulateStepResultsLock = new object();


        public IReadOnlyDictionary<StepIndexes, TongbaoRecordValue> TongbaoRecords => mTongbaoRecords;
        public IReadOnlyDictionary<ResRecordKey, ResRecordValue> ResValueRecords => mResValueRecords;
        public IReadOnlyDictionary<StepIndexes, SwitchStepResult> SwitchStepResults => mSwitchStepResults;
        public IReadOnlyDictionary<int, SimulateStepResult> SimulateStepResults => mSimulateStepResults;

        public void OnSimulateBegin(SimulationType type, int totalSimCount, in IReadOnlyPlayerData playerData)
        {
            ClearData();
        }

        public void OnSimulateEnd(int executedSimCount, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            TotalSimulateCount = executedSimCount;
            TotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {

        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            lock (mSimulateStepResultsLock)
            {
                mSimulateStepResults[context.SimulationStepIndex] = result;
            }
        }

        public void OnSwitchStepBegin(in SimulateContext context)
        {
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

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
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
                mTongbaoRecords[indexes] = new TongbaoRecordValue
                {
                    SlotIndex = context.SlotIndex,
                    BeforeId = mTongbaoRecords[indexes].BeforeId,
                    AfterId = tongbaoId,
                };
            }

            foreach (var item in context.PlayerData.ResValues)
            {
                lock (mResValueRecordsLock)
                {
                    var key = new ResRecordKey
                    {
                        Indexes = indexes,
                        ResType = item.Key,
                    };

                    mResValueRecords[key] = new ResRecordValue
                    {
                        BeforeValue = mResValueRecords[key].BeforeValue,
                        AfterValue = item.Value,
                    };
                }
            }
        }

        public void ClearData()
        {
            TotalSimulateCount = 0;
            TotalSimulateTime = 0;
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
