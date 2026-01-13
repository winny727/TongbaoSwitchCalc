using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    public interface IReadOnlyPlayerData
    {
        SquadType SquadType { get; }
        int SwitchCount { get; }
        int MaxTongbaoCount { get; }
        int NextSwitchCostLifePoint { get; }
        IReadOnlyDictionary<ResType, int> ResValues { get; }

        Tongbao GetTongbao(int slotIndex);
        bool IsTongbaoLocked(int id);
        int GetResValue(ResType type);
        bool HasSpecialCondition(SpecialConditionFlag specialCondition);
    }
}
