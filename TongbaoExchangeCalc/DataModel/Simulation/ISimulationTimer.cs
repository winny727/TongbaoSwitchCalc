using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public interface ISimulationTimer
    {
        public void Start();
        public float Stop();
    }
}
