using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.Undo
{
    public class UndoCommandMgr
    {
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

        public int MaxUndoCommand { get; set; } = 100; // 小于0则不限制撤销命令数量

        private void Initialize()
        {

        }

        public void ClearUndoCommands()
        {
            mUndoCommands.Clear();
            mCommandIndexNode = null;
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

            return result;
        }

        public void Undo()
        {
            // 确保有可撤销的命令
            if (mCommandIndexNode != null)
            {
                mCommandIndexNode.Value.Undo();
                mCommandIndexNode = mCommandIndexNode.Previous; // 索引位置-1
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
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            else
            {
                mCommandIndexNode = mUndoCommands.First; // 从-1位置移动到0位置
                mCommandIndexNode.Value.Redo();
            }
        }
    }
}
