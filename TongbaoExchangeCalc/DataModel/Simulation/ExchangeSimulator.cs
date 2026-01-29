using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class ExchangeSimulator
    {
        public PlayerData PlayerData { get; protected set; }
        public ISimulationTimer Timer { get; protected set; }
        public IDataCollector<SimulateContext> DataCollector { get; protected set; }

        public SimulationType SimulationType { get; set; } = SimulationType.LifePointLimit;
        public int MinimumLifePoint { get; set; } = 1; // 最小生命值限制
        public int TotalSimulationCount { get; set; } = 1000;

        // 自定义交换策略
        public List<int> ExchangeableSlots { get; protected set; } = new List<int>(); // 可交换槽位
        public HashSet<int> UnexchangeableTongbaoIds { get; protected set; } = new HashSet<int>(); // 不可交换通宝ID集合
        public HashSet<int> PriorityTongbaoIds { get; protected set; } = new HashSet<int>(); // 优先通宝ID集合
        public HashSet<int> ExpectedTongbaoIds { get; protected set; } = new HashSet<int>(); // 期望通宝ID集合

        protected int mSimulationStepIndex = 0;
        public int SimulationStepIndex => mSimulationStepIndex;
        public int ExchangeStepIndex { get; protected set; } = 0; // 包括交换失败
        public int ExchangeSlotIndex { get; set; } = -1;

        protected SimulateStepResult mSimulateStepResult = SimulateStepResult.Success;
        protected bool mIsSimulating = false;
        protected int mExchangeableSlotsPosIndex = 0;
        protected bool mPriorityPhaseFinished = false;
        protected int mInitialExchangeSlotIndex;
        protected readonly PlayerData mRevertPlayerData;

        public const int EXCHANGE_STEP_LIMIT = 10000; // 单轮循环的交换上限，防止死循环

        //protected void Log(string msg) => System.Diagnostics.Debug.WriteLine(msg);

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

        public virtual void RevertPlayerData()
        {
            PlayerData.CopyFrom(mRevertPlayerData, true);
            ExchangeSlotIndex = mInitialExchangeSlotIndex;
        }

        public virtual void CachePlayerData()
        {
            mRevertPlayerData.CopyFrom(PlayerData);
            mInitialExchangeSlotIndex = ExchangeSlotIndex;
        }

        public virtual void Simulate()
        {
            mSimulationStepIndex = 0;
            ExchangeStepIndex = 0;
            mExchangeableSlotsPosIndex = 0;
            mPriorityPhaseFinished = false;
            mSimulateStepResult = SimulateStepResult.Success;
            CachePlayerData();
            mIsSimulating = true;
            DataCollector?.OnSimulateBegin(SimulationType, TotalSimulationCount, PlayerData);
            Timer.Start();
            while (SimulationStepIndex < TotalSimulationCount)
            {
                SimulateStep();
                mSimulationStepIndex++;
            }

            float time = Timer.Stop(); // ms
            mIsSimulating = false;
            DataCollector?.OnSimulateEnd(SimulationStepIndex, time, PlayerData);

        }

        protected virtual void SimulateStep()
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

                ExchangeStep();
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

        protected virtual void ExchangeStep()
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

        protected void BreakSimulationStep(SimulateStepResult reason)
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
