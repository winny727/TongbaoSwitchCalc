using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.Undo
{
    public interface IUndoCommand
    {
        bool Execute();
        void Undo();
        void Redo();
        //bool Merge(IUndoCommand other);
    }
}
