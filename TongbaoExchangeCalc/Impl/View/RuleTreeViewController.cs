using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;


namespace TongbaoExchangeCalc.Impl.View
{
    public class RuleTreeViewController : ISimulationRuleController
    {
        private readonly TreeView mRuleTreeView;
        private readonly PlayerData mPlayerData;

        public RuleTreeViewController(TreeView treeView, PlayerData playerData)
        {
            mRuleTreeView = treeView ?? throw new ArgumentNullException(nameof(treeView));
            mPlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));

            mRuleTreeView.AfterCheck += OnRuleAfterCheck;
            mRuleTreeView.DoubleClick += OnRuleDoubleClick;
            mRuleTreeView.KeyDown += OnTreeViewKeyDown;
        }

        public void BindButtons(Button btnAdd, Button btnRemove, Button btnUp, Button btnDown)
        {
            btnAdd.Click += OnAddClick;
            btnRemove.Click += OnRemoveClick;
            btnUp.Click += OnMoveUpClick;
            btnDown.Click += OnMoveDownClick;
        }

        public void InitRuleTreeView()
        {
            mRuleTreeView.Nodes.Clear();
            foreach (var item in Helper.SimulationRulePresets)
            {
                string name = SimulationDefine.GetSimulationRuleName(item.Key);
                UniqueRuleCollection collection = new UniqueRuleCollection(item.Key);
                foreach (var preset in item.Value)
                {
                    collection.Add(preset);
                }
                TreeNode treeNode = mRuleTreeView.Nodes.Add(name);
                treeNode.Checked = true;
                treeNode.Tag = collection;
            }
            UpdateRuleTreeView();
            mRuleTreeView.ExpandAll();
        }

        public void UpdateRuleTreeView()
        {
            mRuleTreeView.BeginUpdate();
            try
            {
                foreach (TreeNode treeNode in mRuleTreeView.Nodes)
                {
                    if (treeNode.Tag is UniqueRuleCollection collection)
                    {
                        for (int i = 0; i < collection.Count; i++)
                        {
                            SimulationRule rule = collection[i];
                            string text = rule.GetRuleString();
                            TreeNode child;
                            if (i < treeNode.Nodes.Count)
                            {
                                child = treeNode.Nodes[i];
                            }
                            else
                            {
                                child = new TreeNode();
                                treeNode.Nodes.Add(child);
                            }
                            child.Text = text;
                            child.Checked = rule.Enabled;
                            child.Tag = rule;
                        }
                        for (int i = treeNode.Nodes.Count - 1; i >= collection.Count; i--)
                        {
                            treeNode.Nodes.RemoveAt(i);
                        }
                    }
                }
            }
            finally
            {
                mRuleTreeView.EndUpdate();
            }
        }

        public void ApplySimulationRule(ExchangeSimulator simulator)
        {
            if (simulator == null)
            {
                return;
            }

            simulator.ExchangeableSlots.Clear();
            simulator.UnexchangeableTongbaoIds.Clear();
            simulator.ExpectedTongbaoIds.Clear();
            simulator.PriorityTongbaoIds.Clear();

            foreach (TreeNode treeNode in mRuleTreeView.Nodes)
            {
                if (treeNode.Tag is UniqueRuleCollection collection && treeNode.Checked)
                {
                    foreach (var item in collection)
                    {
                        if (!item.Enabled) continue;
                        item.ApplyRule(simulator);
                    }
                }
            }
        }

        private UniqueRuleCollection GetRuleCollection(SimulationRule rule)
        {
            if (rule == null)
            {
                return null;
            }

            foreach (TreeNode treeNode in mRuleTreeView.Nodes)
            {
                if (treeNode.Tag is UniqueRuleCollection collection && collection.Type == rule.Type)
                {
                    return collection;
                }
            }
            return null;
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = mRuleTreeView.SelectedNode;
            if (selectedNode?.Tag is SimulationRule rule)
            {
                TreeNode parentNode = selectedNode.Parent;
                var collection = GetRuleCollection(rule);
                int index = collection.IndexOf(rule) + 1;
                CustomRuleForm customRuleForm = new CustomRuleForm(collection.Type);

                // 设Numeric范围+自动填一个没填过的SlotIndex
                if (rule.Type == SimulationRuleType.ExchangeableSlot)
                {
                    customRuleForm.SetNumericRange(1, mPlayerData.MaxTongbaoCount);
                    AutoSetNextValidSlotIndex(customRuleForm, collection);
                }

                if (customRuleForm.ShowDialog() == DialogResult.OK)
                {
                    SimulationRule newRule = SimulationDefine.CreateSimulationRule(collection.Type, customRuleForm.SelectedParams);
                    if (collection.Insert(index, newRule))
                    {
                        UpdateRuleTreeView();
                        selectedNode.TreeView.SelectedNode = parentNode.Nodes[index];
                    }
                    else if (newRule != null)
                    {
                        MessageBox.Show("自定义规则添加失败，自定义规则与已有规则冲突", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (selectedNode?.Tag is UniqueRuleCollection collection)
            {
                CustomRuleForm customRuleForm = new CustomRuleForm(collection.Type);

                // 设Numeric范围+自动填一个没填过的SlotIndex
                if (collection.Type == SimulationRuleType.ExchangeableSlot)
                {
                    customRuleForm.SetNumericRange(1, mPlayerData.MaxTongbaoCount);
                    AutoSetNextValidSlotIndex(customRuleForm, collection);
                }

                if (customRuleForm.ShowDialog() == DialogResult.OK)
                {
                    SimulationRule newRule = SimulationDefine.CreateSimulationRule(collection.Type, customRuleForm.SelectedParams);
                    if (collection.Add(newRule))
                    {
                        UpdateRuleTreeView();
                        selectedNode.TreeView.SelectedNode = selectedNode.Nodes[collection.Count - 1];
                    }
                    else if (newRule != null)
                    {
                        MessageBox.Show("自定义规则添加失败，已存在相同规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void OnRemoveClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = mRuleTreeView.SelectedNode;
            if (selectedNode?.Tag is SimulationRule rule)
            {
                TreeNode parentNode = selectedNode.Parent;
                var collection = GetRuleCollection(rule);
                int index = collection.IndexOf(rule);
                collection.RemoveAt(index);
                UpdateRuleTreeView();
                if (index < parentNode.Nodes.Count)
                {
                    selectedNode.TreeView.SelectedNode = parentNode.Nodes[index];
                }
            }
        }

        private void OnMoveUpClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = mRuleTreeView.SelectedNode;
            if (selectedNode?.Tag is SimulationRule rule)
            {
                TreeNode parentNode = selectedNode.Parent;
                var collection = GetRuleCollection(rule);
                int index = collection.IndexOf(rule);
                if (collection.MoveToIndex(rule, index - 1))
                {
                    UpdateRuleTreeView();
                    selectedNode.TreeView.SelectedNode = parentNode.Nodes[index - 1];
                }
            }
        }

        private void OnMoveDownClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = mRuleTreeView.SelectedNode;
            if (selectedNode?.Tag is SimulationRule rule)
            {
                TreeNode parentNode = selectedNode.Parent;
                var collection = GetRuleCollection(rule);
                int index = collection.IndexOf(rule);
                if (collection.MoveToIndex(rule, index + 1))
                {
                    UpdateRuleTreeView();
                    selectedNode.TreeView.SelectedNode = parentNode.Nodes[index + 1];
                }
            }
        }

        private void OnRuleAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is SimulationRule rule)
            {
                rule.Enabled = e.Node.Checked;
            }
        }

        private void OnRuleDoubleClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = mRuleTreeView.SelectedNode;
            if (selectedNode?.Tag is SimulationRule rule)
            {
                var collection = GetRuleCollection(rule);
                int index = collection.IndexOf(rule);
                CustomRuleForm customRuleForm = new CustomRuleForm(collection.Type);
                object[] args = SimulationDefine.GetSimulationRuleArgs(rule);
                customRuleForm.SetSelectedParams(args);
                customRuleForm.SetNumericRange(1, mPlayerData.MaxTongbaoCount);
                if (customRuleForm.ShowDialog() == DialogResult.OK)
                {
                    SimulationRule newRule = SimulationDefine.CreateSimulationRule(rule.Type, customRuleForm.SelectedParams);
                    collection.RemoveAt(index);
                    if (collection.Insert(index, newRule))
                    {
                        UpdateRuleTreeView();
                    }
                    else
                    {
                        collection.Insert(index, rule); // 还原
                        UpdateRuleTreeView();
                        MessageBox.Show("自定义规则修改失败，已存在相同规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void OnTreeViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                OnRemoveClick(sender, e);
            }
        }

        private void AutoSetNextValidSlotIndex(CustomRuleForm customRuleForm, UniqueRuleCollection collection)
        {
            int defaultIndex = 0;
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                bool isExist = false;
                foreach (var item in collection)
                {
                    if (item is SlotIndexRule slotIndexRule && slotIndexRule.SlotIndex == i)
                    {
                        isExist = true;
                        break;
                    }
                }
                if (!isExist)
                {
                    defaultIndex = i;
                    break;
                }
            }
            object[] args = SimulationDefine.GetSimulationRuleArgs(new ExchangeableSlotRule(defaultIndex));
            customRuleForm.SetSelectedParams(args);
        }
    }
}
