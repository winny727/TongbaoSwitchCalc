using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public abstract class SimulationRule
    {
        public bool Enabled { get; set; } = true;
        public abstract SimulationRuleType Type { get; }
        public abstract void ApplyRule(SwitchSimulator simulator);
        public abstract void UnapplyRule(SwitchSimulator simulator);
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

    public class PrioritySlotRule : SlotIndexRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.PrioritySlot;

        public PrioritySlotRule(int slotIndex) : base(slotIndex) { }

        public override void ApplyRule(SwitchSimulator simulator)
        {
            if (!simulator.SlotIndexPriority.Contains(SlotIndex))
            {
                simulator.SlotIndexPriority.Add(SlotIndex);
            }
        }

        public override void UnapplyRule(SwitchSimulator simulator)
        {
            simulator.SlotIndexPriority.Remove(SlotIndex);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is PrioritySlotRule otherRule && otherRule.SlotIndex == SlotIndex;
        }

        public override string GetRuleString()
        {
            return $"优先交换钱盒槽位{SlotIndex + 1}里的通宝";
        }
    }

    public class AutoStopRule : TongbaoIdRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.AutoStop;

        public AutoStopRule(int targetId) : base(targetId) { }

        public override void ApplyRule(SwitchSimulator simulator)
        {
            simulator.TargetTongbaoIds.Add(TongbaoId);
        }

        public override void UnapplyRule(SwitchSimulator simulator)
        {
            simulator.TargetTongbaoIds.Remove(TongbaoId);
        }

        public override bool Equals(SimulationRule other)
        {
            return other is AutoStopRule otherRule && otherRule.TongbaoId == TongbaoId;
        }

        public override string GetRuleString()
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(TongbaoId);
            if (config == null)
            {
                return "无效规则，通宝配置错误";
            }
            return $"交换出{config.Name}就停止";
        }
    }

    public class ExpectationTongbaoRule : TongbaoIdRule
    {
        public override SimulationRuleType Type { get; } = SimulationRuleType.ExpectationTongbao;

        public ExpectationTongbaoRule(int id) : base(id) { }

        public override void ApplyRule(SwitchSimulator simulator)
        {
            simulator.ExpectedTongbaoId = TongbaoId;
        }

        public override void UnapplyRule(SwitchSimulator simulator)
        {
            if (simulator.ExpectedTongbaoId == TongbaoId)
            {
                simulator.ExpectedTongbaoId = -1;
            }
        }

        public override bool Equals(SimulationRule other)
        {
            return true; // 限制期望通宝只能存在一个
        }

        public override string GetRuleString()
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(TongbaoId);
            if (config == null)
            {
                return "无效规则，通宝配置错误";
            }
            return $"期望获得{config.Name}";
        }
    }
}
