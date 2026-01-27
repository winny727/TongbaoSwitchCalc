using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Impl.Simulation
{
    public class SimulationTimer : ISimulationTimer
    {
        private CodeTimer mCodeTimer;

        public void Start()
        {
            mCodeTimer?.Dispose();
            mCodeTimer = CodeTimer.StartNew("Simulate");
        }

        public float Stop()
        {
            float time = mCodeTimer?.ElapsedMilliseconds ?? -1;
            mCodeTimer?.Dispose();
            mCodeTimer = null;
            return time;
        }
    }
}
