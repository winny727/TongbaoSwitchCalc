using System;
using System.Collections.Generic;
using System.Text;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class StatisticDataCollector : IDataCollector<SimulateContext>
    {
        private int mTotalSimulateStep;
        private int mExecSimulateStep;
        private float mTotalSimulateTime;
        private int mTotalSwitchStep;
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

        public void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            ClearData();
            mTotalSimulateStep = totalSimStep;
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            mExecSimulateStep = executedSimStep;
            mTotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateParallel(int estimatedLeftSwitchStep, int curSimStep)
        {

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

        public void OnSwitchStepBegin(in SimulateContext context)
        {
            var resDict = mTempResBefore[context.SimulationStepIndex];
            resDict.Clear();
            foreach (var item in context.PlayerData.ResValues)
            {
                resDict.Add(item.Key, item.Value);
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            if (result == SwitchStepResult.Success)
            {
                mTotalSwitchStep++;
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
            mTempStringBuilder.AppendLine("模拟完成，模拟次数: ")
                              .Append(mExecSimulateStep)
                              .Append('/')
                              .Append(mTotalSimulateStep)
                              .Append(", 模拟耗时: ")
                              .Append(mTotalSimulateTime)
                              .Append("ms")
                              .Append(", 总交换次数: ")
                              .Append(mTotalSwitchStep)
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

        public void ClearData()
        {
            mTotalSimulateStep = 0;
            mTotalSimulateTime = 0;
            mExecSimulateStep = 0;
            mTotalSwitchStep = 0;
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
