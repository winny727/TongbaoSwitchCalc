using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.Undo
{
    public class UndoCommandMgr
    {
        private class CompositeCommand : IUndoCommand
        {
            private readonly List<IUndoCommand> mCommands = new List<IUndoCommand>();
            public void AddCommand(IUndoCommand command)
            {
                if (command != null)
                {
                    mCommands.Add(command);
                }
            }
            public bool Execute()
            {
                int executedCount = 0;

                foreach (var command in mCommands)
                {
                    if (!command.Execute())
                    {
                        // 失败自动回滚
                        for (int i = executedCount - 1; i >= 0; i--)
                        {
                            mCommands[i].Undo();
                        }
                        return false;
                    }
                    executedCount++;
                }

                return executedCount > 0;
            }
            public void Redo()
            {
                for (int i = 0; i < mCommands.Count; i++)
                {
                    mCommands[i].Redo();
                }
            }
            public void Undo()
            {
                for (int i = mCommands.Count - 1; i >= 0; i--)
                {
                    mCommands[i].Undo();
                }
            }
        }

        private static UndoCommandMgr mInstance;
        public static UndoCommandMgr Instance
        {
            get
            {
                mInstance ??= new UndoCommandMgr();
                return mInstance;
            }
        }

        private UndoCommandMgr()
        {
            Initialize();
        }

        private readonly LinkedList<IUndoCommand> mUndoCommands = new LinkedList<IUndoCommand>(); // 已执行的命令列表，越靠后越新
        private LinkedListNode<IUndoCommand> mCommandIndexNode; // 若mCommandIndexNode为空表示在-1位置

        private int mMergeDepth = 0; // 支持嵌套
        private LinkedListNode<IUndoCommand> mMergeStartNode;

        public int MaxUndoCommand { get; set; } = 128; // 小于0则不限制撤销命令数量
        public bool CanUndo => mCommandIndexNode != null;
        public bool CanRedo => mCommandIndexNode?.Next != null || (mCommandIndexNode == null && mUndoCommands.First != null);
        public event Action OnCommandChanged;

        private void Initialize()
        {

        }

        public void ClearUndoCommands()
        {
            mUndoCommands.Clear();
            mCommandIndexNode = null;
            OnCommandChanged?.Invoke();
        }

        public bool ExecuteCommand<T>(params object[] args) where T : IUndoCommand
        {
            T command = (T)Activator.CreateInstance(typeof(T), args);
            return ExecuteCommand(command);
        }

        public bool ExecuteCommand(IUndoCommand command)
        {
            if (command == null)
            {
                return false;
            }

            bool result = command.Execute(); // 执行成功才加入命令列表，失败则不加入
            if (!result || MaxUndoCommand == 0)
            {
                return result;
            }

            // 删除重做部分的命令
            if (mCommandIndexNode != null)
            {
                while (mCommandIndexNode.Next != null)
                {
                    mUndoCommands.RemoveLast();
                }
            }
            else
            {
                ClearUndoCommands();
            }

            // 限制 Undo 的最大命令数量
            if (MaxUndoCommand > 0 && mUndoCommands.Count >= MaxUndoCommand)
            {
                mUndoCommands.RemoveFirst();
            }

            mUndoCommands.AddLast(command);
            mCommandIndexNode = mUndoCommands.Last;

            OnCommandChanged?.Invoke();

            return result;
        }

        public void Undo()
        {
            // 确保有可撤销的命令
            if (mCommandIndexNode != null)
            {
                mCommandIndexNode.Value.Undo();
                mCommandIndexNode = mCommandIndexNode.Previous; // 索引位置-1
                OnCommandChanged?.Invoke();
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        public void Redo()
        {
            // 确保有可重做的命令
            if (mCommandIndexNode != null)
            {
                if (mCommandIndexNode.Next != null)
                {
                    mCommandIndexNode = mCommandIndexNode.Next; // 索引位置+1
                    mCommandIndexNode.Value.Redo();
                    OnCommandChanged?.Invoke();
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            else if (mUndoCommands.First != null)
            {
                mCommandIndexNode = mUndoCommands.First; // 从-1位置移动到0位置
                mCommandIndexNode.Value.Redo();
                OnCommandChanged?.Invoke();
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        public int GetCurrentCommandIndex()
        {
            var list = mUndoCommands;
            var node = mCommandIndexNode;

            int index = 0;
            var current = list.First;

            while (current != null)
            {
                if (current == node)
                {
                    return index;
                }
                current = current.Next;
                index++;
            }

            return -1;
        }

        public void BeginMerge()
        {
            mMergeDepth++;

            // 只在最外层 Begin 时记录起点
            if (mMergeDepth == 1)
            {
                mMergeStartNode = mCommandIndexNode;
            }
        }

        public bool EndMerge()
        {
            if (mMergeDepth <= 0)
            {
                return false;
            }

            mMergeDepth--;

            // 只有最外层 End 才真正合并
            if (mMergeDepth > 0)
            {
                return true;
            }

            // 没有新命令
            if (mCommandIndexNode == mMergeStartNode)
            {
                return false;
            }

            // 计算需要合并的命令数量
            int count = 0;
            var node = mCommandIndexNode;

            while (node != null && node != mMergeStartNode)
            {
                count++;
                node = node.Previous;
            }

            // 至少 2 个命令才有意义
            if (count <= 1)
            {
                return false;
            }

            return MergeLast(count);
        }


        public bool MergeLast(int count)
        {
            if (count <= 1)
            {
                return false;
            }

            LinkedListNode<IUndoCommand> end = mCommandIndexNode;
            LinkedListNode<IUndoCommand> start = end;

            if (start == null)
            {
                return false;
            }

            CompositeCommand composite = new CompositeCommand();

            for (int i = 1; i < count; i++)
            {
                if (start.Previous == null)
                {
                    return false; // 不足 count 个
                }
                start = start.Previous;
            }

            // 把区间内的命令加入 composite（按执行顺序）
            LinkedListNode<IUndoCommand> node = start;
            while (true)
            {
                composite.AddCommand(node.Value);
                if (node == end)
                {
                    break;
                }
                node = node.Next;
            }

            // 清掉 redo 区
            while (end.Next != null)
            {
                mUndoCommands.RemoveLast();
            }

            // 记录 start 前一个节点
            LinkedListNode<IUndoCommand> insertPos = start.Previous;

            // 删除原有节点
            node = start;
            while (true)
            {
                var next = node.Next;
                mUndoCommands.Remove(node);
                if (node == end)
                {
                    break;
                }
                node = next;
            }

            // 插入 composite
            LinkedListNode<IUndoCommand> newNode;
            if (insertPos != null)
            {
                newNode = mUndoCommands.AddAfter(insertPos, composite);
            }
            else
            {
                newNode = mUndoCommands.AddFirst(composite);
            }

            // 更新索引：合并后的命令视为“当前命令”
            mCommandIndexNode = newNode;
            OnCommandChanged?.Invoke();

            Helper.Log($"[Undo] UndoCommandMgr MergeLast {count} Command");

            return true;
        }
    }
}
