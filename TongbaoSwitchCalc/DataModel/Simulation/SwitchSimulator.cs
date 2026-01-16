using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public class SwitchSimulator
    {
        public SimulationType SimulationType { get; private set; } = SimulationType.LifePointLimit;
        public PlayerData PlayerData { get; private set; }
        public IDataCollector<SimulateContext> DataCollector { get; private set; }
        public List<int> SlotIndexPriority { get; private set; } = new List<int>(); // 槽位优先级
        public HashSet<int> TargetTongbaoIds { get; private set; } = new HashSet<int>(); // 目标/降级通宝ID集合
        public int ExpectedTongbaoId { get; set; } = -1; // 期望通宝ID
        public int SimulationStepIndex { get; private set; } = 0;
        public int SwitchStepIndex { get; private set; } = 0; // 包括交换失败
        public int MinimumLifePoint { get; set; } = 1; // 最小生命值限制
        public int TotalSimulationCount { get; set; } = 1000;
        public int NextSwitchSlotIndex { get; set; } = -1;

        private SimulateStepResult mSimulateStepResult = SimulateStepResult.Success;
        private bool mIsSimulating = false;
        private int mSlotIndexPriorityIndex = 0;
        private readonly PlayerData mRevertPlayerData;

        private const int SWITCH_STEP_LIMIT = 10000; // 交换上限，防止死循环

        public int ParallelThreshold { get; set; } = 100000; // 触发多线程的阈值（交换次数*剩余模拟次数）
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
        private bool mUseParallel = false;

        public SwitchSimulator(PlayerData playerData, IDataCollector<SimulateContext> collector = null)
        {
            PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            DataCollector = collector;

            mRevertPlayerData = new PlayerData(playerData.TongbaoSelector, playerData.Random);
        }

        private void Log(string msg) => System.Diagnostics.Debug.WriteLine(msg);

        public void RevertPlayerData()
        {
            PlayerData.CopyFrom(mRevertPlayerData);
        }

        public void Simulate(SimulationType type)
        {
            SimulationType = type;
            SimulationStepIndex = 0;
            SwitchStepIndex = 0;
            mSlotIndexPriorityIndex = 0;
            mSimulateStepResult = SimulateStepResult.Success;
            mUseParallel = false;
            mRevertPlayerData.CopyFrom(PlayerData); // CachePlayerData
            using (CodeTimer ct = CodeTimer.StartNew("Simulate"))
            {
                mIsSimulating = true;
                DataCollector?.OnSimulateBegin(type, TotalSimulationCount, PlayerData);
                while (SimulationStepIndex < TotalSimulationCount)
                {
                    SimulateStep();
                    SimulationStepIndex++;

                    //int estimatedLeftSwitchCount = SwitchStepIndex * (TotalSimulationCount - SimulationStepIndex);
                    //if (!mUseParallel && estimatedLeftSwitchCount >= ParallelThreshold)
                    //{
                    //    Log($"预计剩余交换次数过多({estimatedLeftSwitchCount})，触发多线程优化");
                    //    mUseParallel = true;
                    //    break;
                    //}
                }

                //if (mUseParallel && SimulationStepIndex < TotalSimulationCount)
                //{
                //    SimulateParallel(SimulationStepIndex);
                //}

                mIsSimulating = false;
                DataCollector?.OnSimulateEnd(SimulationStepIndex, (float)ct.ElapsedMilliseconds, PlayerData);
            }
        }

        private void SimulateParallel(int startIndex)
        {
            int remain = TotalSimulationCount - startIndex;
            var collector = new ThreadSafeDataCollector(DataCollector);

            Parallel.For(
                0,
                remain,
                new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism },
                i =>
                {
                    int simIndex = startIndex + i;

                    // 每个线程独立数据
                    PlayerData localPlayerData = new PlayerData(PlayerData.TongbaoSelector, PlayerData.Random);
                    localPlayerData.CopyFrom(mRevertPlayerData); //TODO ThreadSafe
                    //TODO DataCollector优化？根据顺序排序？
                    //TODO 数据处理问题修复，数据不对

                    var localSimulator = new SwitchSimulator(localPlayerData, collector)
                    {
                        SimulationType = SimulationType,
                        SlotIndexPriority = new List<int>(SlotIndexPriority),
                        TargetTongbaoIds = new HashSet<int>(TargetTongbaoIds),
                        ExpectedTongbaoId = ExpectedTongbaoId,
                        MinimumLifePoint = MinimumLifePoint,
                        TotalSimulationCount = 1,
                        SimulationStepIndex = simIndex
                    };
                    localSimulator.SimulateStep();
                }
            );

            SimulationStepIndex += remain;
        }


        private void SimulateStep()
        {
            RevertPlayerData();
            SwitchStepIndex = 0;
            mSlotIndexPriorityIndex = 0;
            if (SlotIndexPriority.Count > 0)
            {
                //Log($"优先槽位(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}");
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

                SwitchStep();
                SwitchStepIndex++;
            }
            if (SwitchStepIndex >= SWITCH_STEP_LIMIT && mSimulateStepResult == SimulateStepResult.Success)
            {
                mSimulateStepResult = SimulateStepResult.SwitchStepLimitReached;
            }
            DataCollector?.OnSimulateStepEnd(new SimulateContext(SimulationStepIndex, SwitchStepIndex, NextSwitchSlotIndex, PlayerData), mSimulateStepResult);
        }

        private void SwitchStep()
        {
            /*
            模拟停止规则：
            高级交换：根据目标生命停止模拟；
            期望通宝：不限制血量，根据是否交换出期望通宝停止模拟（除非次数超过上限）
            通用规则：按顺序交换指定槽位内的通宝，直到交换到目标/降级通宝，切换到下一个槽位；若指定槽位被目标/降级通宝填满，则也停止模拟；
            */

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
                //Log($"优先槽位(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}获得目标通宝{tongbao.Name}");
                mSlotIndexPriorityIndex++;
                if (mSlotIndexPriorityIndex < SlotIndexPriority.Count)
                {
                    NextSwitchSlotIndex = SlotIndexPriority[mSlotIndexPriorityIndex];
                    //Log($"优先槽位(#{mSlotIndexPriorityIndex}): {NextSwitchSlotIndex}");
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
