using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc.Undo.Commands
{
    public class CommandBase
    {
#if DEBUG
        public static bool DebugMode { get; set; } = true;
#else
        public static bool DebugMode { get; set; } = false;
#endif
        public bool ShowDebugMessage { get; set; } = true;
        protected string mDebugInfo;

        public CommandBase(string debugInfo = null)
        {
            mDebugInfo = debugInfo;
        }

        public void DebugMessage(string msg)
        {
            if (DebugMode && ShowDebugMessage)
            {
                Helper.Log($"[Undo] {msg}");
            }
        }
    }

    /// <summary>
    /// 变量设置命令，适用于单纯的值类型或string类型的变量设置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SetValueCommand<T> : CommandBase, IUndoCommand
    {
        private readonly Func<T, bool> mSetter;

        protected T mBeforeValue;
        protected T mAfterValue;

        public SetValueCommand(Func<T, bool> setter, T beforeValue, T afterValue,
             string debugInfo = null) : base(debugInfo)
        {
            mSetter = setter ?? throw new ArgumentNullException(nameof(setter));
            mBeforeValue = beforeValue;
            mAfterValue = afterValue;
        }

        public SetValueCommand(Action<T> setter, T beforeValue, T afterValue,
             string debugInfo = null) : base(debugInfo)
        {
            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            var type = typeof(T);
            if (!type.IsValueType && type != typeof(string))
            {
                throw new NotSupportedException($"SetValueCommand<{type.Name}> only support value-type or string.");
            }

            mSetter = (value) =>
            {
                setter(value);
                return true;
            };
            mBeforeValue = beforeValue;
            mAfterValue = afterValue;
        }

        public bool Execute()
        {
            if (mBeforeValue.Equals(mAfterValue))
            {
                return false;
            }

            bool result = mSetter(mAfterValue);
            if (result)
            {
                DebugMessage($"SetValueCommand Execute {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
            }
            return result;
        }

        public void Undo()
        {
            mSetter(mBeforeValue);
            DebugMessage($"SetValueCommand Undo {mDebugInfo}: {mAfterValue}->{mBeforeValue}");
        }

        public void Redo()
        {
            mSetter(mAfterValue);
            DebugMessage($"SetValueCommand Redo {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
        }
    }

    public abstract class ScopeCommandBase : CommandBase
    {
        protected static bool mScopeCommandExist;

        public ScopeCommandBase(string debugInfo = null) : base(debugInfo) { }
    }

    /// <summary>
    /// 作用域值变化检测命令，自动将using范围内的变量值变化纳入Undo管理，适用于单纯的值类型或string类型的变量设置，禁止嵌套使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ScopeOnSetValueCommand<T> : ScopeCommandBase, IUndoCommand, IDisposable
    {
        private readonly bool mIsValid;

        private readonly Func<T> mGetter;
        private readonly Action<T> mSetter;

        protected T mBeforeValue;
        protected T mAfterValue;

        public ScopeOnSetValueCommand(Func<T> getter, Action<T> setter,
             string debugInfo = null) : base(debugInfo)
        {
            var type = typeof(T);
            if (!type.IsValueType && type != typeof(string))
            {
                throw new NotSupportedException($"ScopeSetValueCommand<{type.Name}> only support value-type or string.");
            }

            mIsValid = !mScopeCommandExist; // 禁止嵌套
            mScopeCommandExist = true;

            mGetter = getter ?? throw new ArgumentNullException(nameof(getter));
            mSetter = setter ?? throw new ArgumentNullException(nameof(setter));
            mBeforeValue = mGetter();
        }

        public bool Execute()
        {
            // 值已经在外部被设置了，这个命令的作用只是为了Append Undo/Redo 到UndoCommandMgr，并不实际执行设置操作
            bool result = !mBeforeValue.Equals(mAfterValue);
            if (result)
            {
                DebugMessage($"ScopeOnSetValueCommand Append {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
            }
            return result;
        }

        public void Undo()
        {
            mSetter(mBeforeValue);
            DebugMessage($"ScopeOnSetValueCommand Undo {mDebugInfo}: {mAfterValue}->{mBeforeValue}");
        }

        public void Redo()
        {
            mSetter(mAfterValue);
            DebugMessage($"ScopeOnSetValueCommand Redo {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
        }

        public void Dispose()
        {
            if (!mIsValid)
            {
                Helper.Log("Invalid ScopeSetValueCommand Disposed.");
                return;
            }

            mScopeCommandExist = false;
            mAfterValue = mGetter();
            UndoCommandMgr.Instance.ExecuteCommand(this);
        }
    }

    /// <summary>
    /// 控件设值后命令
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OnControlSetValueCommand<T> : CommandBase, IUndoCommand
    {
        private readonly Func<T, bool> mSetter;

        protected T mBeforeValue;
        protected T mAfterValue;

        public OnControlSetValueCommand(Func<T, bool> setter, T beforeValue, T afterValue,
             string debugInfo = null) : base(debugInfo)
        {
            var type = typeof(T);
            if (!type.IsValueType && type != typeof(string))
            {
                throw new NotSupportedException($"ScopeSetValueCommand<{type.Name}> only support value-type or string.");
            }

            mSetter = setter ?? throw new ArgumentNullException(nameof(setter));
            mBeforeValue = beforeValue;
            mAfterValue = afterValue;
        }

        public OnControlSetValueCommand(Action<T> setter, T beforeValue, T afterValue,
             string debugInfo = null) : base(debugInfo)
        {
            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            mSetter = (value) =>
            {
                setter(value);
                return true;
            };
            mBeforeValue = beforeValue;
            mAfterValue = afterValue;
        }

        public bool Execute()
        {
            // 值已经在外部被设置了，这个命令的作用只是为了Append Undo/Redo 到UndoCommandMgr，并不实际执行设置操作
            bool result = mSetter != null && !mBeforeValue.Equals(mAfterValue);
            if (result)
            {
                DebugMessage($"OnControlSetValueCommand Append {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
            }
            return result;
        }

        public void Undo()
        {
            mSetter(mBeforeValue);
            DebugMessage($"OnControlSetValueCommand Undo {mDebugInfo}: {mAfterValue}->{mBeforeValue}");
        }

        public void Redo()
        {
            mSetter(mAfterValue);
            DebugMessage($"OnControlSetValueCommand Undo {mDebugInfo}: {mBeforeValue}->{mAfterValue}");
        }
    }

    /// <summary>
    /// PlayerData数据变化命令，128个命令还达不到性能瓶颈，暂时不区分不同类型的PlayerData变化命令了，
    /// 直接用一个命令类来管理PlayerData的变化，并通过ClonePlayerData来记录变化前后的数据状态，
    /// Undo/Redo时直接CopyFrom还原数据状态
    /// </summary>
    public class PlayerDataCommand : CommandBase, IUndoCommand
    {
        protected readonly PlayerData mPlayerData;
        private readonly Func<bool> mCallback;

        protected PlayerData mPlayerDataBefore;
        protected PlayerData mPlayerDataAfter;

        public PlayerDataCommand(PlayerData playerData, Func<bool> callback)
        {
            mPlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            mCallback = callback;
        }

        public PlayerDataCommand(PlayerData playerData, Action callback)
        {
            mPlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            mCallback = () =>
            {
                callback?.Invoke();
                return true;
            };
        }

        protected PlayerData ClonePlayerData()
        {
            var playerData = new PlayerData(mPlayerData.TongbaoSelector, mPlayerData.Random);
            playerData.CopyFrom(mPlayerData);
            return playerData;
        }

        public virtual bool Execute()
        {
            mPlayerDataBefore = ClonePlayerData();
            bool result = ExecuteInternal();
            mPlayerDataAfter = ClonePlayerData();
            result &= !mPlayerDataBefore.Equals(mPlayerDataAfter);
            if (result)
            {
                DebugMessage($"PlayerDataCommand Execute");
            }
            return result;
        }

        protected virtual bool ExecuteInternal()
        {
            return mCallback?.Invoke() ?? false;
        }

        public virtual void Undo()
        {
            mPlayerData.CopyFrom(mPlayerDataBefore);
            DebugMessage($"PlayerDataCommand Undo");
        }

        public virtual void Redo()
        {
            mPlayerData.CopyFrom(mPlayerDataAfter);
            DebugMessage($"PlayerDataCommand Redo");
        }
    }

    /// <summary>
    /// 作用域PlayerData命令，自动将using范围内PlayerData的变化纳入Undo管理
    /// </summary>
    public class ScopePlayerDataCommand : PlayerDataCommand, IDisposable
    {
        private static bool mScopeCommandExist;
        private readonly bool mIsValid;

        public ScopePlayerDataCommand(PlayerData playerData)
            : base(playerData, null)
        {
            mIsValid = !mScopeCommandExist; // 禁止嵌套
            mScopeCommandExist = true;
            mPlayerDataBefore = ClonePlayerData();
        }

        public override bool Execute()
        {
            bool result = !mPlayerDataBefore.Equals(mPlayerDataAfter);
            if (result)
            {
                DebugMessage($"PlayerDataCommand Append");
            }
            return result;
        }

        public void Dispose()
        {
            if (!mIsValid)
            {
                Helper.Log("Invalid ScopePlayerDataCommand Disposed.");
                return;
            }

            mScopeCommandExist = false;
            mPlayerDataAfter = ClonePlayerData();
            UndoCommandMgr.Instance.ExecuteCommand(this);
        }
    }

    //public class ExchangeTongbaoCommand : PlayerDataCommand
    //{
    //    public ExchangeTongbaoCommand(PlayerData playerData,
    //        int slotIndex)
    //        : base(playerData, () => playerData.ExchangeTongbao(slotIndex)) { }
    //}

    #region RuleTreeView Commands

    public class AddRuleCommand : CommandBase, IUndoCommand
    {
        private readonly UniqueRuleCollection mCollection;
        private readonly SimulationRule mRule;
        private readonly int mIndex;

        public AddRuleCommand(UniqueRuleCollection collection, SimulationRule rule, int index)
        {
            mCollection = collection;
            mRule = rule;
            mIndex = index;
        }

        public AddRuleCommand(UniqueRuleCollection collection, SimulationRule rule)
        {
            mCollection = collection;
            mRule = rule;
            mIndex = collection.Count;
        }

        public bool Execute()
        {
            bool result = mCollection.Insert(mIndex, mRule);
            if (result)
            {
                DebugMessage($"AddRuleCommand Execute Collection({mCollection.Type}) Insert({mIndex}): {mRule.GetRuleString()}");
            }
            return result;
        }

        public void Undo()
        {
            mCollection.RemoveAt(mIndex);
            DebugMessage($"AddRuleCommand Undo Collection({mCollection.Type}) RemoveAt({mIndex}): {mRule.GetRuleString()}");
        }

        public void Redo()
        {
            mCollection.Insert(mIndex, mRule);
            DebugMessage($"AddRuleCommand Redo Collection({mCollection.Type}) Insert({mIndex}): {mRule.GetRuleString()}");
        }
    }

    public class RemoveRuleCommand : CommandBase, IUndoCommand
    {
        private readonly UniqueRuleCollection mCollection;
        private readonly SimulationRule mRule;
        private readonly int mIndex;

        public RemoveRuleCommand(UniqueRuleCollection collection, SimulationRule rule, int index)
        {
            mCollection = collection;
            mRule = rule;
            mIndex = index;
        }

        public bool Execute()
        {
            mCollection.RemoveAt(mIndex);
            DebugMessage($"RemoveRuleCommand Execute Collection({mCollection.Type}) RemoveAt({mIndex}): {mRule.GetRuleString()}");
            return true;
        }

        public void Undo()
        {
            bool result = mCollection.Insert(mIndex, mRule);
            DebugMessage($"RemoveRuleCommand Undo{(!result ? " Failed " : " ")}Collection({mCollection.Type}) Insert({mIndex}): {mRule.GetRuleString()}");
        }

        public void Redo()
        {
            mCollection.RemoveAt(mIndex);
            DebugMessage($"RemoveRuleCommand Redo Collection({mCollection.Type}) RemoveAt({mIndex}): {mRule.GetRuleString()}");
        }
    }

    public class MoveRuleCommand : CommandBase, IUndoCommand
    {
        private readonly UniqueRuleCollection mCollection;
        private readonly SimulationRule mRule;
        private readonly int mBeforeIndex;
        private readonly int mAfterIndex;

        public MoveRuleCommand(UniqueRuleCollection collection, SimulationRule rule, int beforeIndex, int afterIndex)
        {
            mCollection = collection;
            mRule = rule;
            mBeforeIndex = beforeIndex;
            mAfterIndex = afterIndex;
        }

        public bool Execute()
        {
            if (mBeforeIndex == mAfterIndex)
            {
                return false;
            }

            bool result = mCollection.MoveToIndex(mRule, mAfterIndex);
            if (result)
            {
                DebugMessage($"MoveRuleCommand Execute Collection({mCollection.Type}) MoveToIndex({mBeforeIndex}->{mAfterIndex}): {mRule.GetRuleString()}");
            }
            return result;
        }

        public void Undo()
        {
            bool result = mCollection.MoveToIndex(mRule, mBeforeIndex);
            DebugMessage($"MoveRuleCommand Undo{(!result ? " Failed " : " ")}Collection({mCollection.Type}) MoveToIndex({mAfterIndex}->{mBeforeIndex}): {mRule.GetRuleString()}");
        }

        public void Redo()
        {
            bool result = mCollection.MoveToIndex(mRule, mAfterIndex);
            DebugMessage($"MoveRuleCommand Redo{(!result ? " Failed " : " ")}Collection({mCollection.Type}) MoveToIndex({mBeforeIndex}->{mAfterIndex}): {mRule.GetRuleString()}");
        }
    }

    public class ToggleRuleEnabledCommand : CommandBase, IUndoCommand
    {
        private readonly UniqueRuleCollection mCollection;
        private readonly SimulationRule mRule;
        private readonly bool mBeforeChecked;
        private readonly bool mAfterChecked;

        public ToggleRuleEnabledCommand(UniqueRuleCollection collection, SimulationRule rule, bool beforeChecked, bool afterChecked)
        {
            mCollection = collection;
            mRule = rule;
            mBeforeChecked = beforeChecked;
            mAfterChecked = afterChecked;
        }

        public bool Execute()
        {
            if (mRule.Enabled == mAfterChecked)
            {
                return false;
            }

            mRule.Enabled = mAfterChecked;
            mCollection.SetDirty();
            DebugMessage($"ToggleRuleEnabledCommand Execute SetEnabled({mBeforeChecked}->{mAfterChecked}): {mRule.GetRuleString()}");
            return true;
        }

        public void Undo()
        {
            mRule.Enabled = mBeforeChecked;
            mCollection.SetDirty();
            DebugMessage($"ToggleRuleEnabledCommand Undo SetEnabled({mAfterChecked}->{mBeforeChecked}): {mRule.GetRuleString()}");
        }

        public void Redo()
        {
            mRule.Enabled = mAfterChecked;
            mCollection.SetDirty();
            DebugMessage($"ToggleRuleEnabledCommand Redo SetEnabled({mBeforeChecked}->{mAfterChecked}): {mRule.GetRuleString()}");
        }
    }

    public class ReplaceRuleCommand : CommandBase, IUndoCommand
    {
        private readonly UniqueRuleCollection mCollection;
        private readonly SimulationRule mBeforeRule;
        private readonly SimulationRule mAfterRule;
        private readonly int mIndex;

        public ReplaceRuleCommand(UniqueRuleCollection collection, SimulationRule beforeRule, SimulationRule afterRule, int index)
        {
            mCollection = collection;
            mBeforeRule = beforeRule;
            mAfterRule = afterRule;
            mIndex = index;
        }

        private bool ExecuteInternal()
        {
            if (mBeforeRule == mAfterRule)
            {
                return false;
            }

            if (mBeforeRule.Type == mAfterRule.Type)
            {
                var beforeArgs = SimulationDefine.GetSimulationRuleArgs(mBeforeRule);
                var afterArgs = SimulationDefine.GetSimulationRuleArgs(mAfterRule);
                if (beforeArgs.Length == afterArgs.Length)
                {
                    bool isEquals = true;
                    for (int i = 0; i < beforeArgs.Length; i++)
                    {
                        if (beforeArgs[i] != afterArgs[i])
                        {
                            isEquals = false;
                            break;
                        }
                    }
                    if (isEquals)
                    {
                        return false;
                    }
                }
            }

            mCollection.RemoveAt(mIndex);
            if (!mCollection.Insert(mIndex, mAfterRule))
            {
                mCollection.Insert(mIndex, mBeforeRule); // 还原
                return false;
            }

            return true;
        }

        public bool Execute()
        {
            bool result = ExecuteInternal();
            if (result)
            {
                DebugMessage($"ReplaceRuleCommand Execute Collection({mCollection.Type}) Replace({mIndex}): {mBeforeRule.GetRuleString()} -> {mAfterRule.GetRuleString()}");
            }
            return result;
        }

        public void Undo()
        {
            mCollection.RemoveAt(mIndex);
            bool result = mCollection.Insert(mIndex, mBeforeRule);
            if (!result)
            {
                mCollection.Insert(mIndex, mAfterRule); // 还原
            }

            DebugMessage($"ReplaceRuleCommand Undo{(!result ? " Failed " : " ")}Collection({mCollection.Type}) Replace({mIndex}): {mAfterRule.GetRuleString()} -> {mBeforeRule.GetRuleString()}");
        }

        public void Redo()
        {
            bool result = ExecuteInternal();
            DebugMessage($"ReplaceRuleCommand Redo{(!result ? " Failed " : " ")}Collection({mCollection.Type}) Replace({mIndex}): {mBeforeRule.GetRuleString()} -> {mAfterRule.GetRuleString()}");
        }
    }

    #endregion
}
