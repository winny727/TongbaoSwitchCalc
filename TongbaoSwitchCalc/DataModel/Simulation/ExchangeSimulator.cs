using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class ExchangeSimulator
    {
        public PlayerData PlayerData { get; private set; }
        public IDataCollector<SimulateContext> DataCollector { get; private set; }

        public SimulationType SimulationType { get; set; } = SimulationType.LifePointLimit;
        public List<int> ExchangeableSlots { get; private set; } = new List<int>(); // 可交换槽位
        public HashSet<int> TargetTongbaoIds { get; private set; } = new HashSet<int>(); // 目标/降级通宝ID集合
        public int ExpectedTongbaoId { get; set; } = -1; // 期望通宝ID
        public int MinimumLifePoint { get; set; } = 1; // 最小生命值限制
        public int TotalSimulationCount { get; set; } = 1000;

        private int mSimulationStepIndex = 0;
        public int SimulationStepIndex => mSimulationStepIndex;
        public int ExchangeStepIndex { get; private set; } = 0; // 包括交换失败
        public int NextExchangeSlotIndex { get; set; } = -1;

        private IProgress<int> mAsyncProgress;
        private CancellationTokenSource mCancellationTokenSource;
        public bool IsAsyncSimulating => mCancellationTokenSource != null;

        private SimulateStepResult mSimulateStepResult = SimulateStepResult.Success;
        private bool mIsSimulating = false;
        private int mExchangeableSlotsListIndex = 0;
        private int mOriginNextExchangeSlotIndex;
        private readonly PlayerData mRevertPlayerData;

        public const int EXCHANGE_STEP_LIMIT = 10000; // 单轮循环的交换上限，防止死循环

        public bool UseMultiThreadOptimize => DataCollector == null || DataCollector is IThreadSafeDataCollector<SimulateContext>;
        public int OptimizeThreshold { get; set; } = 100000; // 触发多线程的阈值（预计剩余交换次数）
        public int MaxParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 4); // 最大线程数，线程太多竞态很严重
        private bool mUseParallel;

        private void Log(string msg) => Helper.Log(msg);

        public ExchangeSimulator(PlayerData playerData, IDataCollector<SimulateContext> collector = null)
        {
            PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            DataCollector = collector;

            mRevertPlayerData = new PlayerData(playerData.TongbaoSelector, playerData.Random);
        }

        public void SetDataCollector(IDataCollector<SimulateContext> collector)
        {
            DataCollector = collector;
        }

        public void RevertPlayerData()
        {
            PlayerData.CopyFrom(mRevertPlayerData);
            NextExchangeSlotIndex = mOriginNextExchangeSlotIndex;
        }

        public void CachePlayerData()
        {
            mRevertPlayerData.CopyFrom(PlayerData);
            mOriginNextExchangeSlotIndex = NextExchangeSlotIndex;
        }

        public async Task SimulateAsync(IProgress<int> progress = null)
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mAsyncProgress = progress;

            try
            {
                await Task.Run(() => SimulateInternal(mCancellationTokenSource.Token));
            }
            finally
            {
                mCancellationTokenSource.Dispose();
                mCancellationTokenSource = null;
                mAsyncProgress = null;
            }
        }

        public void CancelSimulate()
        {
            mCancellationTokenSource?.Cancel();
        }

        public void Simulate()
        {
            SimulateInternal(CancellationToken.None);
        }

        public void SimulateInternal(CancellationToken token)
        {
            mSimulationStepIndex = 0;
            ExchangeStepIndex = 0;
            mExchangeableSlotsListIndex = 0;
            mSimulateStepResult = SimulateStepResult.Success;
            mUseParallel = false;
            CachePlayerData();
            bool disableOptimization = !UseMultiThreadOptimize;
            using (CodeTimer ct = CodeTimer.StartNew("Simulate"))
            {
                mIsSimulating = true;
                DataCollector?.OnSimulateBegin(SimulationType, TotalSimulationCount, PlayerData);
                while (SimulationStepIndex < TotalSimulationCount)
                {
                    SimulateStep(token);
                    Interlocked.Increment(ref mSimulationStepIndex);
                    mAsyncProgress?.Report(SimulationStepIndex);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (disableOptimization)
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
                    try
                    {
                        SimulateParallel(SimulationStepIndex, token);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                }

                mIsSimulating = false;
                DataCollector?.OnSimulateEnd(SimulationStepIndex, (float)ct.ElapsedMilliseconds, PlayerData);
            }
        }

        private void SimulateParallel(int startIndex, CancellationToken token)
        {
            int remain = TotalSimulationCount - startIndex;
            if (remain <= 0)
            {
                return;
            }

            int workerCount = Math.Min(MaxParallelism, remain);

            // 每个 worker 负责的模拟数量（向上取整）
            int batchSize = (remain + workerCount - 1) / workerCount;

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

                    PlayerData localPlayerData = new PlayerData(PlayerData.TongbaoSelector, PlayerData.Random);
                    localPlayerData.CopyFrom(mRevertPlayerData);

                    var localSimulator = new ExchangeSimulator(localPlayerData, (IThreadSafeDataCollector<SimulateContext>)DataCollector)
                    {
                        SimulationType = SimulationType,
                        NextExchangeSlotIndex = mOriginNextExchangeSlotIndex,
                        ExchangeableSlots = new List<int>(ExchangeableSlots),
                        TargetTongbaoIds = new HashSet<int>(TargetTongbaoIds),
                        ExpectedTongbaoId = ExpectedTongbaoId,
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
                        mAsyncProgress?.Report(mSimulationStepIndex);
                    }
                    localSimulator.mIsSimulating = false;
                }
            );
        }


        private void SimulateStep(CancellationToken token)
        {
            RevertPlayerData();
            ExchangeStepIndex = 0;
            mExchangeableSlotsListIndex = 0;
            if (ExchangeableSlots.Count > 0)
            {
                //Log($"初始可交换槽位(#{mExchangeableSlotsListIndex}): {NextExchangeSlotIndex}");
                NextExchangeSlotIndex = ExchangeableSlots[0];
            }
            mSimulateStepResult = SimulateStepResult.Success;
            DataCollector?.OnSimulateStepBegin(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, NextExchangeSlotIndex, PlayerData));
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
                ExchangeStepIndex--; //修正为最后一次的Index
            }
            else
            {
                ExchangeStepIndex -= 2; //修正为最后一次交换成功的轮次的Index
            }
            DataCollector?.OnSimulateStepEnd(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, NextExchangeSlotIndex, PlayerData), mSimulateStepResult);
        }

        private void ExchangeStep(CancellationToken token)
        {
            /*
            模拟停止规则：
            高级交换：根据目标生命停止模拟；
            期望通宝-不限次数：不限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            期望通宝-限制血量：限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            通用规则：按顺序交换指定槽位内的通宝，直到交换到目标/降级通宝，切换到下一个槽位；若指定槽位被目标/降级通宝填满，则也停止模拟；
            */

            //TODO 槽位已有目标通宝还是会换的bug
            //TODO 第一轮循环，把优先全换掉；第二轮循环，把非不可交换全换掉
            //TODO 多线程优化

            if (token.IsCancellationRequested)
            {
                BreakSimulationStep(SimulateStepResult.CancellationRequested);
                return;
            }

            if (SimulationType == SimulationType.LifePointLimit || SimulationType == SimulationType.ExpectationTongbao_Limited)
            {
                int lifePointAfterExchange = PlayerData.GetResValue(ResType.LifePoint) - PlayerData.NextExchangeCostLifePoint;
                if (lifePointAfterExchange < MinimumLifePoint)
                {
                    BreakSimulationStep(SimulateStepResult.LifePointLimitReached);
                    return;
                }
            }

            //if (SimulationType == SimulationType.ExpectationTongbao || SimulationType == SimulationType.ExpectationTongbao_Limited)
            //{
                if (ExpectedTongbaoId > 0 && PlayerData.IsTongbaoExist(ExpectedTongbaoId))
                {
                    BreakSimulationStep(SimulateStepResult.ExpectationAchieved);
                    return;
                }
            //}

            Tongbao tongbao;
            while (true)
            {
                tongbao = PlayerData.GetTongbao(NextExchangeSlotIndex);
                if (tongbao == null || !TargetTongbaoIds.Contains(tongbao.Id))
                {
                    //Log($"可交换槽位(#{mExchangeableSlotsListIndex}): {NextExchangeSlotIndex}获得目标通宝{tongbao.Name}");
                    break;
                }

                mExchangeableSlotsListIndex++;
                if (mExchangeableSlotsListIndex >= ExchangeableSlots.Count)
                {
                    //Log($"可交换槽位切换(#{mExchangeableSlotsListIndex}): {NextExchangeSlotIndex}");
                    BreakSimulationStep(SimulateStepResult.TargetFilledExchangeableSlots);
                    return;
                }

                NextExchangeSlotIndex = ExchangeableSlots[mExchangeableSlotsListIndex];
            }

            bool force = SimulationType == SimulationType.ExpectationTongbao;
            DataCollector?.OnExchangeStepBegin(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, NextExchangeSlotIndex, PlayerData));
            bool isSuccess = PlayerData.ExchangeTongbao(NextExchangeSlotIndex, force);
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
            DataCollector?.OnExchangeStepEnd(new SimulateContext(SimulationStepIndex, ExchangeStepIndex, NextExchangeSlotIndex, PlayerData), result);
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
