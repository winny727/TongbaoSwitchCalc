using System;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.Simulation
{
    public class WarpperThreadSafeDataCollector : IThreadSafeDataCollector<SimulateContext>
    {
        private readonly IDataCollector<SimulateContext> mInner;
        private readonly object mLock = new object();

        public WarpperThreadSafeDataCollector(IDataCollector<SimulateContext> inner)
        {
            mInner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void OnSimulateBegin(SimulationType type, int totalSimCount, in IReadOnlyPlayerData playerData)
        {
            lock (mLock)
            {
                mInner.OnSimulateBegin(type, totalSimCount, playerData);
            }
        }

        public void OnSimulateEnd(int executedSimCount, float simCostTimeMS, in IReadOnlyPlayerData playerData)
        {
            lock (mLock)
            {
                mInner.OnSimulateEnd(executedSimCount, simCostTimeMS, playerData);
            }
        }

        public void OnSimulateStepBegin(in SimulateContext context)
        {
            lock (mLock)
            {
                mInner.OnSimulateStepBegin(context);
            }
        }

        public void OnSimulateStepEnd(in SimulateContext context, SimulateStepResult result)
        {
            lock (mLock)
            {
                mInner.OnSimulateStepEnd(context, result);
            }
        }

        public void OnSwitchStepBegin(in SimulateContext context)
        {
            lock (mLock)
            {
                mInner.OnSwitchStepBegin(context);
            }
        }

        public void OnSwitchStepEnd(in SimulateContext context, SwitchStepResult result)
        {
            lock (mLock)
            {
                mInner.OnSwitchStepEnd(context, result);
            }
        }

        public void ClearData()
        {
            lock (mLock)
            {
                mInner.ClearData();
            }
        }
    }
}
