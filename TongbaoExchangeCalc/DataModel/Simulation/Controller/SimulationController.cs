using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class SimulationController
    {
        private readonly ExchangeSimulator mExchangeSimulator;

        private CancellationTokenSource mCancellationTokenSource;
        public bool IsAsyncSimulating => mCancellationTokenSource != null;

        public SimulationController(PlayerData playerData, ISimulationTimer timer, IDataCollector<SimulateContext> dataCollector = null)
        {
            if (playerData == null)
            {
                throw new ArgumentNullException(nameof(playerData));
            }
            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }
            //mExchangeSimulator = new ExchangeSimulator(playerData, timer, dataCollector);
            mExchangeSimulator = new ParallelExchangeSimulator(playerData, timer, dataCollector);
        }

        public void Simulate(SimulationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ApplySimulationOptions(mExchangeSimulator, options);
            SimulateInternal(CancellationToken.None);
        }

        public async Task SimulateAsync(SimulationOptions options, IProgress<int> progress = null)
        {
            if (IsAsyncSimulating)
            {
                throw new InvalidOperationException("Simulation Executing.");
            }

            ApplySimulationOptions(mExchangeSimulator, options);
            mCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Run(() => SimulateInternal(mCancellationTokenSource.Token, progress), mCancellationTokenSource.Token);
            }
            finally
            {
                mCancellationTokenSource.Dispose();
                mCancellationTokenSource = null;
            }
        }

        public void CancelSimulate()
        {
            mCancellationTokenSource?.Cancel();
        }

        public void RevertPlayerData()
        {
            mExchangeSimulator?.RevertPlayerData();
        }

        private void SimulateInternal(CancellationToken token, IProgress<int> progress = null)
        {
            if (mExchangeSimulator is ParallelExchangeSimulator parallelSimulator)
            {
                parallelSimulator.Simulate(token, progress);
            }
            else
            {
                mExchangeSimulator.Simulate();
            }
        }

        private void ApplySimulationOptions(ExchangeSimulator simulator, SimulationOptions options)
        {
            if (simulator == null || options == null)
            {
                return;
            }

            simulator.SimulationType = options.SimulationType;
            simulator.TotalSimulationCount = options.TotalSimulationCount;
            simulator.MinimumLifePoint = options.MinimumLifePoint;
            simulator.ExchangeSlotIndex = options.ExchangeSlotIndex;
            if (simulator is ParallelExchangeSimulator parallelSimulator)
            {
                parallelSimulator.UseMultiThreadOptimize = options.UseMultiThreadOptimize;
            }
            options.RuleController?.ApplySimulationRule(simulator); // ApplyRule
        }
    }
}
