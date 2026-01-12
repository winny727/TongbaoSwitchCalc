using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Simulation
{
    public abstract class Rule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public abstract void ExecuteRule(SwitchSimulator simulator, PlayerData playerData);
    }

    public class TongbaoPickRule : Rule
    {
        public int TargetPosIndex { get; private set; }

        public TongbaoPickRule(int targetPosIndex)
        {
            Name = $"优先交换钱盒槽位{targetPosIndex}里的通宝";
            TargetPosIndex = targetPosIndex;
        }

        public override void ExecuteRule(SwitchSimulator simulator, PlayerData playerData)
        {
            throw new NotImplementedException();
        }
    }

    public class AutoStopRule : Rule
    {
        public int TargetTongbaoId { get; private set; }

        public AutoStopRule(int targetTongbaoId)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(targetTongbaoId);
            if (config != null)
            {
                Name = $"交换出[{config.Name}]就停止";
            }
            else
            {
                Name = $"交换出通宝[ID={targetTongbaoId}]就停止";
            }
            TargetTongbaoId = targetTongbaoId;
        }

        public override void ExecuteRule(SwitchSimulator simulator, PlayerData playerData)
        {
            throw new NotImplementedException();
        }
    }
}
