using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    public interface ITongbaoSelector
    {
        int SelectTongbao(IReadOnlyList<int> tongbaoIds);
    }
}
