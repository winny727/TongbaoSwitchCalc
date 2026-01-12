using System;
using System.Collections.Generic;
using System.Text;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Simulation
{
    public class SwitchSimulator
    {
        public PlayerData PlayerData { get; private set; }
        public List<Rule> Rules { get; private set; }
        public int CurrentSimulateCount { get; private set; } = 0;
        public int MaxSimulateCount { get; set; } = 1000;
        public int NextSwitchPosIndex { get; set; } = -1;
        public bool ForceSwitch { get; set; } = false;

        private StringBuilder mOutputResult = new StringBuilder();
        public string OutputResult => mOutputResult.ToString();

        private bool mIsSimulateBreak = false;
        private bool mIsSimulating = false;

        public event Action OnSimulateStepCompleted;

        private const int SIMULATE_LIMIT = 1000000;

        public SwitchSimulator(PlayerData playerData, List<Rule> rules = null)
        {
            PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            Rules = rules ?? new List<Rule>();
        }

        public void SetRules(List<Rule> rules)
        {
            Rules = rules ?? new List<Rule>();
        }

        public void Simulate()
        {
            mIsSimulateBreak = false;
            CurrentSimulateCount = 0;
            mIsSimulating = true;
            using (CodeTimer ct = CodeTimer.StartNew("Simulate"))
            {
                while (CurrentSimulateCount < MaxSimulateCount)
                {
                    if (CurrentSimulateCount >= SIMULATE_LIMIT)
                    {
                        break;
                    }

                    if (mIsSimulateBreak)
                    {
                        break;
                    }

                    SimulateStep();
                    CurrentSimulateCount++;
                }
                mOutputResult.Append("\r\n模拟完成，模拟次数: ")
                             .Append(CurrentSimulateCount)
                             .Append(", 模拟耗时: ")
                             .Append(ct.ElapsedMilliseconds)
                             .Append("ms");
            }
            mIsSimulating = false;
        }

        public void SimulateStep()
        {
            PlayerData.SwitchTongbao(NextSwitchPosIndex, ForceSwitch);
            if (mOutputResult.Length > 0)
            {
                mOutputResult.Append("\r\n");
            }
            mOutputResult.Append('(')
                         .Append(PlayerData.SwitchCount)
                         .Append(") ")
                         .Append(PlayerData.LastSwitchResult);
            if (!ForceSwitch && !PlayerData.HasEnoughSwitchLife())
            {
                BreakSimulation();
            }
            //foreach (var rule in Rules)
            //{
            //    if (rule.Enabled)
            //    {
            //        rule.ExecuteRule(PlayerData);
            //    }
            //}
            OnSimulateStepCompleted?.Invoke();
        }

        public void BreakSimulation()
        {
            if (!mIsSimulating)
            {
                return;
            }
            mIsSimulateBreak = true;
            mOutputResult.Append("\r\n模拟中止");
        }

        public void ClearOutputResult()
        {
            mOutputResult.Clear();
        }
    }
}
