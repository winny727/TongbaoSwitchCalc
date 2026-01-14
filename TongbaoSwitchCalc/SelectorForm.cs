using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;

namespace TongbaoSwitchCalc
{
    public enum SelectMode
    {
        None,
        SingleSelect,
        SingleSelectWithComboBox, // 单选+品相
        MultiSelect,
    }

    public partial class SelectorForm : Form
    {
        public SelectMode SelectMode { get; } = SelectMode.None;
        public bool IsSingleSelect => SelectMode == SelectMode.SingleSelect ||
            SelectMode == SelectMode.SingleSelectWithComboBox;
        public bool IsMultiSelect => SelectMode == SelectMode.MultiSelect;

        public int SelectedId { get; private set; }
        public List<int> SelectedIds { get; private set; } = new List<int>();
        public RandomResDefine SelectedRandomRes { get; private set; }
        public bool IsSelected => SelectedIds.Count > 0;

        private ListViewItem[] mListViewItems;
        private bool mRawSet = false;

        public SelectorForm(SelectMode type = SelectMode.None)
        {
            SelectMode = type;
            InitializeComponent();
            InitListView();
        }

        private int GetListViewItemId(ListViewItem item) => (item?.Tag is int id) ? id : -1;

        public void SetSelectedIds(List<int> selectedIds)
        {
            SelectedIds.Clear();
            if (IsMultiSelect)
            {
                foreach (var id in selectedIds)
                {
                    if (!SelectedIds.Contains(id))
                    {
                        SelectedIds.Remove(id);
                    }
                }
            }
            else if (IsSingleSelect && selectedIds.Count > 0)
            {
                SelectedIds.Add(selectedIds[0]);
            }
            SelectedId = SelectedIds.Count > 0 ? SelectedIds[0] : -1;
            UpdateChecked();
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
            SelectedId = -1;
            SelectedIds.Clear();
            SelectedRandomRes = null;

            comboBox1.Items.Clear();
            comboBox1.Visible = SelectMode == SelectMode.SingleSelectWithComboBox;
            if (SelectMode == SelectMode.SingleSelectWithComboBox)
            {
                comboBox1.DisplayMember = "Key";
                comboBox1.ValueMember = "Value";
                comboBox1.Items.Add(new ComboBoxItem<RandomResDefine>("无品相", new RandomResDefine(0, ResType.None, 0)));
                comboBox1.Items.Add(new ComboBoxItem<RandomResDefine>("根据实际概率随机品相", null));
                foreach (var item in Define.RandomResDefines)
                {
                    string key = $"品相: {Define.GetResName(item.ResType)}+{item.ResCount}";
                    comboBox1.Items.Add(new ComboBoxItem<RandomResDefine>(key, item));
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
                    Tag = config.Id,
                    ImageKey = config.Id.ToString(),
                };
                listViewItems.Add(listViewItem);
            }
            mListViewItems = listViewItems.ToArray();
            listView1.SelectedItems.Clear();

            UpdateListView();
        }

        private void UpdateListView()
        {
            if (mListViewItems == null || mListViewItems.Length == 0)
            {
                mRawSet = false;
                return;
            }

            if (string.IsNullOrEmpty(textBox1.Text))
            {
                listView1.Items.Clear();
                listView1.Items.AddRange(mListViewItems);
                return;
            }

            listView1.Items.Clear();
            foreach (ListViewItem item in mListViewItems)
            {
                if (item.Text.IndexOf(textBox1.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    listView1.Items.Add(item);
                }
            }
        }

        private void UpdateChecked()
        {
            foreach (ListViewItem item in listView1.Items)
            {
                int id = GetListViewItemId(item);
                if (SelectedIds.Contains(id))
                {
                    item.Checked = true;
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mRawSet)
            {
                return;
            }

            SelectedId = -1;
            SelectedIds.Clear();
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
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    if (item != null && item != e.Item)
                    {
                        item.Checked = false;
                    }
                }
                SelectedIds.Clear();
                if (e.Item.Checked)
                {
                    SelectedIds.Add(id);
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

            SelectedId = SelectedIds.Count > 0 ? SelectedIds[0] : -1;
            btnOK.Enabled = SelectedIds.Count > 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxItem<RandomResDefine> item = comboBox1.SelectedItem as ComboBoxItem<RandomResDefine>;
            SelectedRandomRes = item?.Value;
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
