using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
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

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int remainSimStep)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnSimulateParallel(estimatedLeftExchangeStep, remainSimStep);
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

        public void OnExchangeStepBegin(in SimulateContext context)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnExchangeStepBegin(in context);
            }
        }

        public void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.OnExchangeStepEnd(in context, result);
            }
        }

        public IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new CompositeDataCollector();
            foreach (var item in mDataCollectors)
            {
                collector.mDataCollectors.Add(item.CloneAsEmpty());
            }
            return collector;
        }

        public void SetCollectRange(int offset, int length)
        {
            foreach (var collector in mDataCollectors)
            {
                collector.SetCollectRange(offset, length);
            }
        }

        public void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is CompositeDataCollector collector)
            {
                foreach (var otherItem in collector.mDataCollectors)
                {
                    foreach (var item in mDataCollectors)
                    {
                        item.MergeData(otherItem);
                    }
                }
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
