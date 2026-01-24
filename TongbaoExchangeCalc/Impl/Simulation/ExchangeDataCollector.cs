using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class ExchangeDataCollector : IDataCollector<SimulateContext>
    {
        public bool RecordEachExchange { get; set; } = true;
        public int MaxExchangeRecord { get; set; } = -1; // 交换次数过多则省略，-1表示无限制
        public bool OmitExcessiveExchanges => MaxExchangeRecord >= 0; // 省略过多的交换信息
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

        public ExchangeDataCollector(int maxExchangeRecord = -1)
        {
            MaxExchangeRecord = maxExchangeRecord;
        }

        // SimulateStepIndex, ExchangeStepIndex
        public void ForEachTongbaoRecords(Action<int, int, TongbaoRecord> callback)
        {
            if (mTongbaoRecords == null || callback == null)
            {
                return;
            }

            for (int i = 0; i < mTongbaoRecords.Length; i++)
            {
                var records = mTongbaoRecords[i];
                if (records == null || records.Length <= 0)
                {
                    continue;
                }

                for (int j = 0; j < records.Length; j++)
                {
                    var record = records[j];
                    callback(i, j, new TongbaoRecord
                    {
                        SlotIndex = record.SlotIndex,
                        BeforeTongbaoId = record.BeforeId,
                        AfterTongbaoId = record.AfterId,
                    });
                }
            }
        }

        // SimulateStepIndex, ExchangeStepIndex
        public void ForEachResValueRecords(Action<int, int, ResValueRecord> callback)
        {
            if (mResValueRecords == null || callback == null)
            {
                return;
            }

            for (int i = 0; i < mResValueRecords.Length; i++)
            {
                var records = mResValueRecords[i];
                if (records == null || records.Length <= 0)
                {
                    continue;
                }

                for (int j = 0; j < records.Length; j++)
                {
                    var resRecords = records[j];
                    if (resRecords == null || resRecords.Length <= 0)
                    {
                        continue;
                    }

                    for (int k = 0; k < resRecords.Length; k++)
                    {
                        var record = resRecords[k];
                        callback(i, j, new ResValueRecord
                        {
                            ResType = (ResType)(k + 1),
                            BeforeValue = record.BeforeValue,
                            AfterValue = record.AfterValue,
                        });
                    }
                }
            }
        }

        // SimulateStepIndex, ExchangeStepIndex
        public void ForEachExchangeStepResults(Action<int, int, ExchangeStepResult> callback)
        {
            if (mExchangeStepResults == null || callback == null)
            {
                return;
            }

            for (int i = 0; i < mExchangeStepResults.Length; i++)
            {
                var records = mExchangeStepResults[i];
                if (records == null || records.Length <= 0)
                {
                    continue;
                }

                for (int j = 0; j < records.Length; j++)
                {
                    var record = records[j];
                    callback(i, j, record);
                }
            }
        }

        // SimulateStepIndex
        public void ForEachSimulateStepResults(Action<int, SimulateStepResult> callback)
        {
            if (mSimulateStepResults == null || callback == null)
            {
                return;
            }

            for (int i = 0; i < mSimulateStepResults.Length; i++)
            {
                var record = mSimulateStepResults[i];
                callback(i, record);
            }
        }

        public virtual void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            ClearData();
            SimulationType = type;
            TotalSimulateStep = totalSimStep;

            // TODO 模拟次数过多时，会创建过大的数组，是否动态扩容？
            // 若省略X条后的多余信息，会将X~LIMIT的资源变化信息存在X+1位置里
            int length = MaxExchangeRecord >= 0 ? MaxExchangeRecord + 1 : ExchangeSimulator.EXCHANGE_STEP_LIMIT;
            if (RecordEachExchange)
            {
                if (mTongbaoRecords == null || totalSimStep != mTongbaoRecords.Length)
                {
                    mTongbaoRecords = new TongbaoRecordValue[totalSimStep][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mTongbaoRecords[i] = new TongbaoRecordValue[length];
                    }
                }
                if (mResValueRecords == null || totalSimStep != mResValueRecords.Length)
                {
                    mResValueRecords = new ResRecordValue[totalSimStep][][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mResValueRecords[i] = new ResRecordValue[length][];
                        for (int j = 0; j < length; j++)
                        {
                            mResValueRecords[i][j] = new ResRecordValue[(int)ResType.Count - 1]; // 排除0:None
                        }
                    }
                }
                if (mExchangeStepResults == null || totalSimStep != mExchangeStepResults.Length)
                {
                    mExchangeStepResults = new ExchangeStepResult[totalSimStep][];
                    for (int i = 0; i < totalSimStep; i++)
                    {
                        mExchangeStepResults[i] = new ExchangeStepResult[length];
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

            if (OmitExcessiveExchanges)
            {
                foreach (var item in context.PlayerData.ResValues)
                {
                    mResValueRecords[context.SimulationStepIndex][MaxExchangeRecord][(int)item.Key - 1].AfterValue = item.Value;
                }
            }
        }

        public virtual void OnExchangeStepBegin(in SimulateContext context)
        {
            if (!RecordEachExchange)
            {
                return;
            }

            // >: 多记录一次资源变化，可用于最终计算差值
            if (OmitExcessiveExchanges && context.ExchangeStepIndex > MaxExchangeRecord)
            {
                return;
            }

            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords[context.SimulationStepIndex][context.ExchangeStepIndex][(int)item.Key - 1] = new ResRecordValue
                {
                    BeforeValue = item.Value,
                    AfterValue = item.Value,
                };
            }

            if (OmitExcessiveExchanges && context.ExchangeStepIndex >= MaxExchangeRecord)
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
        }

        public virtual void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            TotalExecExchangeStep++;
            if (!RecordEachExchange)
            {
                return;
            }

            if (OmitExcessiveExchanges && context.ExchangeStepIndex >= MaxExchangeRecord)
            {
                return;
            }

            mExchangeStepResults[context.SimulationStepIndex][context.ExchangeStepIndex] = result;

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            mTongbaoRecords[context.SimulationStepIndex][context.ExchangeStepIndex].AfterId = (short)tongbaoId;

            foreach (var item in context.PlayerData.ResValues)
            {
                mResValueRecords[context.SimulationStepIndex][context.ExchangeStepIndex][(int)item.Key - 1].AfterValue = item.Value;
            }
        }

        public virtual IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new ExchangeDataCollector
            {
                RecordEachExchange = RecordEachExchange,
                MaxExchangeRecord = MaxExchangeRecord,
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
