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
        public SimulationType SimulationType { get; private set; }
        public int TotalSimulateStep { get; private set; }
        public int ExecSimulateStep { get; private set; }
        public float TotalSimulateTime { get; private set; }
        public int TotalExecExchangeStep { get; private set; }
        public int EstimatedExchangeStep { get; private set; }

        private PlayerData mInitialPlayerData;
        private int mExecSimulationStep; // local
        private int mTotalExchangeRecord;
        private ExchangeStepResult mLastExchangeStepResult;

        // 不同线程对这个数组ShareContainer之后分别写入不同Index线程安全
        private SimulationRecord[] mSimulationRecords; // [SimulateStepIndex]

        public ExchangeDataCollector(int maxExchangeRecord = -1)
        {
            MaxExchangeRecord = maxExchangeRecord;
        }

        public void ForEachTongbaoRecords(Action<ExchangeRecord> callback)
        {
            if (mSimulationRecords == null || callback == null)
            {
                return;
            }

            var tempTongbaoBox = new int[mInitialPlayerData.MaxTongbaoCount];
            for (int i = 0; i < mSimulationRecords.Length; i++)
            {
                var records = mSimulationRecords[i].ExchangeRecords;
                if (records == null || records.Count <= 0)
                {
                    continue;
                }

                for (int j = 0; j < mInitialPlayerData.MaxTongbaoCount; j++)
                {
                    tempTongbaoBox[j] = mInitialPlayerData.GetTongbao(j)?.Id ?? -1;
                }

                for (int j = 0; j < records.Count; j++)
                {
                    var record = records[j];

                    int beforeTongbaoId = tempTongbaoBox[record.SlotIndex];
                    var retRecord = new ExchangeRecord
                    {
                        SimulationStepIndex = i,
                        ExchangeStepIndex = j,
                        SlotIndex = record.SlotIndex,
                        BeforeTongbaoId = beforeTongbaoId,
                        AfterTongbaoId = record.TongbaoId,
                        ExchangeStepResult = record.ExchangeStepResult,
                        ResValueRecords = new ResValueRecord[(int)ResType.Count - 1],
                    };

                    unsafe
                    {
                        for (int k = 0; k < retRecord.ResValueRecords.Length; k++)
                        {
                            ResType resType = (ResType)(k + 1);
                            int beforeValue;
                            if (j == 0)
                            {
                                beforeValue = mInitialPlayerData.GetResValue(resType);
                            }
                            else
                            {
                                var lastRecord = records[j - 1];
                                beforeValue = lastRecord.ResRecords[k];
                            }
                            retRecord.ResValueRecords[k] = new ResValueRecord
                            {
                                ResType = resType,
                                BeforeValue = beforeValue,
                                AfterValue = record.ResRecords[k]
                            };
                        }
                    }

                    callback(retRecord);
                    tempTongbaoBox[record.SlotIndex] = record.TongbaoId;
                }
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

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int remainSimStep)
        {
            EstimatedExchangeStep = estimatedLeftExchangeStep;
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

            // 若省略X条后的多余信息，会将X~LIMIT的资源变化信息存在X+1位置里
            if (OmitExcessiveExchanges)
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
                    for (int i = 0; i < (int)ResType.Count - 1; i++)
                    {
                        int resValue = context.PlayerData.GetResValue((ResType)(i + 1));
                        record.ResRecords[i] = (short)resValue;
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
                ExchangeStepResult = mLastExchangeStepResult,
            };

            unsafe
            {
                for (int i = 0; i < (int)ResType.Count - 1; i++)
                {
                    int resValue = context.PlayerData.GetResValue((ResType)(i + 1));
                    record.ResRecords[i] = (short)resValue;
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
            }
        }

        public void ClearData()
        {
            TotalSimulateStep = 0;
            ExecSimulateStep = 0;
            TotalSimulateTime = 0;
            TotalExecExchangeStep = 0;
            EstimatedExchangeStep = 0;
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
