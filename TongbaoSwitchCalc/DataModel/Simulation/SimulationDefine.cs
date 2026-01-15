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

        public static string GetSimulationRuleName(SimulationRuleType type)
        {
            switch (type)
            {
                case SimulationRuleType.PrioritySlot:
                    return "优先交换钱盒槽位";
                case SimulationRuleType.AutoStop:
                    return "交换出目标/降级通宝后切换槽位";
                case SimulationRuleType.ExpectationTongbao:
                    return "交换出期望通宝后停止交换";
                default:
                    break;
            }
            return string.Empty;
        }

        public static SimulationRule CreateSimulationRule(SimulationRuleType type, params object[] args)
        {
            switch (type)
            {
                case SimulationRuleType.PrioritySlot:
                    if (args != null && args.Length > 0 && args[0] is int prioritySlot)
                    {
                        return new PrioritySlotRule(prioritySlot);
                    }
                    break;
                case SimulationRuleType.AutoStop:
                    if (args != null && args.Length > 0 && args[0] is int targetTongbaoId)
                    {
                        return new AutoStopRule(targetTongbaoId);
                    }
                    break;
                case SimulationRuleType.ExpectationTongbao:
                    if (args != null && args.Length > 0 && args[0] is int expectedTongbaoId)
                    {
                        return new ExpectationTongbaoRule(expectedTongbaoId);
                    }
                    break;
                default:
                    break;
            }
            return null;
        }

        public static object[] GetSimulationRuleArgs(SimulationRule rule)
        {
            if (rule is PrioritySlotRule prioritySlotRule)
            {
                return new object[] { prioritySlotRule.PrioritySlotIndex };
            }
            else if (rule is AutoStopRule autoStopRule)
            {
                return new object[] { autoStopRule.TargetTongbaoId };
            }
            else if (rule is ExpectationTongbaoRule expectedTongbaoRule)
            {
                return new object[] { expectedTongbaoRule.ExpectedTongbaoId };
            }
            return null;
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
        LifePointLimit = 0, // 高级交换：根据目标生命确定一轮模拟；
        ExpectationTongbao = 1, // 期望通宝：不限制血量，根据是否交换出期望通宝确定一轮模拟（除非次数超过上限）；
    }

    public enum SimulateStepResult
    {
        Success = 0,
        LifePointLimitReached = 1,
        ExpectationAchieved = 2,
        TargetTongbaoFilledPrioritySlots = 3,
        SwitchStepLimitReached = 4,
        SwitchFailed = 5,
    }

    public enum SwitchStepResult
    {
        Success = 0,
        SelectedEmpty = 1,
        TongbaoCanNotSwitch = 2,
        LifePointNotEnough = 3,
        NoSwitchableTongbao = 4,
        UnknownError = 5,
    }

    public enum SimulationRuleType
    {
        PrioritySlot = 0,
        AutoStop = 1,
        ExpectationTongbao = 2,
    }
}
