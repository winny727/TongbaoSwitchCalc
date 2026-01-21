using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel.Simulation
{
    public interface IDataCollector<TContext> where TContext : struct
    {
        // 注意以下所有函数不保证SimulateStep之间的触发先后顺序，只保证单个SimulateStep内的SwitchStep顺序执行
        // 因为会多线程并发执行，需要通过context里的SimulateStepIndex来区分
        void OnSimulateBegin(SimulationType type, int totalSimStep, in IReadOnlyPlayerData playerData);
        void OnSimulateEnd(int executedSimStep, float simCostTimeMS, in IReadOnlyPlayerData playerData);
        void OnSimulateParallel(int estimatedLeftSwitchStep, int curSimStep); // 切换到并行模拟时调用
        void OnSimulateStepBegin(in TContext context);
        void OnSimulateStepEnd(in TContext context, SimulateStepResult result);
        void OnSwitchStepBegin(in TContext context);
        void OnSwitchStepEnd(in TContext context, SwitchStepResult result); 
        void ClearData();

        //TODO
        //IDataCollector<TContext> CloneAsEmpty();
        //void MergeData(IDataCollector<TContext> other);
    }

    public interface IThreadSafeDataCollector<TContext> : IDataCollector<TContext> where TContext : struct
    {

    }
}
