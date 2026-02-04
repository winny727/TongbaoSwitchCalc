using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TongbaoExchangeCalc.View
{
    class IconGridControl : Panel
    {
        private readonly List<Image> mIcons = new List<Image>();

        public int CellSize { get; set; } = 48;
        public int Spacing { get; set; } = 4;

        private int Columns
        {
            get
            {
                int availableWidth = this.Width;
                if (availableWidth <= 0)
                    return 1;

                int full = CellSize + Spacing;
                int columns = availableWidth / full;
                return Math.Max(1, columns);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int columns = Columns;
            Rectangle GetCellRect(int index)
            {
                int col = index % columns;
                int row = index / columns;

                int x = Spacing + col * (CellSize + Spacing);
                int y = Spacing + row * (CellSize + Spacing);

                return new Rectangle(x, y, CellSize, CellSize);
            }

            for (int i = 0; i < mIcons.Count; i++)
            {
                Rectangle rect = GetCellRect(i);
                if (mIcons[i] != null)
                {
                    e.Graphics.DrawImage(mIcons[i], rect);
                }
                else
                {
                    using var brush = new SolidBrush(Color.Transparent);
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
        }

        public void SetIcons(IReadOnlyList<Image> icons)
        {
            mIcons.Clear();
            if (icons != null)
            {
                mIcons.AddRange(icons);
            }
            this.Invalidate();
        }
    }
}
