using System;
using System.Collections.Generic;

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

        public SwitchSimulator(PlayerData playerData, IDataCollector<SimulateContext> dataCollector = null)
        {
            PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            DataCollector = dataCollector;

            mRevertPlayerData = new PlayerData(playerData.Random);
        }

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
            mRevertPlayerData.CopyFrom(PlayerData); // CachePlayerData
            using (CodeTimer ct = CodeTimer.StartNew("Simulate"))
            {
                mIsSimulating = true;
                DataCollector?.OnSimulateBegin(type, TotalSimulationCount, PlayerData);
                while (SimulationStepIndex < TotalSimulationCount)
                {
                    SimulateStep();
                    SimulationStepIndex++;
                }
                mIsSimulating = false;
                DataCollector?.OnSimulateEnd(SimulationStepIndex, (float)ct.ElapsedMilliseconds, PlayerData);
            }
        }

        private void SimulateStep()
        {
            RevertPlayerData();
            SwitchStepIndex = 0;
            mSlotIndexPriorityIndex = 0;
            if (SlotIndexPriority.Count > 0)
            {
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
                mSlotIndexPriorityIndex++;
                if (mSlotIndexPriorityIndex < SlotIndexPriority.Count)
                {
                    NextSwitchSlotIndex = SlotIndexPriority[mSlotIndexPriorityIndex];
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
                    result = SwitchStepResult.UnknownError;
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
