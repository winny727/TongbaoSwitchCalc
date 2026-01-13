using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public static class SimulationDefine
    {
        public static string GetSimulationName(SimulationType type)
        {
            switch (type)
            {
                case SimulationType.LifePointLimit:
                    return "高级交换";
                case SimulationType.ExpectationTongbao:
                    return "期望通宝";
                default:
                    break;
            }
            return string.Empty;
        }

        public static string GetSimulateStepEndReason(SimulateStepResult type)
        {
            switch (type)
            {
                case SimulateStepResult.Success:
                    return "正常完成";
                case SimulateStepResult.LifePointLimitReached:
                    return "已达目标生命值限制";
                case SimulateStepResult.ExpectationAchieved:
                    return "已获得期望通宝";
                case SimulateStepResult.TargetTongbaoFilledPrioritySlots:
                    return "目标/降级通宝已填满优先槽位";
                case SimulateStepResult.SwitchStepLimitReached:
                    return "已达交换次数限制";
                case SimulateStepResult.SwitchFailed:
                    return "交换失败";
                default:
                    break;
            }
            return string.Empty;
        }
    }

    public struct SimulateContext
    {
        public int SimulationStepIndex { get; private set; }
        public int SwitchStepIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public IReadOnlyPlayerData PlayerData { get; private set; }

        public SimulateContext(int simulationStepIndex, int switchStepIndex, int slotIndex, IReadOnlyPlayerData playerData)
        {
            SimulationStepIndex = simulationStepIndex;
            SwitchStepIndex = switchStepIndex;
            SlotIndex = slotIndex;
            PlayerData = playerData;
        }
    }

    public enum SimulationType
    {
        LifePointLimit, // 高级交换：根据目标生命确定一轮模拟；
        ExpectationTongbao, // 期望通宝：不限制血量，根据是否交换出期望通宝确定一轮模拟（除非次数超过上限）；
    }

    public enum SimulateStepResult
    {
        Success,
        LifePointLimitReached,
        ExpectationAchieved,
        TargetTongbaoFilledPrioritySlots,
        SwitchStepLimitReached,
        SwitchFailed,
    }

    public enum SwitchStepResult
    {
        Success,
        SelectedEmpty,
        TongbaoCanNotSwitch,
        LifePointNotEnough,
        UnknownError,
    }

}
