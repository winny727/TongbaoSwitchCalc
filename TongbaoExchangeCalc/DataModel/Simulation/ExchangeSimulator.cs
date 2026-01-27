using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class ExchangeSimulator
    {
        public PlayerData PlayerData { get; private set; }
        public ISimulationTimer Timer { get; private set; }
        public IDataCollector<SimulateContext> DataCollector { get; private set; }

        public SimulationType SimulationType { get; set; } = SimulationType.LifePointLimit;
        public int MinimumLifePoint { get; set; } = 1; // 最小生命值限制
        public int TotalSimulationCount { get; set; } = 1000;

        // 自定义交换策略
        public List<int> ExchangeableSlots { get; private set; } = new List<int>(); // 可交换槽位
        public HashSet<int> UnexchangeableTongbaoIds { get; private set; } = new HashSet<int>(); // 不可交换通宝ID集合
        public HashSet<int> PriorityTongbaoIds { get; private set; } = new HashSet<int>(); // 优先通宝ID集合
        public HashSet<int> ExpectedTongbaoIds { get; private set; } = new HashSet<int>(); // 期望通宝ID集合

        private int mSimulationStepIndex = 0;
        public int SimulationStepIndex => mSimulationStepIndex;
        public int ExchangeStepIndex { get; private set; } = 0; // 包括交换失败
        public int ExchangeSlotIndex { get; set; } = -1;

        private SimulateStepResult mSimulateStepResult = SimulateStepResult.Success;
        private bool mIsSimulating = false;
        private int mExchangeableSlotsPosIndex = 0;
        private bool mPriorityPhaseFinished = false;
        private int mInitialExchangeSlotIndex;
        private readonly PlayerData mRevertPlayerData;

        public const int EXCHANGE_STEP_LIMIT = 10000; // 单轮循环的交换上限，防止死循环

        public bool UseMultiThreadOptimize { get; set; } = true;
        public int OptimizeThreshold { get; set; } = 100000; // 触发多线程的阈值（预计剩余交换次数）
        public int MaxParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount - 1); // 最大线程数
        private bool mUseParallel;

        //private void Log(string msg) => System.Diagnostics.Debug.WriteLine(msg);

        public ExchangeSimulator(PlayerData playerData, ISimulationTimer timer, IDataCollector<SimulateContext> collector = null)
        {
            PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            Timer = timer ?? throw new ArgumentNullException(nameof(timer));
            DataCollector = collector;

            mRevertPlayerData = new PlayerData(playerData.TongbaoSelector, playerData.Random);
        }

        public void SetDataCollector(IDataCollector<SimulateContext> collector)
        {
            DataCollector = collector;
        }

        public void RevertPlayerData()
        {
            PlayerData.CopyFrom(mRevertPlayerData, true);
            ExchangeSlotIndex = mInitialExchangeSlotIndex;
        }

        public void CachePlayerData()
        {
            mRevertPlayerData.CopyFrom(PlayerData);
            mInitialExchangeSlotIndex = ExchangeSlotIndex;
        }

        public void Simulate()
        {
            Simulate(CancellationToken.None);
        }

        public void Simulate(CancellationToken token, IProgress<int> progress = null)
        {
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
                if (token.IsCancellationRequested)
                {
                    break;
                }

                SimulateStep(token);
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
                        clonedDataCollector?.ShareContainer(DataCollector);
                        dataCollectors[workerIndex] = clonedDataCollector;

                        PlayerData localPlayerData = new PlayerData(clonedTongbaoSelector, clonedRandom);
                        localPlayerData.CopyFrom(mRevertPlayerData);

                        var localSimulator = new ExchangeSimulator(localPlayerData, Timer, clonedDataCollector)
                        {
                            SimulationType = SimulationType,
                            ExchangeSlotIndex = ExchangeSlotIndex,
                            ExchangeableSlots = new List<int>(ExchangeableSlots),
                            UnexchangeableTongbaoIds = new HashSet<int>(UnexchangeableTongbaoIds),
                            ExpectedTongbaoIds = new HashSet<int>(ExpectedTongbaoIds),
                            PriorityTongbaoIds = new HashSet<int>(PriorityTongbaoIds),
                            MinimumLifePoint = MinimumLifePoint,
                            TotalSimulationCount = 1,
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
                            localSimulator.SimulateStep(token);
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


        private void SimulateStep(CancellationToken token)
        {
            RevertPlayerData();
            ExchangeStepIndex = 0;
            mExchangeableSlotsPosIndex = 0;
            mPriorityPhaseFinished = false;
            if (ExchangeableSlots.Count > 0)
            {
                //Log($"初始可交换槽位(#{mExchangeableSlotsPosIndex}): {ExchangeSlotIndex}");
                ExchangeSlotIndex = ExchangeableSlots[0];
            }
            mSimulateStepResult = SimulateStepResult.Success;
            DataCollector?.OnSimulateStepBegin(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, ExchangeSlotIndex, PlayerData));
            while (ExchangeStepIndex < EXCHANGE_STEP_LIMIT)
            {
                if (mSimulateStepResult != SimulateStepResult.Success)
                {
                    break;
                }

                ExchangeStep(token);
                ExchangeStepIndex++;
            }
            if (ExchangeStepIndex >= EXCHANGE_STEP_LIMIT && mSimulateStepResult == SimulateStepResult.Success)
            {
                mSimulateStepResult = SimulateStepResult.ExchangeStepLimitReached;
                ExchangeStepIndex--; //修正为最后一次的Index (最后一次是成功交换的)
            }
            else
            {
                ExchangeStepIndex -= 2; //修正为最后一次交换成功的轮次的Index (倒数第二次是成功交换的)
            }
            DataCollector?.OnSimulateStepEnd(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, ExchangeSlotIndex, PlayerData), mSimulateStepResult);
        }

        private void ExchangeStep(CancellationToken token)
        {
            /*
            模拟停止规则：
            高级交换：根据目标生命停止模拟；
            期望通宝-不限次数：不限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            期望通宝-限制血量：限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            通用规则：
            交换可以交换槽位的优先交换通宝，直到没有优先交换通宝时，开始下一项
            按照可以交换槽位的顺序交换通宝，将要交换的通宝是不交换的通宝时，切换到下一槽位
            已经持有所有期望通宝或在最后一个槽位需要切换到下一个槽位时，停止交换
            优先交换通宝和不交换通宝冲突时，按不交换算
            */

            if (token.IsCancellationRequested)
            {
                BreakSimulationStep(SimulateStepResult.CancellationRequested);
                return;
            }

            // 血量限制检测
            if (SimulationType == SimulationType.LifePointLimit || SimulationType == SimulationType.ExpectationTongbao_Limited)
            {
                int lifePointAfterExchange = PlayerData.GetResValue(ResType.LifePoint) - PlayerData.NextExchangeCostLifePoint;
                if (lifePointAfterExchange < MinimumLifePoint)
                {
                    BreakSimulationStep(SimulateStepResult.LifePointLimitReached);
                    return;
                }
            }

            // 获得全部期望通宝检测
            if (ExpectedTongbaoIds.Count > 0)
            {
                bool expectationAchieved = true;
                foreach (var tongbaoId in ExpectedTongbaoIds)
                {
                    if (!PlayerData.IsTongbaoExist(tongbaoId))
                    {
                        expectationAchieved = false;
                        break;
                    }
                }
                if (expectationAchieved)
                {
                    BreakSimulationStep(SimulateStepResult.ExpectationAchieved);
                    return;
                }
            }

            Tongbao tongbao = PlayerData.GetTongbao(ExchangeSlotIndex);
            if (ExchangeableSlots.Count > 0)
            {
                while (true)
                {
                    if (mExchangeableSlotsPosIndex >= ExchangeableSlots.Count)
                    {
                        if (!mPriorityPhaseFinished)
                        {
                            mExchangeableSlotsPosIndex = 0;
                            mPriorityPhaseFinished = true;
                        }
                        else
                        {
                            BreakSimulationStep(SimulateStepResult.TargetFilledExchangeableSlots);
                            return;
                        }
                    }

                    ExchangeSlotIndex = ExchangeableSlots[mExchangeableSlotsPosIndex];
                    tongbao = PlayerData.GetTongbao(ExchangeSlotIndex);

                    if (tongbao == null)
                    {
                        // 当前槽位为空，切到下个槽位
                        mExchangeableSlotsPosIndex++;
                        continue;
                    }
                    else if (!tongbao.CanExchange())
                    {
                        // 当前通宝不可交换，切到下个槽位
                        mExchangeableSlotsPosIndex++;
                        continue;
                    }
                    else if (ExpectedTongbaoIds.Contains(tongbao.Id))
                    {
                        // 当前通宝是期望通宝，切到下个槽位
                        mExchangeableSlotsPosIndex++;
                        continue;
                    }
                    else if (!mPriorityPhaseFinished)
                    {
                        // 优先通宝阶段，若当前槽位不是优先通宝或是不可交换通宝，切到下个槽位
                        if (!PriorityTongbaoIds.Contains(tongbao.Id) || UnexchangeableTongbaoIds.Contains(tongbao.Id))
                        {
                            mExchangeableSlotsPosIndex++;
                            continue;
                        }
                    }
                    else
                    {
                        // 普通交换阶段，若当前槽位是不可交换通宝，切到下个槽位
                        if (UnexchangeableTongbaoIds.Contains(tongbao.Id))
                        {
                            mExchangeableSlotsPosIndex++;
                            continue;
                        }
                    }

                    break; // 找到合法槽位
                }
            }

            bool force = SimulationType == SimulationType.ExpectationTongbao;
            DataCollector?.OnExchangeStepBegin(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, ExchangeSlotIndex, PlayerData));
            bool isSuccess = PlayerData.ExchangeTongbao(ExchangeSlotIndex, force);
            ExchangeStepResult result;
            if (isSuccess)
            {
                result = ExchangeStepResult.Success;
            }
            else
            {
                if (tongbao == null)
                {
                    result = ExchangeStepResult.SelectedEmpty;
                }
                else if (!tongbao.CanExchange())
                {
                    result = ExchangeStepResult.TongbaoUnexchangeable;
                }
                else if (!force && !PlayerData.HasEnoughExchangeLife)
                {
                    result = ExchangeStepResult.LifePointNotEnough;
                }
                else
                {
                    result = ExchangeStepResult.ExchangeableTongbaoNotExist;
                }
            }
            DataCollector?.OnExchangeStepEnd(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, ExchangeSlotIndex, PlayerData), result);
            if (result != ExchangeStepResult.Success)
            {
                BreakSimulationStep(SimulateStepResult.ExchangeFailed);
            }
        }

        private void BreakSimulationStep(SimulateStepResult reason)
        {
            if (!mIsSimulating)
            {
                return;
            }
            if (reason != SimulateStepResult.Success)
            {
                mSimulateStepResult = reason;
            }
        }
    }
}
