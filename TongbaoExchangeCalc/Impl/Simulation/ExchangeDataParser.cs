using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class ExchangeDataParser
    {
        private struct SlotExchangeData
        {
            internal ExchangeCount ExchangeCount;
            internal Dictionary<int, SlotTongbaoData> SlotTongbaoDatas;

            internal static SlotExchangeData Create()
            {
                return new SlotExchangeData
                {
                    ExchangeCount = ExchangeCount.Create("Slot"),
                    SlotTongbaoDatas = new Dictionary<int, SlotTongbaoData>(),
                };
            }
        }

        private struct ExchangeCount
        {
            private readonly string mType;
            internal int SimulationStepCount;
            internal int MinExchangeCount;
            internal int MaxExchangeCount;
            internal int TotalExchangeCount;

            private static readonly Dictionary<string, int> mTotalExchangeCountSum = new Dictionary<string, int>();

            private ExchangeCount(string type)
            {
                mType = type;
                SimulationStepCount = 0;
                MinExchangeCount = -1;
                MaxExchangeCount = -1;
                TotalExchangeCount = 0;
            }

            internal static ExchangeCount Create(string type)
            {
                return new ExchangeCount(type);
            }

            internal static void ClearTotalExchangeCountSum()
            {
                mTotalExchangeCountSum.Clear();
            }

            internal void AddExchangeCount(int exchangeCount)
            {
                SimulationStepCount++;
                if (MinExchangeCount < 0 || exchangeCount < MinExchangeCount)
                {
                    MinExchangeCount = exchangeCount;
                }
                if (MaxExchangeCount < 0 || exchangeCount > MaxExchangeCount)
                {
                    MaxExchangeCount = exchangeCount;
                }
                TotalExchangeCount += exchangeCount;

                if (!mTotalExchangeCountSum.ContainsKey(mType))
                {
                    mTotalExchangeCountSum.Add(mType, 0);
                }
                mTotalExchangeCountSum[mType] += exchangeCount;
            }

            internal void MergeExchangeCount(ExchangeCount other)
            {
                SimulationStepCount += other.SimulationStepCount;
                if (MinExchangeCount < 0 || other.MinExchangeCount < MinExchangeCount)
                {
                    MinExchangeCount = other.MinExchangeCount;
                }
                if (MaxExchangeCount < 0 || other.MaxExchangeCount > MaxExchangeCount)
                {
                    MaxExchangeCount = other.MaxExchangeCount;
                }
                TotalExchangeCount += other.TotalExchangeCount;
            }

            internal readonly StringBuilder AppendExchangeCount(StringBuilder sb)
            {
                if (sb == null)
                {
                    return null;
                }

                sb.Append("总交换次数: ")
                  .Append(TotalExchangeCount);

                if (mTotalExchangeCountSum.TryGetValue(mType, out int totalExchangedCountSum))
                {
                    float percent = TotalExchangeCount * 100f / totalExchangedCountSum;
                    sb.Append('/')
                      .Append(totalExchangedCountSum)
                      .Append(" (")
                      .Append($"{percent:F2}")
                      .Append("%)");
                }
                if (SimulationStepCount > 0)
                {
                    float avgExchangeCount = (float)TotalExchangeCount / SimulationStepCount;
                    sb.Append(", 平均交换次数: ")
                      .Append($"{avgExchangeCount:F2}");
                }
                if (MinExchangeCount >= 0)
                {
                    sb.Append(", 最小交换次数: ")
                      .Append(MinExchangeCount);
                }
                if (MaxExchangeCount >= 0)
                {
                    sb.Append(", 最大交换次数: ")
                      .Append(MaxExchangeCount);
                }

                return sb;
            }
        }

        private struct SlotTongbaoData
        {
            internal int TotalCount; // 通宝在这个槽位的总出现次数
            internal ExchangeCount ExchangeCount; // 交换出该通宝的交换次数

            internal static SlotTongbaoData Create(int slotIndex)
            {
                return new SlotTongbaoData
                {
                    TotalCount = 0,
                    ExchangeCount = ExchangeCount.Create(GetExchangeCountKey(slotIndex)),
                };
            }

            internal static string GetExchangeCountKey(int slotIndex)
            {
                return $"Slot{slotIndex}Tongbao";
            }
        }

        private readonly ExchangeDataCollector mExchangeDataCollector;
        private readonly ExchangeSimulator mExchangeSimulator;
        private CancellationTokenSource mCancellationTokenSource;
        private IProgress<int> mAsyncProgress;

        public bool IsAsyncBuilding => mCancellationTokenSource != null;
        private bool mIsClearDataRequested = false;

        // 打印数据
        public StringBuilder OutputResultSB { get; } = new StringBuilder();
        public string OutputResult => OutputResultSB.ToString();

        // 统计数据
        private int mCurrentSlotIndex = -1;
        private int mSlotExchangeCount = 0;
        private int mSlotLastTongbaoId = -1;
        private readonly Dictionary<SimulateStepResult, int> mTotalSimulateStepResult = new Dictionary<SimulateStepResult, int>();
        private readonly Dictionary<int, SlotExchangeData> mSlotExchangeDatas = new Dictionary<int, SlotExchangeData>();
        private readonly Dictionary<ResType, int> mTotalResChanged = new Dictionary<ResType, int>();
        public StringBuilder StatisticResultSB { get; } = new StringBuilder();
        public string StatisticResult => StatisticResultSB.ToString();

        public StringBuilder SlotStatisticResultSB { get; } = new StringBuilder();
        public string SlotStatisticResult => SlotStatisticResultSB.ToString();

        private CodeTimer mCodeTimer;


        public ExchangeDataParser(ExchangeDataCollector collector, ExchangeSimulator simulator)
        {
            mExchangeDataCollector = collector ?? throw new ArgumentNullException(nameof(collector));
            mExchangeSimulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        }

        public async Task BuildResultAsync(IProgress<int> progress = null)
        {
            mCodeTimer = CodeTimer.StartNew("BuildResult");
            mCancellationTokenSource = new CancellationTokenSource();
            mAsyncProgress = progress;
            try
            {
                await Task.Run(BuildResult, mCancellationTokenSource.Token);
            }
            finally
            {
                if (mCancellationTokenSource.IsCancellationRequested)
                {
                    OutputResultSB.AppendLine("交换结果未解析完成: 用户取消");
                    StatisticResultSB.AppendLine("数据统计结果未解析完成: 用户取消");
                    SlotStatisticResultSB.Clear();
                }
                mCodeTimer?.Dispose();
                mCodeTimer = null;
                mCancellationTokenSource?.Dispose();
                mCancellationTokenSource = null;
                mAsyncProgress = null;
                if (mIsClearDataRequested)
                {
                    ClearData();
                }
            }
        }

        public void CancelBuild()
        {
            mCancellationTokenSource?.Cancel();
        }

        public void BuildResult()
        {
            var sb = OutputResultSB;
            var collector = mExchangeDataCollector;

            ClearDataInternal();
            mAsyncProgress?.Report(0);

            sb.Append('[')
              .Append(SimulationDefine.GetSimulationName(collector.SimulationType))
              .Append("]模拟开始，共计")
              .Append(collector.TotalSimulateStep)
              .AppendLine("次模拟");

            for (int i = 0; i < collector.ExecSimulateStep; i++)
            {
                if (mCancellationTokenSource != null && mCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                AppendSimulateStepBegin(i);
                mCurrentSlotIndex = -1;
                collector.ForEachExchangeRecords(i, ExchangeRecordCallback);
                RecordSlotData();
                AppendSimulateStepEnd(i);
                mAsyncProgress?.Report(i);
            }

            float buildResultTime = mCodeTimer.ElapsedMilliseconds;

            sb.Append('[')
              .Append(SimulationDefine.GetSimulationName(collector.SimulationType))
              .Append("]模拟完成，模拟次数: ")
              .Append(collector.ExecSimulateStep)
              .Append(", 模拟耗时: ")
              .Append(collector.TotalSimulateTime)
              .AppendLine("ms")
              .AppendLine();

            BuildStatisticResult();

            mAsyncProgress?.Report(collector.ExecSimulateStep);
        }

        private void BuildStatisticResult()
        {
            var sb = StatisticResultSB;
            var collector = mExchangeDataCollector;

            sb.Append('[')
              .Append(SimulationDefine.GetSimulationName(collector.SimulationType))
              .Append("]模拟完成");

            if (collector.IsParallel)
            {
                sb.Append("(触发多线程优化)");
            }

            float buildResultTime = mCodeTimer.ElapsedMilliseconds;
            sb.AppendLine("，模拟次数: ")
              .Append(collector.ExecSimulateStep)
              .Append('/')
              .Append(collector.TotalSimulateStep)
              .Append(", 模拟耗时: ")
              .Append(collector.TotalSimulateTime)
              .Append("ms, 数据处理耗时: ")
              .Append(buildResultTime)
              .Append("ms, 总耗时: ")
              .Append(collector.TotalSimulateTime + buildResultTime)
              .Append("ms, 成功交换总次数: ")
              .Append(collector.TotalSuccessExchangeStep)
              .AppendLine()
              .AppendLine();

            sb.AppendLine("模拟结果统计: ");
            foreach (var item in mTotalSimulateStepResult)
            {
                string name = SimulationDefine.GetSimulateStepEndReason(item.Key);
                sb.Append(name)
                  .Append(": ");
                AppendPercent(sb, item.Value, collector.ExecSimulateStep);
                sb.AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("期望资源变化:");
            foreach (var item in mTotalResChanged)
            {
                string name = Define.GetResName(item.Key);
                float expectation = collector.ExecSimulateStep > 0 ? (float)item.Value / collector.ExecSimulateStep : 0;
                sb.Append(name)
                  .Append(": ")
                  .Append(expectation)
                  .AppendLine();
            }

            if (!collector.HasOmitRecord)
            {
                BuildSlotStatisticResult();
            }
        }

        private void BuildSlotStatisticResult()
        {
            var sb = SlotStatisticResultSB;

            var unexchangeableTongbaoDict = new Dictionary<string, SlotTongbaoData>();
            var expectedTongbaoDict = new Dictionary<string, SlotTongbaoData>();

            sb.AppendLine("槽位统计: ");
            for (int i = 0; i < mExchangeSimulator.PlayerData.MaxTongbaoCount; i++)
            {
                if (!mSlotExchangeDatas.TryGetValue(i, out var data))
                {
                    continue;
                }

                sb.Append('[')
                  .Append(i + 1)
                  .Append("] ");
                data.ExchangeCount.AppendExchangeCount(sb);

                int totalTongbaoCount = 0;
                int otherTongbaoCount = 0;
                string key = SlotTongbaoData.GetExchangeCountKey(i);
                ExchangeCount otherExchangeCount = ExchangeCount.Create(key);
                unexchangeableTongbaoDict.Clear();
                expectedTongbaoDict.Clear();
                foreach (var item in data.SlotTongbaoDatas)
                {
                    int tongbaoId = item.Key;
                    if (mExchangeSimulator.UnexchangeableTongbaoIds.Contains(tongbaoId))
                    {
                        // 不可交换通宝
                        string name = Helper.GetTongbaoFullName(tongbaoId);
                        if (!string.IsNullOrEmpty(name))
                        {
                            unexchangeableTongbaoDict.Add(name, item.Value);
                        }
                    }
                    else if (mExchangeSimulator.ExpectedTongbaoIds.Contains(tongbaoId))
                    {
                        // 期望通宝
                        string name = Helper.GetTongbaoFullName(tongbaoId);
                        if (!string.IsNullOrEmpty(name))
                        {
                            expectedTongbaoDict.Add(name, item.Value);
                        }
                    }
                    else
                    {
                        otherTongbaoCount += item.Value.TotalCount;
                        otherExchangeCount.MergeExchangeCount(item.Value.ExchangeCount);
                    }
                    totalTongbaoCount += item.Value.TotalCount;
                }
                if (unexchangeableTongbaoDict.Count > 0)
                {
                    sb.AppendLine()
                      .Append("交换出不可交换通宝: ");
                    foreach (var item in unexchangeableTongbaoDict)
                    {
                        sb.AppendLine()
                          .Append(item.Key)
                          .Append(" [");
                        AppendPercent(sb, item.Value.TotalCount, totalTongbaoCount);
                        sb.Append("] ");
                        item.Value.ExchangeCount.AppendExchangeCount(sb);
                    }
                }
                if (expectedTongbaoDict.Count > 0)
                {
                    sb.AppendLine()
                      .Append("交换出期望通宝: ");
                    foreach (var item in expectedTongbaoDict)
                    {
                        sb.AppendLine()
                          .Append(item.Key)
                          .Append(" [");
                        AppendPercent(sb, item.Value.TotalCount, totalTongbaoCount);
                        sb.Append("] ");
                        item.Value.ExchangeCount.AppendExchangeCount(sb);
                    }
                }
                if (otherExchangeCount.TotalExchangeCount > 0)
                {
                    //otherExchangeCount.SimulationStepCount = ExchangeCount.GetTotalExchangeCountSum(key);
                    sb.AppendLine()
                      .Append("其它原因停止交换/切换槽位 (生命值限制/通宝无法交换/槽位为空) [");
                    AppendPercent(sb, otherTongbaoCount, totalTongbaoCount);
                    sb.Append("] ");
                    otherExchangeCount.AppendExchangeCount(sb);
                }

                sb.AppendLine()
                  .AppendLine();
            }
            sb.AppendLine();
        }

        public void ClearData()
        {
            if (IsAsyncBuilding)
            {
                CancelBuild();
                mIsClearDataRequested = true;
                return;
            }

            ClearDataInternal();
        }

        private void ClearDataInternal()
        {
            mIsClearDataRequested = false;
            OutputResultSB.Clear();
            mCurrentSlotIndex = -1;
            mSlotExchangeCount = 0;
            mSlotLastTongbaoId = -1;
            mTotalSimulateStepResult.Clear();
            mSlotExchangeDatas.Clear();
            mTotalResChanged.Clear();
            StatisticResultSB.Clear();
            SlotStatisticResultSB.Clear();
            ExchangeCount.ClearTotalExchangeCountSum();
        }

        private StringBuilder AppendPercent(StringBuilder sb, int value, int total)
        {
            if (sb == null)
            {
                return null;
            }

            float percent = total > 0 ? value * 100f / total : 0;
            sb.Append(value)
              .Append("/")
              .Append(total)
              .Append(" (")
              .Append($"{percent:F2}")
              .Append("%)");

            return sb;
        }

        private bool ExchangeRecordCallback(ExchangeRecord record)
        {
            var collector = mExchangeDataCollector;

            if (mCancellationTokenSource != null && mCancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            for (int i = 0; i < record.ResValueRecords.Length; i++)
            {
                var r = record.ResValueRecords[i];
                if (!mTotalResChanged.ContainsKey(r.ResType))
                {
                    mTotalResChanged.Add(r.ResType, 0);
                }
                mTotalResChanged[r.ResType] += r.ChangedValue;
            }

            if (!collector.RecordEachExchange)
            {
                AppendSkippedExchangeStep(record);
                return true;
            }

            if (collector.OmitExcessiveExchanges && record.ExchangeStepIndex >= collector.MaxExchangeRecord)
            {
                AppendSkippedExchangeStep(record);
                return true;
            }

            if (mCurrentSlotIndex != record.SlotIndex)
            {
                RecordSlotData();
                mSlotExchangeCount = 0;
                mCurrentSlotIndex = record.SlotIndex;
            }

            mSlotExchangeCount++;
            mSlotLastTongbaoId = record.AfterTongbaoId;

            AppendExchangeStep(record);

            return true;
        }

        private void RecordSlotData()
        {
            var collector = mExchangeDataCollector;
            if (collector.HasOmitRecord)
            {
                return;
            }

            if (mCurrentSlotIndex >= 0)
            {
                // 统计上个槽位的数据
                if (!mSlotExchangeDatas.TryGetValue(mCurrentSlotIndex, out var data))
                {
                    data = SlotExchangeData.Create();
                }
                data.ExchangeCount.AddExchangeCount(mSlotExchangeCount);

                TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(mSlotLastTongbaoId);
                if (config != null)
                {
                    if (!data.SlotTongbaoDatas.TryGetValue(mSlotLastTongbaoId, out var tongbaoData))
                    {
                        tongbaoData = SlotTongbaoData.Create(mCurrentSlotIndex);
                    }
                    tongbaoData.ExchangeCount.AddExchangeCount(mSlotExchangeCount);
                    tongbaoData.TotalCount++;
                    data.SlotTongbaoDatas[mSlotLastTongbaoId] = tongbaoData;
                }

                mSlotExchangeDatas[mCurrentSlotIndex] = data;
            }
        }

        private void AppendSimulateStepBegin(int simulationStepIndex)
        {
            if (simulationStepIndex < 0)
            {
                return;
            }

            var sb = OutputResultSB;
            var collector = mExchangeDataCollector;

            if (simulationStepIndex == collector.SwitchParallelSimStepIndex)
            {
                sb.Append("预计剩余交换次数过多(")
                  .Append(collector.EstimatedExchangeStep)
                  .AppendLine(")，触发多线程优化");
            }

            sb.Append("========第")
              .Append(simulationStepIndex + 1)
              .Append('/')
              .Append(collector.TotalSimulateStep)
              .AppendLine("次模拟开始========");
        }

        private void AppendSimulateStepEnd(int simulationStepIndex)
        {
            if (simulationStepIndex < 0)
            {
                return;
            }

            var sb = OutputResultSB;
            var collector = mExchangeDataCollector;

            var result = collector.GetSimulateStepResult(simulationStepIndex);
            if (!mTotalSimulateStepResult.ContainsKey(result))
            {
                mTotalSimulateStepResult.Add(result, 0);
            }
            mTotalSimulateStepResult[result]++;

            string reason = SimulationDefine.GetSimulateStepEndReason(result);

            sb.Append("模拟结束，结束原因: ")
              .Append(reason)
              .AppendLine()
              .Append("========第")
              .Append(simulationStepIndex + 1)
              .Append('/')
              .Append(collector.TotalSimulateStep)
              .AppendLine("次模拟结束========");
        }

        private void AppendSkippedExchangeStep(in ExchangeRecord record)
        {
            var sb = OutputResultSB;
            var collector = mExchangeDataCollector;

            if (!collector.RecordEachExchange)
            {
                sb.Append('(')
                   .Append(record.SimulationStepIndex + 1)
                   .Append("|");

                if (record.ExchangeStepIndex > 1)
                {
                    sb.Append("1-");
                }

                sb.Append(record.ExchangeStepIndex + 1)
                  .Append(") ")
                  .Append("总共经过了")
                  .Append(record.ExchangeStepIndex + 1)
                  .Append("次交换");

                AppendResChanged(record.ResValueRecords);
                sb.AppendLine();
            }
            else if (collector.OmitExcessiveExchanges && record.ExchangeStepIndex >= collector.MaxExchangeRecord)
            {
                sb.Append('(')
                  .Append(record.SimulationStepIndex + 1)
                  .Append('|');

                if (collector.MaxExchangeRecord != record.ExchangeStepIndex)
                {
                    sb.Append(collector.MaxExchangeRecord + 1)
                      .Append('-')
                      .Append(record.ExchangeStepIndex + 1);
                }
                else
                {
                    sb.Append(record.ExchangeStepIndex + 1);
                }

                sb.Append(") ")
                  .Append("省略了")
                  .Append(record.ExchangeStepIndex - collector.MaxExchangeRecord + 1)
                  .Append("次交换信息");

                AppendResChanged(record.ResValueRecords);
                sb.AppendLine();
            }
        }

        private void AppendExchangeStep(in ExchangeRecord record)
        {
            var sb = OutputResultSB;

            sb.Append('(')
              .Append(record.SimulationStepIndex + 1)
              .Append('|')
              .Append(record.ExchangeStepIndex + 1)
              .Append(") ");

            if (record.ExchangeStepResult == ExchangeStepResult.Success)
            {
                string beforeName = GetTongbaoName(record.BeforeTongbaoId);
                string afterName = GetTongbaoName(record.AfterTongbaoId);

                sb.Append("将位置[")
                  .Append(record.SlotIndex + 1)
                  .Append("]上的[")
                  .Append(beforeName)
                  .Append("]交换为[")
                  .Append(afterName)
                  .Append(']');

                AppendResChanged(record.ResValueRecords);
            }
            else
            {
                switch (record.ExchangeStepResult)
                {
                    case ExchangeStepResult.SelectedEmpty:
                        sb.Append("交换失败，选中的位置[")
                          .Append(record.SlotIndex + 1)
                          .Append("]上的通宝为空");
                        break;

                    case ExchangeStepResult.TongbaoUnexchangeable:
                        sb.Append("交换失败，通宝[")
                          .Append(GetTongbaoName(record.BeforeTongbaoId))
                          .Append("]不可交换");
                        break;

                    case ExchangeStepResult.LifePointNotEnough:
                        sb.Append("交换失败，交换所需生命值不足");
                        break;

                    case ExchangeStepResult.ExchangeableTongbaoNotExist:
                        sb.Append("交换失败，通宝[")
                          .Append(GetTongbaoName(record.BeforeTongbaoId))
                          .Append("]无可交换通宝");
                        break;

                    default:
                        sb.Append("交换失败，未知错误");
                        break;
                }
            }

            sb.AppendLine();
        }

        private void AppendResChanged(ResValueRecord[] records)
        {
            bool isEmpty = true;
            var sb = OutputResultSB;

            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];
                if (r.IsValueChanged)
                {
                    if (isEmpty)
                    {
                        sb.Append(" (");
                    }
                    else
                    {
                        sb.Append("，");
                    }
                    sb.Append(Define.GetResName(r.ResType));

                    int changedValue = r.ChangedValue;
                    if (changedValue > 0)
                    {
                        sb.Append('+');
                    }
                    sb.Append(changedValue);

                    sb.Append(": ")
                      .Append(r.BeforeValue)
                      .Append("->")
                      .Append(r.AfterValue);

                    isEmpty = false;
                }
            }

            if (!isEmpty)
            {
                sb.Append(')');
            }
        }

        private string GetTongbaoName(int tongbaoId)
        {
            //return Helper.GetTongbaoName(tongbaoId);
            return Helper.GetTongbaoFullName(tongbaoId);
        }
    }
}