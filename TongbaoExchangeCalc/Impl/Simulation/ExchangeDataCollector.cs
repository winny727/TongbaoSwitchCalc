using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    /// <summary>
    /// 线程安全的高性能模拟数据收集器
    /// </summary>
    public class ExchangeDataCollector : IDataCollector<SimulateContext>, IShareContainer<SimulateContext>
    {
        public bool RecordEachExchange { get; set; } = true;
        public int MaxExchangeRecord { get; set; } = -1; // 交换次数过多则省略，-1表示无限制
        public bool OmitExcessiveExchanges => MaxExchangeRecord >= 0; // 省略过多的交换信息
        public SimulationType SimulationType { get; private set; }
        public int TotalSimulateStep { get; private set; }
        public int ExecSimulateStep { get; private set; }
        public float TotalSimulateTime { get; private set; }
        public int TotalExecExchangeStep { get; private set; }
        public int TotalSuccessExchangeStep { get; private set; }

        public bool IsParallel => SwitchParallelSimStepIndex >= 0;
        public int EstimatedExchangeStep { get; private set; }
        public int SwitchParallelSimStepIndex { get; private set; } = -1; // 触发并行优化时的Index

        private PlayerData mInitialPlayerData;
        private int mExecSimulationStep; // local
        private int mTotalExchangeRecord;
        private ExchangeStepResult mLastExchangeStepResult;

        // 不同线程对这个数组ShareContainer之后分别写入不同Index线程安全
        // 若省略X条后的多余信息，会将X~LIMIT的资源变化信息存在X+1（即最后一个位置）位置里
        private SimulationRecord[] mSimulationRecords; // [SimulateStepIndex]

        public ExchangeDataCollector(int maxExchangeRecord = -1)
        {
            MaxExchangeRecord = maxExchangeRecord;
        }

        /// <summary>
        /// 遍历所有交换数据
        /// 如果开启了不记录详细数据或省略过多交换数据，则会在当轮模拟的最后一次回调作为剩余总结回调，
        /// 传入当前轮次模拟的最后一次交换的ExchangeStepIndex和所有跳过/省略交换次数的资源变化值总结ResValueRecords
        /// 此轮总结回调ExchangeRecord中SlotIndex/BeforeTongbaoId/AfterTongbaoId/ExchangeStepResult为无效值
        /// </summary>
        /// <param name="callback">回调，若返回false则取消终止遍历</param>
        public void ForEachExchangeRecords(int simulationStepIndex, Func<ExchangeRecord, bool> callback)
        {
            if (mSimulationRecords == null || callback == null)
            {
                return;
            }

            if (simulationStepIndex < 0 || simulationStepIndex >= mSimulationRecords.Length)
            {
                return;
            }

            var records = mSimulationRecords[simulationStepIndex].ExchangeRecords;
            if (records == null || records.Count <= 0)
            {
                return;
            }

            int resCount = (int)ResType.Count - 1;
            var tempTongbaoBox = new int[mInitialPlayerData.MaxTongbaoCount];
            for (int i = 0; i < mInitialPlayerData.MaxTongbaoCount; i++)
            {
                tempTongbaoBox[i] = mInitialPlayerData.GetTongbao(i)?.Id ?? -1;
            }

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                int exchangeStepIndex = i;
                if (!RecordEachExchange || (OmitExcessiveExchanges && i >= MaxExchangeRecord))
                {
                    exchangeStepIndex = mSimulationRecords[simulationStepIndex].FinalExchangeStepIndex;
                }

                int beforeTongbaoId = tempTongbaoBox[record.SlotIndex];
                var retRecord = new ExchangeRecord
                {
                    SimulationStepIndex = simulationStepIndex,
                    ExchangeStepIndex = exchangeStepIndex,
                    SlotIndex = record.SlotIndex,
                    BeforeTongbaoId = beforeTongbaoId,
                    AfterTongbaoId = record.TongbaoId,
                    ExchangeStepResult = record.ExchangeStepResult,
                    ResValueRecords = new ResValueRecord[resCount],
                };

                unsafe
                {
                    for (int k = 0; k < resCount; k++)
                    {
                        ResType type = (ResType)(k + 1);
                        int beforeValue;
                        if (i == 0)
                        {
                            beforeValue = mInitialPlayerData.GetResValue(type);
                        }
                        else
                        {
                            var lastRecord = records[i - 1];
                            beforeValue = lastRecord.ResRecords[k];
                        }
                        retRecord.ResValueRecords[k] = new ResValueRecord
                        {
                            ResType = type,
                            BeforeValue = beforeValue,
                            AfterValue = record.ResRecords[k]
                        };
                    }
                }

                if (!callback(retRecord))
                {
                    return;
                }
                tempTongbaoBox[record.SlotIndex] = record.TongbaoId;
            }
        }

        public SimulateStepResult GetSimulateStepResult(int simulateStepIndex)
        {
            return mSimulationRecords[simulateStepIndex].SimulateStepResult;
        }

        public void OnSimulateBegin(SimulationType type, int totalSimStep, PlayerData playerData)
        {
            ClearData();
            SimulationType = type;
            TotalSimulateStep = totalSimStep;
            mInitialPlayerData = new PlayerData(playerData.TongbaoSelector, playerData.Random);
            mInitialPlayerData.CopyFrom(playerData);

            if (mSimulationRecords == null || totalSimStep != mSimulationRecords.Length)
            {
                mSimulationRecords = new SimulationRecord[totalSimStep];
            }
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, PlayerData playerData)
        {
            ExecSimulateStep = executedSimStep;
            TotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int curSimStep)
        {
            EstimatedExchangeStep = estimatedLeftExchangeStep;
            SwitchParallelSimStepIndex = curSimStep;
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            // 根据平均值初始化List容量
            int avgExchangeCount = mExecSimulationStep > 0 ? mTotalExchangeRecord / mExecSimulationStep : 0;
            mSimulationRecords[context.SimulationStepIndex].ExchangeRecords ??= new List<ExchangeResultRecord>(avgExchangeCount);
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            mSimulationRecords[context.SimulationStepIndex].SimulateStepResult = result;
            mSimulationRecords[context.SimulationStepIndex].FinalExchangeStepIndex = (short)context.ExchangeStepIndex;

            // 若省略X条后的多余信息，会将X~LIMIT的资源变化信息存在X+1位置里
            if (!RecordEachExchange || (OmitExcessiveExchanges && context.ExchangeStepIndex >= MaxExchangeRecord))
            {
                Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
                int tongbaoId = tongbao != null ? tongbao.Id : -1;

                var record = new ExchangeResultRecord
                {
                    SlotIndex = (sbyte)context.SlotIndex,
                    TongbaoId = (Int16)tongbaoId,
                    ExchangeStepResult = mLastExchangeStepResult,
                };

                unsafe
                {
                    int resCount = (int)ResType.Count - 1;
                    for (int i = 0; i < resCount; i++)
                    {
                        int resValue = context.PlayerData.GetResValue((ResType)(i + 1));
                        record.ResRecords[i] = (Int16)resValue;
                    }
                }

                mSimulationRecords[context.SimulationStepIndex].ExchangeRecords.Add(record);
            }

            mExecSimulationStep++;
            mTotalExchangeRecord += mSimulationRecords[context.SimulationStepIndex].ExchangeRecords.Count;
        }

        public void OnExchangeStepBegin(in SimulateContext context)
        {

        }

        public void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            TotalExecExchangeStep++;
            if (result == ExchangeStepResult.Success)
            {
                TotalSuccessExchangeStep++;
            }

            if (!RecordEachExchange)
            {
                return;
            }

            if (OmitExcessiveExchanges && context.ExchangeStepIndex >= MaxExchangeRecord)
            {
                mLastExchangeStepResult = result;
                return;
            }

            Tongbao tongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            int tongbaoId = tongbao != null ? tongbao.Id : -1;

            var record = new ExchangeResultRecord
            {
                SlotIndex = (sbyte)context.SlotIndex,
                TongbaoId = (Int16)tongbaoId,
                ExchangeStepResult = result,
            };

            unsafe
            {
                int resCount = (int)ResType.Count - 1;
                for (int i = 0; i < resCount; i++)
                {
                    int resValue = context.PlayerData.GetResValue((ResType)(i + 1));
                    record.ResRecords[i] = (Int16)resValue;
                }
            }

            mSimulationRecords[context.SimulationStepIndex].ExchangeRecords.Add(record);
        }

        public IDataCollector<SimulateContext> CloneAsEmpty()
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

        public void ShareContainer(IDataCollector<SimulateContext> other)
        {
            if (other is ExchangeDataCollector collector)
            {
                mSimulationRecords = collector.mSimulationRecords;
            }
        }

        public void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is ExchangeDataCollector collector)
            {
                TotalExecExchangeStep += collector.TotalExecExchangeStep;
                TotalSuccessExchangeStep += collector.TotalSuccessExchangeStep;
            }
        }

        public void ClearData()
        {
            TotalSimulateStep = 0;
            ExecSimulateStep = 0;
            TotalSimulateTime = 0;
            TotalExecExchangeStep = 0;
            TotalSuccessExchangeStep = 0;
            EstimatedExchangeStep = 0;
            SwitchParallelSimStepIndex = -1;
            mExecSimulationStep = 0;
            mTotalExchangeRecord = 0;
            mLastExchangeStepResult = default;
            mInitialPlayerData = null;
            if (mSimulationRecords == null)
            {
                return;
            }

            for (int i = 0; i < mSimulationRecords.Length; i++)
            {
                mSimulationRecords[i].SimulateStepResult = default;
                mSimulationRecords[i].ExchangeRecords?.Clear();
            }
        }
    }
}
