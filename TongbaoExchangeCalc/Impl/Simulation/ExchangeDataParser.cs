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
        private readonly ExchangeDataCollector mExchangeDataCollector;
        private CancellationTokenSource mCancellationTokenSource;
        private IProgress<int> mAsyncProgress;

        public bool IsAsyncBuilding => mCancellationTokenSource != null;
        private bool mIsClearDataRequested = false;

        // 打印数据
        public StringBuilder OutputResultSB { get; private set; } = new StringBuilder();
        public string OutputResult => OutputResultSB.ToString();

        // 统计数据
        private readonly Dictionary<SimulateStepResult, int> mTotalSimulateStepResult = new Dictionary<SimulateStepResult, int>();
        private readonly Dictionary<ResType, int> mTotalResChanged = new Dictionary<ResType, int>();
        public StringBuilder StatisticResultSB { get; private set; } = new StringBuilder();
        public string StatisticResult => StatisticResultSB.ToString();


        public ExchangeDataParser(ExchangeDataCollector collector)
        {
            mExchangeDataCollector = collector ?? throw new ArgumentNullException(nameof(collector));
        }

        public async Task BuildResultAsync(IProgress<int> progress = null)
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mAsyncProgress = progress;
            try
            {
                using CodeTimer ct = new CodeTimer("BuildResult");
                await Task.Run(BuildResult, mCancellationTokenSource.Token);
            }
            finally
            {
                if (mCancellationTokenSource.IsCancellationRequested)
                {
                    OutputResultSB.AppendLine("交换结果未解析完成: 用户取消");
                    StatisticResultSB.AppendLine("数据统计结果未解析完成: 用户取消");
                }
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
                collector.ForEachExchangeRecords(i, ExchangeRecordCallback);
                AppendSimulateStepEnd(i);
                mAsyncProgress?.Report(i);
            }

            sb.Append('[')
              .Append(SimulationDefine.GetSimulationName(collector.SimulationType))
              .Append("]模拟完成，模拟次数: ")
              .Append(collector.ExecSimulateStep)
              .Append(", 模拟耗时: ")
              .Append(collector.TotalSimulateTime)
              .AppendLine("ms");

            BuildStatisticResult();

            mAsyncProgress?.Report(collector.ExecSimulateStep);
        }

        private void BuildStatisticResult()
        {
            var sb = StatisticResultSB;
            var collector = mExchangeDataCollector;

            sb.Append("模拟完成");

            if (collector.IsParallel)
            {
                sb.Append("(触发多线程优化)");
            }

            sb.AppendLine("，模拟次数: ")
              .Append(collector.ExecSimulateStep)
              .Append('/')
              .Append(collector.TotalSimulateStep)
              .Append(", 模拟耗时: ")
              .Append(collector.TotalSimulateTime)
              .Append("ms")
              .Append(", 成功交换总次数: ")
              .Append(collector.TotalSuccessExchangeStep)
              .AppendLine()
              .AppendLine();

            sb.AppendLine("模拟结果统计: ");
            foreach (var item in mTotalSimulateStepResult)
            {
                string name = SimulationDefine.GetSimulateStepEndReason(item.Key);
                float percent = item.Value * 100f / collector.ExecSimulateStep;
                sb.Append(name)
                  .Append(": ")
                  .Append(item.Value)
                  .Append(" (")
                  .Append(percent)
                  .AppendLine("%)");
            }
            sb.AppendLine();


            sb.AppendLine("期望资源变化:");
            foreach (var item in mTotalResChanged)
            {
                string name = Define.GetResName(item.Key);
                float expectation = (float)item.Value / collector.ExecSimulateStep;
                sb.Append(name)
                  .Append(": ")
                  .Append(expectation)
                  .AppendLine();
            }
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
            mTotalSimulateStepResult.Clear();
            mTotalResChanged.Clear();
            StatisticResultSB.Clear();
        }

        private bool ExchangeRecordCallback(ExchangeRecord record)
        {
            var sb = OutputResultSB;
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

            AppendExchangeStep(record);

            return true;
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
                  .AppendLine("次交换");

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
                  .Append("交换次数过多，省略了")
                  .Append(record.ExchangeStepIndex - collector.MaxExchangeRecord + 1)
                  .Append("次交换信息");

                AppendResChanged(record.ResValueRecords);
                sb.AppendLine();
            }
        }

        private void AppendExchangeStep(in ExchangeRecord record)
        {
            var sb = OutputResultSB;
            var collector = mExchangeDataCollector;

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
                        sb.Append('(');
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