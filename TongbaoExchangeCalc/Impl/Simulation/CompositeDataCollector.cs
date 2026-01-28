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
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                if (mDataCollectors[i] is TCollector collector)
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

        public void OnSimulateBegin(SimulationType type, int totalSimStep, PlayerData playerData)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnSimulateBegin(type, totalSimStep, playerData);
            }
        }

        public void OnSimulateEnd(int executedSimStep, float simCostTimeMS, PlayerData playerData)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnSimulateEnd(executedSimStep, simCostTimeMS, playerData);
            }
        }

        public void OnSimulateParallel(int estimatedLeftExchangeStep, int curSimStep)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnSimulateParallel(estimatedLeftExchangeStep, curSimStep);
            }
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnSimulateStepBegin(in context);
            }
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnSimulateStepEnd(in context, result);
            }
        }

        public void OnExchangeStepBegin(in SimulateContext context)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnExchangeStepBegin(in context);
            }
        }

        public void OnExchangeStepEnd(in SimulateContext context, ExchangeStepResult result)
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].OnExchangeStepEnd(in context, result);
            }
        }

        public IDataCollector<SimulateContext> CloneAsEmpty()
        {
            var collector = new CompositeDataCollector();
            foreach (var collectors in mDataCollectors)
            {
                collector.mDataCollectors.Add(collectors.CloneAsEmpty());
            }
            return collector;
        }

        public void MergeData(IDataCollector<SimulateContext> other)
        {
            if (other is CompositeDataCollector collector)
            {
                for (int i = 0; i < collector.mDataCollectors.Count; i++)
                {
                    var otherItem = collector.mDataCollectors[i];
                    for (int j = 0; j < mDataCollectors.Count; j++)
                    {
                        mDataCollectors[j].MergeData(otherItem);
                    }
                }
            }
        }

        public void ClearData()
        {
            for (int i = 0; i < mDataCollectors.Count; i++)
            {
                mDataCollectors[i].ClearData();
            }
        }
    }
}
