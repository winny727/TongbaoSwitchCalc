using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public interface IDataCollector<TContext> where TContext : struct
    {
        void OnSimulateBegin(SimulationType type, int totalSimCount, in IReadOnlyPlayerData playerData);
        void OnSimulateEnd(int executedSimCount, float simCostTimeMS, in IReadOnlyPlayerData playerData);
        void OnSimulateStepBegin(in TContext context);
        void OnSimulateStepEnd(in TContext context, SimulateStepResult result);
        void OnSwitchStepBegin(in TContext context);
        void OnSwitchStepEnd(in TContext context, SwitchStepResult result);
    }
}
