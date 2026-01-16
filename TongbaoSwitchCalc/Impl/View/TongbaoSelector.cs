using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc.Impl.View
{
    public enum TongbaoSelectMode
    {
        Default, // 固定第一个
        Random, // 随机
        Specific, // 固定ID
        Dialog, // 弹窗询问
    }

    public class TongbaoSelector : ITongbaoSelector
    {
        public TongbaoSelectMode TongbaoSelectMode { get; set; }
        public int SpecificTongbaoId { get; set; }
        public IRandomGenerator Random { get; private set; }

        public TongbaoSelector(IRandomGenerator random)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int SelectTongbao(IReadOnlyList<int> tongbaoIds)
        {
            if (tongbaoIds == null || tongbaoIds.Count <= 0)
            {
                return -1;
            }

            if (tongbaoIds.Count == 1)
            {
                return tongbaoIds[0];
            }

            if (TongbaoSelectMode == TongbaoSelectMode.Default)
            {
                return tongbaoIds[0];
            }
            else if (TongbaoSelectMode == TongbaoSelectMode.Random)
            {
                int index = Random.Next(0, tongbaoIds.Count);
                return tongbaoIds[index];
            }
            else if (TongbaoSelectMode == TongbaoSelectMode.Specific)
            {
                foreach (var tongbaoId in tongbaoIds)
                {
                    if (tongbaoId == SpecificTongbaoId)
                    {
                        return SpecificTongbaoId;
                    }
                }
            }
            else if (TongbaoSelectMode == TongbaoSelectMode.Dialog)
            {
                SelectorForm selectorForm = new SelectorForm(SelectMode.SwitchTongbaoSelector);
                selectorForm.SetSwitchTongbaoIdList(tongbaoIds);
                if (selectorForm.ShowDialog() == DialogResult.OK)
                {
                    return selectorForm.SelectedId;
                }
            }

            return tongbaoIds[0];
        }
    }
}
