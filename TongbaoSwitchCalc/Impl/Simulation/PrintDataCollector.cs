using System;
using System.Collections.Generic;
using System.Text;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class PrintDataCollector : IDataCollector<SimulateContext>
    {
        public bool RecordEverySwitch { get; set; } = true;
        //TODO 多线程先收集，最后再排序转字符串

        private SimulationType mSimulationType;
        private int mSimulationTotal = 0;
        private readonly Tongbao mTongbaoBefore = new Tongbao();
        private readonly Dictionary<ResType, int> mResBefore = new Dictionary<ResType, int>();

        private readonly StringBuilder mSwitchResultSB = new StringBuilder();
        private readonly StringBuilder mResChangedTempSB = new StringBuilder();
        public string LastSwitchResult => mSwitchResultSB.ToString();

        private readonly StringBuilder mOutputResult = new StringBuilder();
        public string OutputResult => mOutputResult.ToString();

        public void OnSimulateBegin(SimulationType type, int totalSimCount, in IReadOnlyPlayerData playerData)
        {
            mSimulationType = type;
            mSimulationTotal = totalSimCount;
            mOutputResult.Clear();
            mOutputResult.Append('[')
                         .Append(SimulationDefine.GetSimulationName(type))
                         .Append("]模拟开始，共计")
                         .Append(totalSimCount)
                         .AppendLine("次模拟");
        }

        public void OnSimulateEnd(int executedSimCount, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            mOutputResult.Append('[')
                         .Append(SimulationDefine.GetSimulationName(mSimulationType))
                         .Append("模拟完成，模拟次数: ")
                         .Append(executedSimCount)
                         .Append(", 模拟耗时: ")
                         .Append(simCostTimeMS)
                         .AppendLine("ms");
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            mOutputResult.Append("========第")
                         .Append(context.SimulationStepIndex + 1)
                         .Append('/')
                         .Append(mSimulationTotal)
                         .AppendLine("次模拟开始========");
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            string breakReason = SimulationDefine.GetSimulateStepEndReason(result);
            mOutputResult.Append("模拟结束，结束原因: ")
                         .Append(breakReason)
                         .AppendLine()
                         .Append("========第")
                         .Append(context.SimulationStepIndex + 1)
                         .Append('/')
                         .Append(mSimulationTotal)
                         .AppendLine("次模拟结束========");
        }

        public void OnSwitchStepBegin(in SimulateContext context)
        {
            if (!RecordEverySwitch)
            {
                return;
            }

            Tongbao tongbaoBefore = context.PlayerData.GetTongbao(context.SlotIndex);
            mTongbaoBefore.CopyFrom(tongbaoBefore);
            mResBefore.Clear();
            foreach (var item in context.PlayerData.ResValues)
            {
                mResBefore.Add(item.Key, item.Value);
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            if (!RecordEverySwitch)
            {
                return;
            }

            mSwitchResultSB.Clear();
            mResChangedTempSB.Clear();

            if (result == SwitchStepResult.Success)
            {
                Tongbao tongbaoAfter = context.PlayerData.GetTongbao(context.SlotIndex);

                // PlayerData的项只增加不删除，所以这里不需要考虑并集
                foreach (var item in context.PlayerData.ResValues)
                {
                    ResType type = item.Key;
                    int beforeValue = mResBefore.ContainsKey(type) ? mResBefore[type] : 0;
                    int afterValue = item.Value;
                    int changedValue = afterValue - beforeValue;
                    if (beforeValue != afterValue)
                    {
                        if (mResChangedTempSB.Length > 0)
                        {
                            mResChangedTempSB.Append("，");
                        }
                        mResChangedTempSB.Append(Define.GetResName(type));

                        if (changedValue > 0)
                        {
                            mResChangedTempSB.Append('+');
                        }
                        mResChangedTempSB.Append(changedValue);

                        mResChangedTempSB.Append(": ")
                                         .Append(beforeValue)
                                         .Append("->")
                                         .Append(afterValue);
                    }
                }

                mSwitchResultSB.Append("将位置[")
                               .Append(context.SlotIndex + 1)
                               .Append("]上的[")
                               .Append(mTongbaoBefore.Name)
                               .Append("]交换为[")
                               .Append(tongbaoAfter.Name)
                               .Append("] (")
                               .Append(mResChangedTempSB)
                               .Append(')');
            }
            else
            {
                switch (result)
                {
                    case SwitchStepResult.SelectedEmpty:
                        mSwitchResultSB.Append("交换失败，选中的位置[")
                                       .Append(context.SlotIndex + 1)
                                       .Append("]上的通宝为空");
                        break;
                    case SwitchStepResult.TongbaoCanNotSwitch:
                        mSwitchResultSB.Append("交换失败，通宝[")
                                       .Append(mTongbaoBefore.Name)
                                       .Append("]不可交换");
                        break;
                    case SwitchStepResult.LifePointNotEnough:
                        mSwitchResultSB.Append("交换失败，交换所需生命值不足");
                        break;
                    case SwitchStepResult.NoSwitchableTongbao:
                        mSwitchResultSB.Append("交换失败，通宝[")
                                       .Append(mTongbaoBefore.Name)
                                       .Append("]无可交换通宝");
                        break;
                    case SwitchStepResult.UnknownError:
                        mSwitchResultSB.Append("交换失败，未知错误");
                        break;
                    default:
                        break;
                }
            }

            mOutputResult.Append('(')
                         .Append(context.SimulationStepIndex + 1)
                         .Append('|')
                         .Append(context.SwitchStepIndex + 1)
                         .Append(") ")
                         .Append(LastSwitchResult)
                         .AppendLine();
        }

        public void ClearData()
        {
            mSwitchResultSB.Clear();
            mResChangedTempSB.Clear();
            mOutputResult.Clear();
        }
    }
}
