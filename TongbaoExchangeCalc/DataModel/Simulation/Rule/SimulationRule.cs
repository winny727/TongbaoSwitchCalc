using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public abstract class SimulationRule
    {
        public bool Enabled { get; set; } = true;
        public abstract SimulationRuleType Type { get; }
        public abstract void ApplyRule(ExchangeSimulator simulator);
        public abstract void UnapplyRule(ExchangeSimulator simulator);
        public abstract bool Equals(SimulationRule other);
        public abstract string GetRuleString();
    }

    public abstract class IntParamRule : SimulationRule
    {
        public int IntParam { get; private set; }

        public IntParamRule(int param)
        {
            IntParam = param;
        }
    }

    public abstract class SlotIndexRule : IntParamRule
    {
        public int SlotIndex => IntParam;

        public SlotIndexRule(int slotIndex) : base(slotIndex) { }
    }

    public abstract class TongbaoIdRule : IntParamRule
    {
        public int TongbaoId => IntParam;

        public TongbaoIdRule(int tongbaoId) : base(tongbaoId) { }
    }

    public class UnexchangeableTongbaoRule : TongbaoIdRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.UnexchangeableTongbao;

        public UnexchangeableTongbaoRule(int targetId) : base(targetId) { }

        public override void ApplyRule(ExchangeSimulator simulator)
        {
            simulator.UnexchangeableTongbaoIds.Add(TongbaoId);
        }

        public override void UnapplyRule(ExchangeSimulator simulator)
        {
            simulator.UnexchangeableTongbaoIds.Remove(TongbaoId);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is UnexchangeableTongbaoRule otherRule && otherRule.TongbaoId == TongbaoId;
        }

        public override string GetRuleString()
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(TongbaoId);
            if (config == null)
            {
                return "无效规则，通宝配置错误";
            }
            return $"交换出[{Define.GetTongbaoTypeName(config.Type)}-{config.Name}]就停止";
        }
    }

    public class ExpectationTongbaoRule : TongbaoIdRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.ExpectationTongbao;

        public ExpectationTongbaoRule(int id) : base(id) { }

        public override void ApplyRule(ExchangeSimulator simulator)
        {
            simulator.ExpectedTongbaoIds.Add(TongbaoId);
        }

        public override void UnapplyRule(ExchangeSimulator simulator)
        {
            simulator.ExpectedTongbaoIds.Remove(TongbaoId);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is ExpectationTongbaoRule otherRule && otherRule.TongbaoId == TongbaoId;
        }

        public override string GetRuleString()
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(TongbaoId);
            if (config == null)
            {
                return "无效规则，通宝配置错误";
            }
            return $"期望获得[{Define.GetTongbaoTypeName(config.Type)}-{config.Name}]";
        }
    }

    public class ExchangeableSlotRule : SlotIndexRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.ExchangeableSlot;

        public ExchangeableSlotRule(int slotIndex) : base(slotIndex) { }

        public override void ApplyRule(ExchangeSimulator simulator)
        {
            if (!simulator.ExchangeableSlots.Contains(SlotIndex))
            {
                simulator.ExchangeableSlots.Add(SlotIndex);
            }
        }

        public override void UnapplyRule(ExchangeSimulator simulator)
        {
            simulator.ExchangeableSlots.Remove(SlotIndex);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is ExchangeableSlotRule otherRule && otherRule.SlotIndex == SlotIndex;
        }

        public override string GetRuleString()
        {
            return $"优先交换钱盒槽位{SlotIndex + 1}里的通宝";
        }
    }

    public class PriorityExchangeTongbaoRule : TongbaoIdRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.PriorityExchangeTongbao;

        public PriorityExchangeTongbaoRule(int id) : base(id) { }

        public override void ApplyRule(ExchangeSimulator simulator)
        {
            simulator.PriorityTongbaoIds.Add(TongbaoId);
        }

        public override void UnapplyRule(ExchangeSimulator simulator)
        {
            simulator.PriorityTongbaoIds.Remove(TongbaoId);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is PriorityExchangeTongbaoRule otherRule && otherRule.TongbaoId == TongbaoId;
        }

        public override string GetRuleString()
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(TongbaoId);
            if (config == null)
            {
                return "无效规则，通宝配置错误";
            }
            return $"优先交换[{Define.GetTongbaoTypeName(config.Type)}-{config.Name}]";
        }
    }
}
