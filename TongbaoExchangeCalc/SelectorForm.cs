using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.View;

namespace TongbaoExchangeCalc
{
    public enum SelectMode
    {
        None,
        SingleSelect,
        SingleSelectWithComboBox, // 单选+品相
        ExchangeTongbaoSelector,
        MultiSelect,
    }

    public partial class SelectorForm : Form
    {
        public SelectMode SelectMode { get; } = SelectMode.None;
        public bool IsSingleSelect =>
            SelectMode == SelectMode.SingleSelect ||
            SelectMode == SelectMode.SingleSelectWithComboBox || 
            SelectMode == SelectMode.ExchangeTongbaoSelector;
        public bool IsMultiSelect => SelectMode == SelectMode.MultiSelect;

        public int SelectedId => IsSelected ? SelectedIds[0] : GetFilterDefaultId();
        public List<int> SelectedIds { get; private set; } = new List<int>();
        public RandomEff SelectedRandomEff { get; private set; }
        public bool IsSelected => SelectedIds.Count > 0;

        private readonly List<int> mDisplayIdList = new List<int>();
        private ListViewItem[] mListViewItems;
        private bool mRawSet = false;

        public SelectorForm(SelectMode type = SelectMode.None)
        {
            SelectMode = type;
            InitializeComponent();
            InitListView();
        }

        private int GetListViewItemId(ListViewItem item) => (item?.Tag is TongbaoConfig config) ? config.Id : -1;

        private int GetFilterDefaultId()
        {
            // 若当前为单选，且未选中任何项，则筛选时默认选中筛选后的第一个
            if (IsSingleSelect && !IsSelected && !string.IsNullOrEmpty(textBox1.Text))
            {
                if (listView1.Items.Count > 0)
                {
                    return GetListViewItemId(listView1.Items[0]);
                }
            }
            return -1;
        }

        public void SetSelectedIds(IReadOnlyList<int> selectedIds)
        {
            SelectedIds.Clear();
            if (IsMultiSelect)
            {
                foreach (var id in selectedIds)
                {
                    if (!SelectedIds.Contains(id))
                    {
                        SelectedIds.Add(id);
                    }
                }
            }
            else if (IsSingleSelect && selectedIds.Count > 0)
            {
                SelectedIds.Add(selectedIds[0]);
            }
            UpdateChecked();
        }

        public void SetDisplayIdList(IReadOnlyList<int> displayIds)
        {
            mDisplayIdList.Clear();
            if (displayIds != null)
            {
                mDisplayIdList.AddRange(displayIds);
            }
            UpdateListView();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            mRawSet = true;
            UpdateListView();
            UpdateChecked();
            mRawSet = false;
        }

        private void InitListView()
        {
            SelectedIds.Clear();
            SelectedRandomEff = null;

            comboBox1.Items.Clear();
            comboBox1.Visible = SelectMode == SelectMode.SingleSelectWithComboBox;
            if (SelectMode == SelectMode.SingleSelectWithComboBox)
            {
                comboBox1.DisplayMember = "Key";
                comboBox1.ValueMember = "Value";
                comboBox1.Items.Add(new ComboBoxItem<RandomEff>("无品相", new RandomEff("无品相", 0, ResType.None, 0)));
                comboBox1.Items.Add(new ComboBoxItem<RandomEff>("根据实际概率随机品相", null));
                foreach (var item in Define.RandomEffDefines)
                {
                    string key = $"品相: {Define.GetResName(item.ResType)}+{item.ResCount}";
                    comboBox1.Items.Add(new ComboBoxItem<RandomEff>(key, item));
                }
                comboBox1.SelectedIndex = 0;
            }

            listView1.SmallImageList?.Dispose();
            listView1.SmallImageList = Helper.CreateTongbaoImageList(new Size(16, 16));

            var listViewItems = new List<ListViewItem>();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                ListViewItem listViewItem = new ListViewItem
                {
                    Name = config.Id.ToString(),
                    Text = Helper.GetTongbaoFullName(config.Id),
                    Tag = config,
                    ImageKey = config.Id.ToString(),
                };
                listViewItems.Add(listViewItem);
            }
            mListViewItems = listViewItems.ToArray();
            listView1.SelectedItems.Clear();

            textBox1.Focus();

            UpdateListView();
        }

        private void UpdateListView()
        {
            if (mListViewItems == null || mListViewItems.Length == 0)
            {
                return;
            }

            listView1.Items.Clear();
            foreach (ListViewItem item in mListViewItems)
            {
                int id = GetListViewItemId(item);
                if (SelectMode == SelectMode.ExchangeTongbaoSelector)
                {
                    if (!mDisplayIdList.Contains(id))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(textBox1.Text))
                {
                    if (item.Text.IndexOf(textBox1.Text, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                listView1.Items.Add(item);
            }
        }

        private void UpdateChecked()
        {
            int filterDefaultId = GetFilterDefaultId();
            foreach (ListViewItem item in listView1.Items)
            {
                if (item == null) continue;
                int id = GetListViewItemId(item);
                item.Checked = SelectedIds.Contains(id) || id == filterDefaultId;
                if (IsSingleSelect && item.Checked)
                {
                    item.EnsureVisible();
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mRawSet)
            {
                return;
            }

            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView1.SelectedItems[0];
                selectedItem.Checked = !selectedItem.Checked;
            }
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (mRawSet)
            {
                return;
            }

            mRawSet = true;
            e.Item.Selected = true;
            int id = GetListViewItemId(e.Item);
            if (IsSingleSelect)
            {
                if (e.Item.Checked && !SelectedIds.Contains(id))
                {
                    SelectedIds.Clear();
                    SelectedIds.Add(id);
                    UpdateChecked(); //清空其它选中项
                }
                else if (!e.Item.Checked && SelectedIds.Contains(id))
                {
                    SelectedIds.Clear();
                    UpdateChecked();
                }
            }
            else if (IsMultiSelect)
            {
                if (e.Item.Checked && !SelectedIds.Contains(id))
                {
                    SelectedIds.Add(id);
                }
                else if (!e.Item.Checked && SelectedIds.Contains(id))
                {
                    SelectedIds.Remove(id);
                }
            }
            mRawSet = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxItem<RandomEff> item = comboBox1.SelectedItem as ComboBoxItem<RandomEff>;
            SelectedRandomEff = item?.Value;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            SelectedIds.Clear();
            UpdateChecked();
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            Random random = new Random();
            int index = random.Next(listView1.Items.Count);
            if (index >= 0 && index < listView1.Items.Count)
            {
                listView1.Items[index].Selected = true;
                listView1.EnsureVisible(index);
                listView1.Focus();
            }
        }
    }
}
