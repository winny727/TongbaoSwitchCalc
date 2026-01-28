using System;
using System.Collections.Generic;
using System.Text;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class StatisticDataCollector : IDataCollector<SimulateContext>
    {
        private int mTotalSimulateStep;
        private int mExecSimulateStep;
        private float mTotalSimulateTime;
        private int mTotalExchangeStep;
        private bool mIsSimulateParallel;
        private readonly Dictionary<SimulateStepResult, int> mTotalSimulateStepResult = new Dictionary<SimulateStepResult, int>();
        private readonly Dictionary<int, Dictionary<ResType, int>> mTempResBefore = new Dictionary<int, Dictionary<ResType, int>>(); // key: simulationStepIndex
        private readonly Dictionary<ResType, int> mTotalResChanged = new Dictionary<ResType, int>();

        private readonly Stack<Dictionary<ResType, int>> mResDictPool = new Stack<Dictionary<ResType, int>>();

        private readonly StringBuilder mTempStringBuilder = new StringBuilder();

        private Dictionary<ResType, int> AllocateResDict()
        {
            if (mResDictPool.Count > 0)
            {
                return mResDictPool.Pop();
            }
            return new Dictionary<ResType, int>();
        }

        private void RecycleResDict(Dictionary<ResType, int> resDict)
        {
            if (resDict != null && !mResDictPool.Contains(resDict))
            {
                resDict.Clear();
                mResDictPool.Push(resDict);
            }
        }

        public void OnSimulateBegin(SimulationType type, int totalSimStep, PlayerData playerData)
        {
            ClearData();
            mTotalSimulateStep = totalSimStep;
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, PlayerData playerData)
        {
            mExecSimulateStep = executedSimStep;
            mTotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int curSimStep)
        {
            mIsSimulateParallel = true;
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            mTempResBefore.Add(context.SimulationStepIndex, AllocateResDict());
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            if (!mTotalSimulateStepResult.ContainsKey(result))
            {
                mTotalSimulateStepResult.Add(result, 0);
            }
            mTotalSimulateStepResult[result]++;

            RecycleResDict(mTempResBefore[context.SimulationStepIndex]);
            mTempResBefore.Remove(context.SimulationStepIndex);
        }

        public void OnExchangeStepBegin(in SimulateContext context)
        {
            var resDict = mTempResBefore[context.SimulationStepIndex];
            resDict.Clear();
            foreach (var item in context.PlayerData.ResValues)
            {
                resDict.Add(item.Key, item.Value);
            }
        }

        public void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            if (result == ExchangeStepResult.Success)
            {
                mTotalExchangeStep++;
                foreach (var item in context.PlayerData.ResValues)
                {
                    ResType type = item.Key;
                    int afterValue = item.Value;
                    mTempResBefore[context.SimulationStepIndex].TryGetValue(type, out int beforeValue);
                    int changedValue = afterValue - beforeValue;
                    if (!mTotalResChanged.ContainsKey(type))
                    {
                        mTotalResChanged.Add(type, 0);
                    }
                    mTotalResChanged[type] += changedValue;
                }
            }
        }

        public string GetOutputResult()
        {
            mTempStringBuilder.Clear();
            mTempStringBuilder.Append("模拟完成");

            if (mIsSimulateParallel)
            {
                mTempStringBuilder.Append("(触发多线程优化)");
            }

            mTempStringBuilder.AppendLine("，模拟次数: ")
                              .Append(mExecSimulateStep)
                              .Append('/')
                              .Append(mTotalSimulateStep)
                              .Append(", 模拟耗时: ")
                              .Append(mTotalSimulateTime)
                              .Append("ms")
                              .Append(", 成功交换总次数: ")
                              .Append(mTotalExchangeStep)
                              .AppendLine()
                              .AppendLine();

            mTempStringBuilder.AppendLine("模拟结果统计: ");
            foreach (var item in mTotalSimulateStepResult)
            {
                string name = SimulationDefine.GetSimulateStepEndReason(item.Key);
                float percent = item.Value * 100f / mExecSimulateStep;
                mTempStringBuilder.Append(name)
                                  .Append(": ")
                                  .Append(item.Value)
                                  .Append(" (")
                                  .Append(percent)
                                  .AppendLine("%)");
            }
            mTempStringBuilder.AppendLine();


            mTempStringBuilder.AppendLine("期望资源变化:");
            foreach (var item in mTotalResChanged)
            {
                string name = Define.GetResName(item.Key);
                float expectation = (float)item.Value / mExecSimulateStep;
                mTempStringBuilder.Append(name)
                                  .Append(": ")
                                  .Append(expectation)
                                  .AppendLine();
            }

            return mTempStringBuilder.ToString();
        }

        public IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new StatisticDataCollector
            {
                mTotalSimulateStep = mTotalSimulateStep,
                mIsSimulateParallel = mIsSimulateParallel,
            };
            return collector;
        }

        public void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is StatisticDataCollector collector)
            {
                mTotalExchangeStep += collector.mTotalExchangeStep;
                foreach (var item in collector.mTotalSimulateStepResult)
                {
                    if (!mTotalSimulateStepResult.ContainsKey(item.Key))
                    {
                        mTotalSimulateStepResult.Add(item.Key, 0);
                    }
                    mTotalSimulateStepResult[item.Key] += item.Value;
                }
                foreach (var item in collector.mTotalResChanged)
                {
                    if (!mTotalResChanged.ContainsKey(item.Key))
                    {
                        mTotalResChanged.Add(item.Key, 0);
                    }
                    mTotalResChanged[item.Key] += item.Value;
                }
            }
        }

        public void ClearData()
        {
            mTotalSimulateStep = 0;
            mTotalSimulateTime = 0;
            mExecSimulateStep = 0;
            mTotalExchangeStep = 0;
            mIsSimulateParallel = false;
            mTotalSimulateStepResult.Clear();
            mTotalResChanged.Clear();
            mTempStringBuilder.Clear();
            foreach (var item in mTempResBefore)
            {
                RecycleResDict(item.Value);
            }
            mTempResBefore.Clear();
        }
    }
}
