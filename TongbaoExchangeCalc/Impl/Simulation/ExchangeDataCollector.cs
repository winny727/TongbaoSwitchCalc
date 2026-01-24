using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class ExchangeDataCollector : IDataCollector<SimulateContext>
    {
        public bool RecordEachExchange { get; set; } = true;
        public SimulationType SimulationType { get; protected set; }
        public int TotalSimulateStep { get; protected set; }
        public int ExecSimulateStep { get; protected set; }
        public float TotalSimulateTime { get; protected set; }
        public int TotalExecExchangeStep { get; protected set; }
        public int EstimatedExchangeStep { get; protected set; }

        protected TongbaoRecordValue[][] mTongbaoRecords; // [SimulateStepIndex, ExchangeStepIndex]
        protected ResRecordValue[][][] mResValueRecords; // [SimulateStepIndex, ExchangeStepIndex, ResType]
        protected ExchangeStepResult[][] mExchangeStepResults; // [SimulateStepIndex, ExchangeStepIndex]
        protected SimulateStepResult[] mSimulateStepResults; // [SimulateStepIndex]

        public virtual void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            ClearData();
            SimulationType = type;
            TotalSimulateStep = totalSimStep;

            if (RecordEachExchange)
            {
                if (mTongbaoRecords == null || totalSimStep != mTongbaoRecords.Length)
                {
                    mTongbaoRecords = new TongbaoRecordValue[totalSimStep][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mTongbaoRecords[i] = new TongbaoRecordValue[ExchangeSimulator.EXCHANGE_STEP_LIMIT];
                    }
                }
                if (mResValueRecords == null || totalSimStep != mResValueRecords.Length)
                {
                    mResValueRecords = new ResRecordValue[totalSimStep][][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mResValueRecords[i] = new ResRecordValue[ExchangeSimulator.EXCHANGE_STEP_LIMIT][];
                        for (int j = 0; j < ExchangeSimulator.EXCHANGE_STEP_LIMIT; j++)
                        {
                            mResValueRecords[i][j] = new ResRecordValue[(int)ResType.Count];
                        }
                    }
                }
                if (mExchangeStepResults == null || totalSimStep != mExchangeStepResults.Length)
                {
                    mExchangeStepResults = new ExchangeStepResult[totalSimStep][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mExchangeStepResults[i] = new ExchangeStepResult[ExchangeSimulator.EXCHANGE_STEP_LIMIT];
                    }
                }
            }
            if (mSimulateStepResults == null || totalSimStep != mSimulateStepResults.Length)
            {
                mSimulateStepResults = new SimulateStepResult[totalSimStep];
            }
        }

        public virtual void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            ExecSimulateStep = executedSimStep;
            TotalSimulateTime = simCostTimeMS;
        }

        public virtual void OnSimulateParallel(int estimatedLeftExchangeStep, int remainSimStep)
        {
            EstimatedExchangeStep = estimatedLeftExchangeStep;
        }

        public virtual void OnSimulateStepBegin(in SimulateContext context)
        {

        }

        public virtual void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            mSimulateStepResults[context.SimulationStepIndex] = result;
        }

        public virtual void OnExchangeStepBegin(in SimulateContext context)
        {
            if (!RecordEachExchange)
            {
                return;
            }

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            mTongbaoRecords[context.SimulationStepIndex][context.ExchangeStepIndex] = new TongbaoRecordValue
            {
                SlotIndex = (byte)context.SlotIndex,
                BeforeId = (short)tongbaoId,
                AfterId = (short)tongbaoId,
            };

            mResValueRecords[context.SimulationStepIndex][context.ExchangeStepIndex] = new ResRecordValue[(int)ResType.Count];
            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords[context.SimulationStepIndex][context.ExchangeStepIndex][(int)item.Key] = new ResRecordValue
                {
                    BeforeValue = item.Value,
                    AfterValue = item.Value,
                };
            }
        }

        public virtual void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            TotalExecExchangeStep++;
            if (!RecordEachExchange)
            {
                return;
            }

            mExchangeStepResults[context.SimulationStepIndex][context.ExchangeStepIndex] = result;

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            mTongbaoRecords[context.SimulationStepIndex][context.ExchangeStepIndex].AfterId = (short)tongbaoId;

            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords[context.SimulationStepIndex][context.ExchangeStepIndex][(int)item.Key].AfterValue = item.Value;
            }
        }

        public virtual IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new ExchangeDataCollector
            {
                RecordEachExchange = RecordEachExchange,
                SimulationType = SimulationType,
                TotalSimulateStep = TotalSimulateStep,
            };
            return collector;
        }

        public virtual void ShareContainer(IDataCollector<SimulateContext> other)
        {
            if (other is ExchangeDataCollector collector)
            {
                mTongbaoRecords = collector.mTongbaoRecords;
                mResValueRecords = collector.mResValueRecords;
                mExchangeStepResults = collector.mExchangeStepResults;
                mSimulateStepResults = collector.mSimulateStepResults;
            }
        }

        public virtual void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is ExchangeDataCollector collector)
            {
                TotalExecExchangeStep += collector.TotalExecExchangeStep;
            }
        }

        public virtual void ClearData()
        {
            TotalSimulateStep = 0;
            ExecSimulateStep = 0;
            TotalSimulateTime = 0;
            TotalExecExchangeStep = 0;
            EstimatedExchangeStep = 0;
            if (mTongbaoRecords != null)
            {
                for (int i = 0; i < mTongbaoRecords.Length; i++)
                {
                    for (int j = 0; j < mTongbaoRecords[i].Length; j++)
                    {
                        mTongbaoRecords[i][j] = default;
                    }
                }
            }
            if (mResValueRecords != null)
            {
                for (int i = 0; i < mResValueRecords.Length; i++)
                {
                    for (int j = 0; j < mResValueRecords[i].Length; j++)
                    {
                        for (int k = 0; k < mResValueRecords[i][j].Length; k++)
                        {
                            mResValueRecords[i][j][k] = default;
                        }
                    }
                }
            }
            if (mExchangeStepResults != null)
            {
                for (int i = 0; i < mExchangeStepResults.Length; i++)
                {
                    for (int j = 0; j < mExchangeStepResults[i].Length; j++)
                    {
                        mExchangeStepResults[i][j] = default;
                    }
                }
            }
            if (mSimulateStepResults != null)
            {
                for (int i = 0; i < mSimulateStepResults.Length; i++)
                {
                    mSimulateStepResults[i] = default;
                }
            }
        }
    }
}
