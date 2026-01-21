using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TongbaoSwitchCalc.Impl.Simulation;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public class SwitchSimulator
    {
        public PlayerData PlayerData { get; private set; }
        public IDataCollector<SimulateContext> DataCollector { get; private set; }

        public SimulationType SimulationType { get; set; } = SimulationType.LifePointLimit;
        public List<int> SlotIndexPriority { get; private set; } = new List<int>(); // 槽位优先级
        public HashSet<int> TargetTongbaoIds { get; private set; } = new HashSet<int>(); // 目标/降级通宝ID集合
        public int ExpectedTongbaoId { get; set; } = -1; // 期望通宝ID
        public int MinimumLifePoint { get; set; } = 1; // 最小生命值限制
        public int TotalSimulationCount { get; set; } = 1000;

        private int mSimulationStepIndex = 0;
        public int SimulationStepIndex => mSimulationStepIndex;
        public int SwitchStepIndex { get; private set; } = 0; // 包括交换失败
        public int NextSwitchSlotIndex { get; set; } = -1;

        private IProgress<int> mAsyncProgress;
        private CancellationTokenSource mCancellationTokenSource;
        public bool IsAsyncSimulating => mCancellationTokenSource != null;

        private SimulateStepResult mSimulateStepResult = SimulateStepResult.Success;
        private bool mIsSimulating = false;
        private int mSlotIndexPriorityIndex = 0;
        private int mOriginNextSwitchSlotIndex;
        private readonly PlayerData mRevertPlayerData;

        public const int SWITCH_STEP_LIMIT = 10000; // 单轮循环的交换上限，防止死循环

        public bool UseMultiThreadOptimize => DataCollector == null || DataCollector is IThreadSafeDataCollector<SimulateContext>;
        public int OptimizeThreshold { get; set; } = 100000; // 触发多线程的阈值（预计剩余交换次数）
        public int MaxParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 4); // 最大线程数，线程太多竞态很严重
        private bool mUseParallel;

        public SwitchSimulator(PlayerData playerData, IDataCollector<SimulateContext> collector = null)
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
            NextSwitchSlotIndex = mOriginNextSwitchSlotIndex;
        }

        public void CachePlayerData()
        {
            mRevertPlayerData.CopyFrom(PlayerData);
            mOriginNextSwitchSlotIndex = NextSwitchSlotIndex;
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
            SwitchStepIndex = 0;
            mSlotIndexPriorityIndex = 0;
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

                    int estimatedLeftSwitchStep = SwitchStepIndex * (TotalSimulationCount - SimulationStepIndex);
                    if (!mUseParallel && estimatedLeftSwitchStep >= OptimizeThreshold)
                    {
                        mUseParallel = true;
                        DataCollector?.OnSimulateParallel(estimatedLeftSwitchStep, SimulationStepIndex);
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

                    //System.Diagnostics.Debug.WriteLine($"{batchStart}->{batchEnd}");

                    PlayerData localPlayerData = new PlayerData(PlayerData.TongbaoSelector, PlayerData.Random);
                    localPlayerData.CopyFrom(mRevertPlayerData);

                    // 考虑到线程安全，Logger就不往下放了，反正也没啥要打印的
                    var localSimulator = new SwitchSimulator(localPlayerData, (IThreadSafeDataCollector<SimulateContext>)DataCollector)
                    {
                        SimulationType = SimulationType,
                        NextSwitchSlotIndex = mOriginNextSwitchSlotIndex,
                        SlotIndexPriority = new List<int>(SlotIndexPriority),
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
            SwitchStepIndex = 0;
            mSlotIndexPriorityIndex = 0;
            if (SlotIndexPriority.Count > 0)
            {
                //Logger?.Log($"初始优先槽位(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}");
                NextSwitchSlotIndex = SlotIndexPriority[0];
            }
            mSimulateStepResult = SimulateStepResult.Success;
            DataCollector?.OnSimulateStepBegin(new SimulateContext(SimulationStepIndex, SwitchStepIndex, NextSwitchSlotIndex, PlayerData));
            while (SwitchStepIndex < SWITCH_STEP_LIMIT)
            {
                if (mSimulateStepResult != SimulateStepResult.Success)
                {
                    break;
                }

                SwitchStep(token);
                SwitchStepIndex++;
            }
            if (SwitchStepIndex >= SWITCH_STEP_LIMIT && mSimulateStepResult == SimulateStepResult.Success)
            {
                mSimulateStepResult = SimulateStepResult.SwitchStepLimitReached;
            }
            SwitchStepIndex--; //修正为最后一次的Index
            DataCollector?.OnSimulateStepEnd(new SimulateContext(SimulationStepIndex, SwitchStepIndex, NextSwitchSlotIndex, PlayerData), mSimulateStepResult);
        }

        private void SwitchStep(CancellationToken token)
        {
            /*
            模拟停止规则：
            高级交换：根据目标生命停止模拟；
            期望通宝：不限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            通用规则：按顺序交换指定槽位内的通宝，直到交换到目标/降级通宝，切换到下一个槽位；若指定槽位被目标/降级通宝填满，则也停止模拟；
            */

            if (token.IsCancellationRequested)
            {
                BreakSimulationStep(SimulateStepResult.CancellationRequested);
                return;
            }

            if (SimulationType == SimulationType.LifePointLimit)
            {
                int lifePointAfterSwitch = PlayerData.GetResValue(ResType.LifePoint) - PlayerData.NextSwitchCostLifePoint;
                if (lifePointAfterSwitch < MinimumLifePoint)
                {
                    BreakSimulationStep(SimulateStepResult.LifePointLimitReached);
                    return;
                }
            }

            if (SimulationType == SimulationType.ExpectationTongbao)
            {
                if (ExpectedTongbaoId > 0 && PlayerData.IsTongbaoExist(ExpectedTongbaoId))
                {
                    BreakSimulationStep(SimulateStepResult.ExpectationAchieved);
                    return;
                }
            }

            Tongbao tongbao = PlayerData.GetTongbao(NextSwitchSlotIndex);
            if (tongbao != null && TargetTongbaoIds.Contains(tongbao.Id))
            {
                //Logger?.Log($"优先槽位(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}获得目标通宝{tongbao.Name}");
                mSlotIndexPriorityIndex++;
                if (mSlotIndexPriorityIndex < SlotIndexPriority.Count)
                {
                    NextSwitchSlotIndex = SlotIndexPriority[mSlotIndexPriorityIndex];
                    //Logger?.Log($"优先槽位切换(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}");
                }
                else
                {
                    BreakSimulationStep(SimulateStepResult.TargetTongbaoFilledPrioritySlots);
                    return;
                }
            }

            bool force = SimulationType == SimulationType.ExpectationTongbao;
            DataCollector?.OnSwitchStepBegin(new SimulateContext(SimulationStepIndex, SwitchStepIndex, NextSwitchSlotIndex, PlayerData));
            bool isSuccess = PlayerData.SwitchTongbao(NextSwitchSlotIndex, force);
            SwitchStepResult result;
            if (isSuccess)
            {
                result = SwitchStepResult.Success;
            }
            else
            {
                if (tongbao == null)
                {
                    result = SwitchStepResult.SelectedEmpty;
                }
                else if (!tongbao.CanSwitch())
                {
                    result = SwitchStepResult.TongbaoCanNotSwitch;
                }
                else if (!force && !PlayerData.HasEnoughSwitchLife)
                {
                    result = SwitchStepResult.LifePointNotEnough;
                }
                else
                {
                    result = SwitchStepResult.NoSwitchableTongbao;
                }
            }
            DataCollector?.OnSwitchStepEnd(new SimulateContext(SimulationStepIndex, SwitchStepIndex, NextSwitchSlotIndex, PlayerData), result);
            if (result != SwitchStepResult.Success)
            {
                BreakSimulationStep(SimulateStepResult.SwitchFailed);
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
