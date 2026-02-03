using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TongbaoExchangeCalc.DataModel;

namespace TongbaoExchangeCalc.Impl.View
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
                for (int i = 0; i < tongbaoIds.Count; i++)
                {
                    if (tongbaoIds[i] == SpecificTongbaoId)
                    {
                        return SpecificTongbaoId;
                    }
                }
            }
            else if (TongbaoSelectMode == TongbaoSelectMode.Dialog)
            {
                SelectorForm selectorForm = new SelectorForm(SelectMode.ExchangeTongbaoSelector);
                selectorForm.SetDisplayIdList(tongbaoIds);
                if (selectorForm.ShowDialog() == DialogResult.OK)
                {
                    return selectorForm.SelectedId;
                }
            }

            return tongbaoIds[0];
        }

        public object Clone()
        {
            return new TongbaoSelector((IRandomGenerator)Random.Clone())
            {
                TongbaoSelectMode = TongbaoSelectMode,
                SpecificTongbaoId = SpecificTongbaoId,
            };
        }
    }
}
