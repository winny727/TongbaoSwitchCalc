using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
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
                    return "期望通宝-不限次数";
                case SimulationType.ExpectationTongbao_Limited:
                    return "期望通宝-血量限制";
                default:
                    break;
            }
            return string.Empty;
        }

        private static readonly string mExchangeStepLimitStr = $"超过模拟交换次数上限({ExchangeSimulator.EXCHANGE_STEP_LIMIT})";
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
                case SimulateStepResult.TargetFilledExchangeableSlots:
                    return "目标/降级通宝已填满可交换槽位";
                case SimulateStepResult.ExchangeStepLimitReached:
                    return mExchangeStepLimitStr;
                case SimulateStepResult.ExchangeFailed:
                    return "交换失败";
                case SimulateStepResult.CancellationRequested:
                    return "用户取消";
                default:
                    break;
            }
            return string.Empty;
        }

        public static string GetSimulationRuleName(SimulationRuleType type)
        {
            switch (type)
            {
                case SimulationRuleType.ExchangeableSlot:
                    return "可交换钱盒槽位";
                case SimulationRuleType.UnexchangeableTongbao:
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
                case SimulationRuleType.ExchangeableSlot:
                    if (args != null && args.Length > 0 && args[0] is int exchangeableSlot)
                    {
                        return new ExchangeableSlotRule(exchangeableSlot);
                    }
                    break;
                case SimulationRuleType.UnexchangeableTongbao:
                    if (args != null && args.Length > 0 && args[0] is int targetTongbaoId)
                    {
                        return new UnexchangeableTongbaoRule(targetTongbaoId);
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
            if (rule is IntParamRule intParamRule)
            {
                return new object[] { intParamRule.IntParam };
            }
            return null;
        }
    }

    public struct SimulateContext
    {
        public int SimulationStepIndex { get; private set; }
        public int ExchangeStepIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public IReadOnlyPlayerData PlayerData { get; private set; }

        public SimulateContext(int simulationStepIndex, int exchangeStepIndex, int slotIndex, IReadOnlyPlayerData playerData)
        {
            SimulationStepIndex = simulationStepIndex;
            ExchangeStepIndex = exchangeStepIndex;
            SlotIndex = slotIndex;
            PlayerData = playerData;
        }
    }

    public enum SimulationType
    {
        LifePointLimit = 0, // 高级交换：根据目标生命确定一轮模拟；
        ExpectationTongbao = 1, // 期望通宝：不限制血量，根据是否交换出期望通宝确定一轮模拟（除非次数超过上限）；
        ExpectationTongbao_Limited = 2, // 期望通宝：限制血量，根据是否交换出期望通宝确定一轮模拟（除非次数超过上限）；
    }

    public enum SimulateStepResult
    {
        Success = 0,
        LifePointLimitReached = 1,
        ExpectationAchieved = 2,
        TargetFilledExchangeableSlots = 3,
        ExchangeStepLimitReached = 4,
        ExchangeFailed = 5,
        CancellationRequested = 6,
    }

    public enum ExchangeStepResult
    {
        Success = 0,
        SelectedEmpty = 1,
        TongbaoUnexchangeable = 2, // 当前选中的通宝不可交换
        LifePointNotEnough = 3,
        ExchangeableTongbaoNotExist = 4, // 没有可以被交换出来的通宝
        UnknownError = 5,
    }

    public enum SimulationRuleType
    {
        UnexchangeableTongbao = 0, // 不可交换通宝，交换到不可交换通宝就切换槽位
        ExpectationTongbao = 1, // 期望通宝，交换到所有期望通宝就停止交换
        ExchangeableSlot = 2, // 可交换槽位
        //PriorityExchangeTongbao = 3, // 优先交换通宝
    }
}
