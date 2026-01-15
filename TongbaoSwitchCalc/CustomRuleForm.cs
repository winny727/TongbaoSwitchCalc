using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc
{
    public partial class CustomRuleForm : Form
    {
        public SimulationRuleType Type { get; }

        public object[] SelectedParams { get; private set; }

        public CustomRuleForm(SimulationRuleType type)
        {
            this.Type = type;
            InitializeComponent();
            InitView();
        }

        private void InitView()
        {
            label1.Text = SimulationDefine.GetSimulationRuleName(Type);
            numericUpDown1.Visible = false;
            pictureBox1.Visible = false;
            label3.Visible = false;
            btnSel.Visible = false;

            switch (Type)
            {
                case SimulationRuleType.PrioritySlot:
                    label2.Text = "钱盒槽位编号";
                    numericUpDown1.Visible = true;
                    break;
                case SimulationRuleType.AutoStop:
                    label2.Text = "目标/降级通宝";
                    label3.Text = "未选择";
                    pictureBox1.Image = null;
                    label3.Visible = true;
                    pictureBox1.Visible = true;
                    btnSel.Visible = true;
                    break;
                case SimulationRuleType.ExpectationTongbao:
                    label2.Text = "期望获得通宝";
                    label3.Text = "未选择";
                    pictureBox1.Image = null;
                    label3.Visible = true;
                    pictureBox1.Visible = true;
                    btnSel.Visible = true;
                    break;
                default:
                    break;
            }
        }

        public void SetSelectedParams(params object[] args)
        {
            SelectedParams = args;
            switch (Type)
            {
                case SimulationRuleType.PrioritySlot:
                    numericUpDown1.Value = GetArg<int>(0) + 1;
                    break;
                case SimulationRuleType.AutoStop:
                    UpdateTongbaoInfo(GetArg<int>(0));
                    break;
                case SimulationRuleType.ExpectationTongbao:
                    UpdateTongbaoInfo(GetArg<int>(0));
                    break;
                default:
                    break;
            }
        }

        public void SetNumericRange(int min, int max)
        {
            numericUpDown1.Minimum = min;
            numericUpDown1.Maximum = max;
        }

        private void UpdateTongbaoInfo(int id)
        {
            pictureBox1.Image = new Bitmap(Helper.GetTongbaoImage(id), new Size(pictureBox1.Width, pictureBox1.Height));
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config != null)
            {
                label3.Text = config.Name;
            }
            else
            {
                label3.Text = "未选择";
            }
        }

        private T GetArg<T>(int index)
        {
            if (SelectedParams != null && index >= 0 && index < SelectedParams.Length)
            {
                if (SelectedParams[index] is T result)
                {
                    return result;
                }
            }
            return default;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SelectedParams = new object[] { (int)(numericUpDown1.Value - 1) };
        }

        private void btnSel_Click(object sender, EventArgs e)
        {
            SelectorForm selectorForm = new SelectorForm(SelectMode.SingleSelect);
            if (SelectedParams != null && SelectedParams.Length > 0)
            {
                selectorForm.SetSelectedIds(new int[] { GetArg<int>(0) });
            }
            if (selectorForm.ShowDialog() == DialogResult.OK)
            {
                SelectedParams = new object[] { selectorForm.SelectedId };
                UpdateTongbaoInfo(selectorForm.SelectedId);
            }
        }
    }
}
