using System;
using System.Collections.Generic;
using System.Text;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class StatisticDataCollector : IDataCollector<SimulateContext>
    {
        private int mTotalSimulateCount;
        private float mTotalSimulateTime;
        private int mTotalSwitchCount;
        private readonly Dictionary<SimulateStepResult, int> mTotalSimulateStepResult = new Dictionary<SimulateStepResult, int>();
        private readonly Dictionary<ResType, int> mTempResBefore = new Dictionary<ResType, int>();
        private readonly Dictionary<ResType, int> mTotalResChanged = new Dictionary<ResType, int>();

        private readonly StringBuilder mTempStringBuilder = new StringBuilder();

        public void OnSimulateBegin(SimulationType type, int totalSimCount, in IReadOnlyPlayerData playerData)
        {
            ClearData();
        }

        public void OnSimulateEnd(int executedSimCount, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            mTotalSimulateCount = executedSimCount;
            mTotalSimulateTime = simCostTimeMS;
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {

        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            if (!mTotalSimulateStepResult.ContainsKey(result))
            {
                mTotalSimulateStepResult.Add(result, 0);
            }
            mTotalSimulateStepResult[result]++;
        }

        public void OnSwitchStepBegin(in SimulateContext context)
        {
            mTempResBefore.Clear();
            foreach (var item in context.PlayerData.ResValues)
            {
                mTempResBefore.Add(item.Key, item.Value);
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            if (result == SwitchStepResult.Success)
            {
                mTotalSwitchCount++;
                foreach (var item in context.PlayerData.ResValues)
                {
                    ResType type = item.Key;
                    int afterValue = item.Value;
                    mTempResBefore.TryGetValue(type, out int beforeValue);
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
            mTempStringBuilder.AppendLine("模拟完成，总模拟次数: ")
                              .Append(mTotalSimulateCount)
                              .Append(", 模拟耗时: ")
                              .Append(mTotalSimulateTime)
                              .Append("ms")
                              .Append(", 总交换次数: ")
                              .Append(mTotalSwitchCount)
                              .AppendLine()
                              .AppendLine();

            mTempStringBuilder.AppendLine("模拟结果统计: ");
            foreach (var item in mTotalSimulateStepResult)
            {
                string name = SimulationDefine.GetSimulateStepEndReason(item.Key);
                float p = (float)item.Value / mTotalSimulateCount;
                mTempStringBuilder.Append(name)
                                  .Append(": ")
                                  .Append(item.Value)
                                  .Append('(')
                                  .Append(p * 100)
                                  .AppendLine("%)");
            }
            mTempStringBuilder.AppendLine();


            mTempStringBuilder.AppendLine("期望资源变化:");
            foreach (var item in mTotalResChanged)
            {
                string name = Define.GetResName(item.Key);
                float expectation = (float)item.Value / mTotalSimulateCount;
                mTempStringBuilder.Append(name)
                                  .Append(": ")
                                  .Append(expectation)
                                  .AppendLine();
            }

            return mTempStringBuilder.ToString();
        }

        public void ClearData()
        {
            mTotalSimulateCount = 0;
            mTotalSwitchCount = 0;
            mTotalSimulateStepResult.Clear();
            mTotalResChanged.Clear();
            mTempStringBuilder.Clear();
        }
    }
}
