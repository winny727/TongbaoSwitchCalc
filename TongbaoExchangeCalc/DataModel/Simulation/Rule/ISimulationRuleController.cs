using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public interface ISimulationRuleController
    {
        void ApplySimulationRule(ExchangeSimulator simulator); // readonly
    }
}
