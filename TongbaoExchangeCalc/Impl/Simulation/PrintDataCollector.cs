using System;
using System.Collections.Generic;
using System.Text;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class PrintDataCollector : IDataCollector<SimulateContext>
    {
        public bool RecordEachExchange { get; set; } = true;
        public int MaxExchangeRecord { get; set; } = -1; // 交换次数过多则省略，-1表示无限制
        public bool OmitExcessiveExchanges => MaxExchangeRecord >= 0; // 省略过多的交换信息

        private SimulationType mSimulationType;
        private int mTotalSimulateStep;
        private readonly Dictionary<int, string> mTempBeforeTongbaoName = new Dictionary<int, string>();
        private readonly Dictionary<int, Dictionary<ResType, int>> mTempResBefore = new Dictionary<int, Dictionary<ResType, int>>(); // key: simulationStepIndex

        private readonly ObjectPool<Dictionary<ResType, int>> mResDictPool = new ObjectPool<Dictionary<ResType, int>>();

        private int mLastExchangeResultStartIndex = -1;
        private int mLastExchangeResultEndIndex = -1;
        public string LastExchangeResult
        {
            get
            {
                if (mLastExchangeResultStartIndex < 0 || mLastExchangeResultEndIndex < 0)
                {
                    return string.Empty;
                }

                int length = mLastExchangeResultEndIndex - mLastExchangeResultStartIndex;
                if (length > 0)
                {
                    char[] chars = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        int index = i + mLastExchangeResultStartIndex;
                        chars[i] = OutputResultSB[index];
                    }
                    return new string(chars);
                }
                return string.Empty;
            }
        }

        // 避免大量模拟时字符串拼接导致频繁GC，用StringBuilder
        public StringBuilder OutputResultSB { get; private set; } = new StringBuilder();
        public string OutputResult => OutputResultSB.ToString();

        public PrintDataCollector(int maxExchangeRecord = -1)
        {
            MaxExchangeRecord = maxExchangeRecord;
        }

        //因为外部可以单步调用OnExchangeStepBegin/OnExchangeStepEnd，这里提供个接口初始化
        public void InitSimulateStep(int simulationStepIndex)
        {
            if (!mTempResBefore.ContainsKey(simulationStepIndex))
            {
                mTempResBefore.Add(simulationStepIndex, mResDictPool.Allocate());
            }
        }

        public void OnSimulateBegin(SimulationType type, int totalSimStep, PlayerData playerData)
        {
            ClearData();
            mSimulationType = type;
            mTotalSimulateStep = totalSimStep;
            OutputResultSB.Clear();
            OutputResultSB.Append('[')
                          .Append(SimulationDefine.GetSimulationName(type))
                          .Append("]模拟开始，共计")
                          .Append(totalSimStep)
                          .AppendLine("次模拟");
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, PlayerData playerData)
        {
            OutputResultSB.Append('[')
                          .Append(SimulationDefine.GetSimulationName(mSimulationType))
                          .Append("]模拟完成，模拟次数: ")
                          .Append(executedSimStep)
                          .Append(", 模拟耗时: ")
                          .Append(simCostTimeMS)
                          .AppendLine("ms");
        }

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int curSimStep)
        {
            OutputResultSB.Append("预计剩余交换次数过多(")
                          .Append(estimatedLeftExchangeStep)
                          .AppendLine(")，触发多线程优化");
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            mTempResBefore.Add(context.SimulationStepIndex, mResDictPool.Allocate());
            if (!RecordEachExchange)
            {
                RecordCurrentExchange(context);
            }

            OutputResultSB.Append("========第")
                          .Append(context.SimulationStepIndex + 1)
                          .Append('/')
                          .Append(mTotalSimulateStep)
                          .AppendLine("次模拟开始========");
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            if (!RecordEachExchange)
            {
                OutputResultSB.Append('(')
                              .Append(context.SimulationStepIndex + 1)
                              .Append("|");

                if (context.ExchangeStepIndex > 1)
                {
                    OutputResultSB.Append("1-");
                }

                OutputResultSB.Append(context.ExchangeStepIndex + 1)
                              .Append(") ")
                              .Append("总共经过了")
                              .Append(context.ExchangeStepIndex + 1)
                              .AppendLine("次交换");

                AppendResChangedResult(OutputResultSB, context);
                OutputResultSB.AppendLine();
            }
            else if (OmitExcessiveExchanges && context.ExchangeStepIndex > MaxExchangeRecord)
            {
                OutputResultSB.Append('(')
                              .Append(context.SimulationStepIndex + 1)
                              .Append('|');

                if (MaxExchangeRecord != context.ExchangeStepIndex)
                {
                    OutputResultSB.Append(MaxExchangeRecord + 1)
                                  .Append('-')
                                  .Append(context.ExchangeStepIndex + 1);
                }
                else
                {
                    OutputResultSB.Append(context.ExchangeStepIndex + 1);
                }

                OutputResultSB.Append(") ")
                              .Append("交换次数过多，省略了")
                              .Append(context.ExchangeStepIndex - MaxExchangeRecord + 1)
                              .Append("次交换信息");

                AppendResChangedResult(OutputResultSB, context);
                OutputResultSB.AppendLine();
            }

            string breakReason = SimulationDefine.GetSimulateStepEndReason(result);
            OutputResultSB.Append("模拟结束，结束原因: ")
                          .Append(breakReason)
                          .AppendLine()
                          .Append("========第")
                          .Append(context.SimulationStepIndex + 1)
                          .Append('/')
                          .Append(mTotalSimulateStep)
                          .AppendLine("次模拟结束========");

            mTempResBefore[context.SimulationStepIndex].Clear();
            mResDictPool.Recycle(mTempResBefore[context.SimulationStepIndex], true);
            mTempResBefore.Remove(context.SimulationStepIndex);
            mTempBeforeTongbaoName.Remove(context.SimulationStepIndex);
        }

        public void OnExchangeStepBegin(in SimulateContext context)
        {
            if (!RecordEachExchange)
            {
                return;
            }

            // >: 多记录一次，用于最终计算差值
            if (OmitExcessiveExchanges && context.ExchangeStepIndex > MaxExchangeRecord)
            {
                return;
            }

            RecordCurrentExchange(context);
        }

        public void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            if (!RecordEachExchange)
            {
                return;
            }

            if (OmitExcessiveExchanges && context.ExchangeStepIndex >= MaxExchangeRecord)
            {
                return;
            }

            //mOutputResult.Append(System.Threading.Thread.CurrentThread.ManagedThreadId);

            OutputResultSB.Append('(')
                          .Append(context.SimulationStepIndex + 1)
                          .Append('|')
                          .Append(context.ExchangeStepIndex + 1)
                          .Append(") ");

            mLastExchangeResultStartIndex = OutputResultSB.Length;
            mLastExchangeResultEndIndex = OutputResultSB.Length;

            mTempBeforeTongbaoName.TryGetValue(context.SimulationStepIndex, out var beforeTongbaoName);

            if (result == ExchangeStepResult.Success)
            {
                Tongbao afterTongbao = context.PlayerData.GetTongbao(context.SlotIndex);

                OutputResultSB.Append("将位置[")
                              .Append(context.SlotIndex + 1)
                              .Append("]上的[")
                              .Append(beforeTongbaoName)
                              .Append("]交换为[")
                              .Append(afterTongbao.Name)
                              .Append(']');
                AppendResChangedResult(OutputResultSB, context);
            }
            else
            {
                switch (result)
                {
                    case ExchangeStepResult.SelectedEmpty:
                        OutputResultSB.Append("交换失败，选中的位置[")
                                      .Append(context.SlotIndex + 1)
                                      .Append("]上的通宝为空");
                        break;
                    case ExchangeStepResult.TongbaoUnexchangeable:
                        OutputResultSB.Append("交换失败，通宝[")
                                      .Append(beforeTongbaoName)
                                      .Append("]不可交换");
                        break;
                    case ExchangeStepResult.LifePointNotEnough:
                        OutputResultSB.Append("交换失败，交换所需生命值不足");
                        break;
                    case ExchangeStepResult.ExchangeableTongbaoNotExist:
                        OutputResultSB.Append("交换失败，通宝[")
                                      .Append(beforeTongbaoName)
                                      .Append("]无可交换通宝");
                        break;
                    case ExchangeStepResult.UnknownError:
                        OutputResultSB.Append("交换失败，未知错误");
                        break;
                    default:
                        break;
                }
            }

            mLastExchangeResultEndIndex = OutputResultSB.Length;
            OutputResultSB.AppendLine();
        }

        private void RecordCurrentExchange(in SimulateContext context)
        {
            Tongbao beforeTongbao = context.PlayerData.GetTongbao(context.SlotIndex);
            mTempBeforeTongbaoName[context.SimulationStepIndex] = beforeTongbao?.Name;

            var resDict = mTempResBefore[context.SimulationStepIndex];
            resDict.Clear();
            foreach (var item in context.PlayerData.ResValuesInternal)
            {
                resDict.Add(item.Key, item.Value);
            }
        }

        private StringBuilder AppendResChangedResult(StringBuilder sb, in SimulateContext context)
        {
            // PlayerData的项只增加不删除，所以这里不需要考虑并集
            bool isEmpty = true;
            foreach (var item in context.PlayerData.ResValuesInternal)
            {
                ResType type = item.Key;
                mTempResBefore[context.SimulationStepIndex].TryGetValue(type, out int beforeValue);
                int afterValue = item.Value;
                int changedValue = afterValue - beforeValue;
                if (beforeValue != afterValue)
                {
                    if (isEmpty)
                    {
                        sb.Append("，");
                    }
                    else
                    {
                        sb.Append('(');
                    }
                    sb.Append(Define.GetResName(type));

                    if (changedValue > 0)
                    {
                        sb.Append('+');
                    }
                    sb.Append(changedValue);

                    sb.Append(": ")
                      .Append(beforeValue)
                      .Append("->")
                      .Append(afterValue);

                    isEmpty = false;
                }
            }
            if (!isEmpty)
            {
                sb.Append(')');
            }
            return sb;
        }

        public IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new PrintDataCollector
            {
                RecordEachExchange = RecordEachExchange,
                MaxExchangeRecord = MaxExchangeRecord,
                mSimulationType = mSimulationType,
                mTotalSimulateStep = mTotalSimulateStep,
            };
            return collector;
        }

        public void ShareContainer(IDataCollector<SimulateContext> other)
        {

        }

        public void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is PrintDataCollector collector)
            {
                OutputResultSB.Append(collector.OutputResult);
            }
        }

        public void ClearData()
        {
            mLastExchangeResultStartIndex = -1;
            mLastExchangeResultEndIndex = -1;
            mTempBeforeTongbaoName.Clear();
            OutputResultSB.Clear();

            foreach (var item in mTempResBefore)
            {
                mResDictPool.Recycle(item.Value, true);
            }
            mTempResBefore.Clear();
        }
    }
}
