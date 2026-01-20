using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class CompositeDataCollector : IDataCollector<SimulateContext>
    {
        private readonly List<IDataCollector<SimulateContext>> mDataCollectors = new List<IDataCollector<SimulateContext>>();

        public void AddDataCollector(IDataCollector<SimulateContext> collector)
        {
            if (collector != null && !mDataCollectors.Contains(collector) && collector != this)
            {
                mDataCollectors.Add(collector);
            }
        }

        public void RemoveDataCollector(IDataCollector<SimulateContext> collector)
        {
            if (collector != null && mDataCollectors.Contains(collector))
            {
                mDataCollectors.Remove(collector);
            }
        }

        public TCollector GetDataCollector<TCollector>() where TCollector : class, IDataCollector<SimulateContext>
        {
            foreach (var dataCollector in mDataCollectors)
            {
                if (dataCollector is TCollector collector)
                {
                    return collector;
                }
            }
            return null;
        }

        public void ClearDataCollectors()
        {
            mDataCollectors.Clear();
        }

        public void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateBegin(type, totalSimStep, in playerData);
            }
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateEnd(executedSimStep, simCostTimeMS, in playerData);
            }
        }

        public void OnSimulateParallel(int estimatedLeftSwitchStep, int remainSimStep)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateParallel(estimatedLeftSwitchStep, remainSimStep);
            }
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateStepBegin(in context);
            }
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateStepEnd(in context, result);
            }
        }

        public void OnSwitchStepBegin(in SimulateContext context)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSwitchStepBegin(in context);
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSwitchStepEnd(in context, result);
            }
        }

        public void ClearData()
        {
            foreach (var collector in mDataCollectors)
            {
                collector.ClearData();
            }
        }
    }
}
