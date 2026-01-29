using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;
using static System.Net.Mime.MediaTypeNames;

namespace TongbaoExchangeCalc
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
            this.Text = $"自定义规则 - {SimulationDefine.GetSimulationRuleName(Type)}";

            switch (Type)
            {
                case SimulationRuleType.UnexchangeableTongbao:
                    label2.Text = "选择不可交换通宝:";
                    label4.Text = "模拟时若交换出不可交换通宝，切换到下一个可交换槽位；通常不可交换通宝为目标通宝或降级通宝";
                    InitTongbaoIdView();
                    break;
                case SimulationRuleType.ExpectationTongbao:
                    label2.Text = "选择期望获得通宝:";
                    label4.Text = "持有所有期望通宝时停止此轮模拟";
                    InitTongbaoIdView();
                    break;
                case SimulationRuleType.ExchangeableSlot:
                    label2.Text = "输入可交换钱盒槽位索引:";
                    label4.Text = "模拟时会按顺序在可交换钱盒槽位中交换";
                    InitSlotIndexView();
                    break;
                case SimulationRuleType.PriorityExchangeTongbao:
                    label2.Text = "选择优先交换通宝:";
                    label4.Text = "模拟时会先将所有可交换槽位内的优先交换通宝用于交换";
                    InitTongbaoIdView();
                    break;
                default:
                    break;
            }
        }

        private void InitSlotIndexView()
        {
            pictureBox1.Visible = false;
            label3.Visible = false;
            btnSel.Visible = false;

            numericUpDown1.Visible = true;
            SelectedParams = new object[] { (int)(numericUpDown1.Value - 1) }; // 默认值
        }

        private void InitTongbaoIdView()
        {
            numericUpDown1.Visible = false;

            label3.Text = "未选择";
            pictureBox1.Image = null;
            label3.Visible = true;
            pictureBox1.Visible = true;
            btnSel.Visible = true;
        }

        public void SetSelectedParams(params object[] args)
        {
            SelectedParams = args;
            switch (Type)
            {
                case SimulationRuleType.UnexchangeableTongbao:
                    UpdateTongbaoInfo(GetArg<int>(0));
                    break;
                case SimulationRuleType.ExpectationTongbao:
                    UpdateTongbaoInfo(GetArg<int>(0));
                    break;
                case SimulationRuleType.ExchangeableSlot:
                    numericUpDown1.Value = GetArg<int>(0) + 1;
                    break;
                case SimulationRuleType.PriorityExchangeTongbao:
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
            var image = Helper.GetTongbaoImage(id);
            pictureBox1.Image = image != null ? new Bitmap(image, new Size(pictureBox1.Width, pictureBox1.Height)) : null;

            string name = Helper.GetTongbaoFullName(id);
            label3.Text = !string.IsNullOrEmpty(name) ? name : "未选择";
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
