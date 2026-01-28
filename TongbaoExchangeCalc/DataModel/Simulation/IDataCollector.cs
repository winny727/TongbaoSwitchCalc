using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public interface IDataCollector<TContext> where TContext : struct
    {
        // 注意以下所有函数不保证SimulateStep之间的触发先后顺序，只保证单个SimulateStep内的ExchangeStep顺序执行
        // 每个DataCollector分别收集不同范围段的SimlateStep的数据
        // 因为会多线程并发执行，需要通过context里的SimulateStepIndex来区分
        // 考虑到性能，玩家数据直接通过PlayerData引用来拿了，这里约定好DataCollector禁止修改PlayerData里的数据
        void OnSimulateBegin(SimulationType type, int totalSimStep, PlayerData playerData);
        void OnSimulateEnd(int executedSimStep, float simCostTimeMS, PlayerData playerData);
        void OnSimulateParallel(int estimatedLeftExchangeStep, int curSimStep); // 切换到并行模拟时调用
        void OnSimulateStepBegin(in TContext context);
        void OnSimulateStepEnd(in TContext context, SimulateStepResult result);
        void OnExchangeStepBegin(in TContext context);
        void OnExchangeStepEnd(in TContext context, ExchangeStepResult result); 
        IDataCollector<TContext> CloneAsEmpty(); // 保留参数克隆但不克隆数据
        void MergeData(IDataCollector<TContext> other); // 把other的数据合并到this
        void ClearData(); // 清理数据
    }

    public interface IShareContainer<TContext> where TContext : struct
    {
        void ShareContainer(IDataCollector<TContext> other); // 把other的容器share到this
    }
}
