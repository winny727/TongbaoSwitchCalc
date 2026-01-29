using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class ParallelExchangeSimulator : ExchangeSimulator
    {
        public bool UseMultiThreadOptimize { get; set; } = true;
        public int OptimizeThreshold { get; set; } = 100000; // 触发多线程的阈值（预计剩余交换次数）
        public int MaxParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount - 1); // 最大线程数
        private bool mUseParallel;

        private CancellationToken mCancellationToken;

        public ParallelExchangeSimulator(PlayerData playerData, ISimulationTimer timer,
            IDataCollector<SimulateContext> collector = null) : base(playerData, timer, collector) { }

        public override void Simulate()
        {
            Simulate(CancellationToken.None);
        }

        public void Simulate(CancellationToken token, IProgress<int> progress = null)
        {
            mCancellationToken = token;
            mSimulationStepIndex = 0;
            ExchangeStepIndex = 0;
            mExchangeableSlotsPosIndex = 0;
            mPriorityPhaseFinished = false;
            mSimulateStepResult = SimulateStepResult.Success;
            mUseParallel = false;
            CachePlayerData();
            progress?.Report(0);
            mIsSimulating = true;
            DataCollector?.OnSimulateBegin(SimulationType, TotalSimulationCount, PlayerData);
            Timer.Start();
            while (SimulationStepIndex < TotalSimulationCount)
            {
                if (mCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                SimulateStep();
                mSimulationStepIndex++;
                progress?.Report(SimulationStepIndex);

                if (!UseMultiThreadOptimize)
                {
                    continue;
                }

                int estimatedLeftExchangeStep = ExchangeStepIndex * (TotalSimulationCount - SimulationStepIndex);
                if (!mUseParallel && estimatedLeftExchangeStep >= OptimizeThreshold)
                {
                    mUseParallel = true;
                    DataCollector?.OnSimulateParallel(estimatedLeftExchangeStep, SimulationStepIndex);
                    break;
                }
            }

            if (mUseParallel && SimulationStepIndex < TotalSimulationCount)
            {
                SimulateParallel(SimulationStepIndex, token, progress);
            }

            float time = Timer.Stop(); // ms
            mCancellationToken = CancellationToken.None;
            mIsSimulating = false;
            DataCollector?.OnSimulateEnd(SimulationStepIndex, time, PlayerData);
            progress?.Report(SimulationStepIndex + 1); // Count = Index + 1
        }

        private void SimulateParallel(int startIndex, CancellationToken token, IProgress<int> progress = null)
        {
            int remain = TotalSimulationCount - startIndex;
            if (remain <= 0)
            {
                return;
            }

            RevertPlayerData();
            int workerCount = Math.Min(MaxParallelism, remain);

            // 每个 worker 负责的模拟数量（向上取整）
            int batchSize = (remain + workerCount - 1) / workerCount;
            var dataCollectors = new IDataCollector<SimulateContext>[workerCount];

            try
            {
                Parallel.For(
                    0,
                    workerCount,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = workerCount,
                        CancellationToken = token,
                    },
                    workerIndex =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        int batchStart = startIndex + workerIndex * batchSize;
                        int batchEnd = Math.Min(batchStart + batchSize, startIndex + remain);

                        if (batchStart >= batchEnd)
                        {
                            return;
                        }

                        //Log($"{batchStart}->{batchEnd}");

                        ITongbaoSelector clonedTongbaoSelector = (ITongbaoSelector)PlayerData.TongbaoSelector.Clone();
                        IRandomGenerator clonedRandom = (IRandomGenerator)PlayerData.Random.Clone();
                        IDataCollector<SimulateContext> clonedDataCollector = DataCollector?.CloneAsEmpty();
                        if (clonedDataCollector is IShareContainer<SimulateContext> shareContainer)
                        {
                            shareContainer.ShareContainer(DataCollector);
                        }
                        dataCollectors[workerIndex] = clonedDataCollector;

                        PlayerData localPlayerData = new PlayerData(clonedTongbaoSelector, clonedRandom);
                        localPlayerData.CopyFrom(mRevertPlayerData);

                        var localSimulator = new ParallelExchangeSimulator(localPlayerData, Timer, clonedDataCollector)
                        {
                            SimulationType = SimulationType,
                            ExchangeSlotIndex = ExchangeSlotIndex,
                            ExchangeableSlots = new List<int>(ExchangeableSlots),
                            UnexchangeableTongbaoIds = new HashSet<int>(UnexchangeableTongbaoIds),
                            ExpectedTongbaoIds = new HashSet<int>(ExpectedTongbaoIds),
                            PriorityTongbaoIds = new HashSet<int>(PriorityTongbaoIds),
                            MinimumLifePoint = MinimumLifePoint,
                            TotalSimulationCount = 1,
                            mCancellationToken = mCancellationToken,
                        };
                        localSimulator.CachePlayerData();

                        localSimulator.mIsSimulating = true;
                        for (int simIndex = batchStart; simIndex < batchEnd; simIndex++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            localSimulator.mSimulationStepIndex = simIndex; //仅用于Context标识，不代表全局进度
                            localSimulator.SimulateStep();
                            Interlocked.Increment(ref mSimulationStepIndex); //总的Step+1
                            progress?.Report(mSimulationStepIndex);
                        }
                        localSimulator.mIsSimulating = false;
                    }
                );
            }
            catch (OperationCanceledException)
            {
                //Log("OperationCanceledException");
            }

            for (int i = 0; i < dataCollectors.Length; i++)
            {
                DataCollector.MergeData(dataCollectors[i]);
            }
        }

        protected override void ExchangeStep()
        {
            if (mCancellationToken.IsCancellationRequested)
            {
                BreakSimulationStep(SimulateStepResult.CancellationRequested);
                return;
            }

            base.ExchangeStep();
        }
    }
}
