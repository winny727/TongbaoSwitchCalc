using ArknightsRoguelikeRec;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;
using TongbaoSwitchCalc.Impl;
using TongbaoSwitchCalc.Impl.Simulation;

namespace TongbaoSwitchCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private RandomGenerator mRandom;
        private SwitchSimulator mSwitchSimulator;
        private DataCollector mDataCollector;

        private SquadType mSelectedSquadType = default;
        private SimulationType mSelectedSimulationType = default;
        private int mSelectedTongbaoSlotIndex = -1;
        private bool mLastSwitchIsSimulation = false;

        private string mOutputResult;
        private bool mOutputResultChanged = false;
        private readonly RecordForm mRecordForm = new RecordForm();
        private readonly StringBuilder mTempStringBuilder = new StringBuilder();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= 0x02000000; //双缓冲
                return createParams;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            InitDataModel();
            InitView();

            //DataModelTest();
            UpdateView();
        }

        private void InitDataModel()
        {
            Helper.InitConfig();
            mRandom = new RandomGenerator();
            mPlayerData = new PlayerData(mRandom);
            mDataCollector = new DataCollector();
            mSwitchSimulator = new SwitchSimulator(mPlayerData, mDataCollector);
            InitPlayerData();
        }

        private void InitPlayerData()
        {
            mPlayerData.Init(mSelectedSquadType, new Dictionary<ResType, int>()
            {
                { ResType.LifePoint, (int)numHp.Value },
                { ResType.OriginiumIngots, (int)numIngots.Value },
                { ResType.Coupon, (int)numCoupon.Value },
                { ResType.Candles, (int)numCandle.Value },
                { ResType.Shield, (int)numShield.Value },
                { ResType.Hope, (int)numHope.Value },
            });
        }

        private void DataModelTest()
        {
            string[] names = new string[] { "大炎通宝", "奇土生金", "水生木护", "金寒水衍", "投木炎延",
            "西廉贞", "北刺面", "南见山", "东缺角", };

            foreach (var name in names)
            {
                TongbaoConfig config = Helper.GetTongbaoConfigByName(name);
                if (config != null)
                {
                    Tongbao tongbao = Tongbao.CreateTongbao(config.Id);
                    mPlayerData.AddTongbao(tongbao);
                }
            }

            using (CodeTimer.StartNew("Test"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    SelectTongbaoSlot(i % names.Length);
                    SwitchOnce(true);
                }
            }

            for (int i = 0; i < mPlayerData.TongbaoBox.Length; i++)
            {
                string tongbaoName = mPlayerData.TongbaoBox[i] != null ?
                    Helper.GetTongbaoFullName(mPlayerData.TongbaoBox[i].Id) : "Empty";
                Helper.Log($"[{i}]={tongbaoName}");
            }

            foreach (ResType type in Enum.GetValues(typeof(ResType)))
            {
                Helper.Log($"[{Define.GetResName(type)}]={mPlayerData.GetResValue(type)}");
            }
        }

        private void InitView()
        {
            Helper.InitResources();

            comboBoxSquad.DisplayMember = "Key";
            comboBoxSquad.ValueMember = "Value";
            comboBoxSquad.Items.Clear();
            foreach (SquadType type in Enum.GetValues(typeof(SquadType)))
            {
                string key = Define.GetSquadName(type);
                comboBoxSquad.Items.Add(new ComboBoxItem<SquadType>(key, type));
            }
            comboBoxSquad.SelectedIndex = 0;

            comboBoxSimMode.DisplayMember = "Key";
            comboBoxSimMode.ValueMember = "Value";
            comboBoxSimMode.Items.Clear();
            foreach (SimulationType type in Enum.GetValues(typeof(SimulationType)))
            {
                string key = SimulationDefine.GetSimulationName(type);
                comboBoxSimMode.Items.Add(new ComboBoxItem<SimulationType>(key, type));
            }
            comboBoxSimMode.SelectedIndex = 0;

            checkBoxFortune.Checked = false;

            Helper.SetupResNumberic(mPlayerData, numHp, ResType.LifePoint);
            Helper.SetupResNumberic(mPlayerData, numIngots, ResType.OriginiumIngots);
            Helper.SetupResNumberic(mPlayerData, numCoupon, ResType.Coupon);
            Helper.SetupResNumberic(mPlayerData, numCandle, ResType.Candles);
            Helper.SetupResNumberic(mPlayerData, numShield, ResType.Shield);
            Helper.SetupResNumberic(mPlayerData, numHope, ResType.Hope);

            InitTongbaoView();

            mRecordForm.SetClearCallback(ClearRecord);
        }

        private void InitTongbaoView()
        {
            // InitImageList
            listViewTongbao.LargeImageList?.Dispose();
            listViewTongbao.LargeImageList = Helper.CreateTongbaoImageList(new Size(54, 54));
            listViewTongbao.SmallImageList = Helper.CreateTongbaoImageList(new Size(24, 24));

            // InitSlot
            InitTongbaoSlot();
            listViewTongbao.SelectedItems.Clear();
        }

        private void InitTongbaoSlot()
        {
            listViewTongbao.Items.Clear();
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                ListViewItem item = new ListViewItem
                {
                    Name = "TongbaoSlot_" + (i + 1),
                    Text = $"[{i + 1}] (空)",
                    ToolTipText = "双击添加/更改通宝",
                    ImageKey = "Empty",
                };
                listViewTongbao.Items.Add(item);
                UpdateTongbaoView(i);
            }
        }

        private void UpdateAllTongbaoView()
        {
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                UpdateTongbaoView(i);
            }
        }

        private void UpdateTongbaoView(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= listViewTongbao.Items.Count)
            {
                return;
            }

            var sb = mTempStringBuilder;
            sb.Clear();

            Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);
            ListViewItem item = listViewTongbao.Items[slotIndex];
            if (tongbao != null)
            {
                sb.Append('[')
                  .Append(slotIndex + 1)
                  .Append(']');
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                if (tongbao.RandomResType != ResType.None)
                {
                    sb.AppendLine()
                      .Append('(')
                      .Append(Define.GetResName(tongbao.RandomResType))
                      .Append('+')
                      .Append(tongbao.RandomResCount)
                      .Append(')');
                }
                item.Text = sb.ToString();
                item.ImageKey = tongbao.Id.ToString();
            }
            else
            {
                sb.Append('[')
                  .Append(slotIndex + 1)
                  .Append("] (空)");
                item.Text = sb.ToString();
                item.ImageKey = "Empty";
            }
            sb.Clear();
        }

        private void UpdateView()
        {
            var sb = mTempStringBuilder;
            sb.Clear();

            int hp = mPlayerData.GetResValue(ResType.LifePoint);
            int ingots = mPlayerData.GetResValue(ResType.OriginiumIngots);
            int coupon = mPlayerData.GetResValue(ResType.Coupon);
            int candle = mPlayerData.GetResValue(ResType.Candles);
            int primalFarmingCandle = mPlayerData.GetResValue(ResType.PrimalFarmingCandles);
            int shield = mPlayerData.GetResValue(ResType.Shield);
            int hope = mPlayerData.GetResValue(ResType.Hope);

            int deltaHp = hp - (int)numHp.Value;
            int deltaIngots = ingots - (int)numIngots.Value;
            int deltaCoupon = coupon - (int)numCoupon.Value;
            int deltaCandle = candle - (int)numCandle.Value;
            int deltaShield = shield - (int)numShield.Value;
            int deltaHope = hope - (int)numHope.Value;

            static void AppendSigned(StringBuilder sb, int value)
            {
                if (value > 0)
                    sb.Append('+');
                sb.Append(value);
            }

            sb.Append("生命值: ").Append(hp).Append('(');
            AppendSigned(sb, deltaHp);
            sb.AppendLine(")");

            sb.Append("源石锭: ").Append(ingots).Append('(');
            AppendSigned(sb, deltaIngots);
            sb.AppendLine(")");

            sb.Append("票券: ").Append(coupon).Append('(');
            AppendSigned(sb, deltaCoupon);
            sb.AppendLine(")");

            sb.Append("烛火: ").Append(candle).Append('(');
            AppendSigned(sb, deltaCandle);
            sb.AppendLine(")");

            sb.Append("鸿蒙开荒烛火: ")
              .Append(primalFarmingCandle)
              .AppendLine();

            sb.Append("护盾: ").Append(shield).Append('(');
            AppendSigned(sb, deltaShield);
            sb.AppendLine(")");

            sb.Append("希望: ").Append(hope).Append('(');
            AppendSigned(sb, deltaHope);
            sb.AppendLine(")");

            lblRes.Text = sb.ToString();
            sb.Clear();

            int slotIndex = mSelectedTongbaoSlotIndex;
            Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);

            if (tongbao == null)
            {
                sb.AppendLine("当前选中通宝:")
                  .AppendLine("无");
            }
            else
            {
                sb.AppendLine("当前选中通宝:")
                  .Append('[')
                  .Append(slotIndex + 1)
                  .Append(']');
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                sb.AppendLine();
            }

            sb.Append("当前交换次数: ")
              .Append(mPlayerData.SwitchCount)
              .AppendLine();

            sb.Append("下次交换消耗生命值: ")
              .Append(mPlayerData.NextSwitchCostLifePoint);

            lblCurrent.Text = sb.ToString();
            sb.Clear();

            if (mRecordForm.Visible && mOutputResultChanged)
            {
                mRecordForm.Content = mOutputResult ?? string.Empty;
                mOutputResultChanged = false;
            }
        }

        private void OnSelectNewRandomTongbao(int id, int slotIndex)
        {
            Tongbao tongbao = Tongbao.CreateTongbao(id, mRandom);
            mPlayerData.InsertTongbao(tongbao, slotIndex);
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private void OnSelectNewCustomTongbao(int id, int slotIndex,
            ResType randomResType = ResType.None, int randomResCount = 0)
        {
            Tongbao tongbao = Tongbao.CreateTongbao(id);
            tongbao.ApplyRandomRes(randomResType, randomResCount);
            mPlayerData.InsertTongbao(tongbao, slotIndex);
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private void SelectTongbaoSlot(int slotIndex)
        {
            mSelectedTongbaoSlotIndex = slotIndex;
            UpdateView();
        }

        private void SwitchOnce(bool force = false)
        {
            if (mLastSwitchIsSimulation)
            {
                ClearRecord();
            }

            mLastSwitchIsSimulation = false;
            int slotIndex = mSelectedTongbaoSlotIndex;
            mDataCollector?.OnSwitchStepBegin(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData));
            if (!mPlayerData.SwitchTongbao(slotIndex, force))
            {
                Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);
                if (tongbao == null)
                {
                    mDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.SelectedEmpty);
                    MessageBox.Show("交换失败，请先选中一个通宝。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!tongbao.CanSwitch())
                {
                    mDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.TongbaoCanNotSwitch);
                    MessageBox.Show($"交换失败，选中通宝[{Helper.GetTongbaoFullName(tongbao.Id)}]无法交换。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!force && !mPlayerData.HasEnoughSwitchLife)
                {
                    mDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.LifePointNotEnough);
                    MessageBox.Show($"交换失败，当前生命值不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                mDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.UnknownError);
                MessageBox.Show("交换失败，请检查当前配置和状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.Success);
            mOutputResult += $"({mPlayerData.SwitchCount}) {mDataCollector.LastSwitchResult}{Environment.NewLine}";
            mOutputResultChanged = true;
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private void SwitchSimulate(SimulationType mode)
        {
            if (checkBoxAutoRevert.Checked)
            {
                ResetPlayerData();
            }
            mLastSwitchIsSimulation = true;
            mSwitchSimulator.TotalSimulationCount = (int)numSimCnt.Value;
            mSwitchSimulator.MinimumLifePoint = (int)numMinHp.Value;
            mSwitchSimulator.NextSwitchSlotIndex = mSelectedTongbaoSlotIndex;
            mSwitchSimulator.Simulate(mode);
            mOutputResult = mDataCollector.OutputResult;
            mOutputResultChanged = true;
            UpdateAllTongbaoView();
            UpdateView();
        }

        private void ResetPlayerData()
        {
            if (mLastSwitchIsSimulation)
            {
                mSwitchSimulator.RevertPlayerData();
            }
            mPlayerData.SwitchCount = 0;
            mPlayerData.InitResValues(new Dictionary<ResType, int>()
            {
                { ResType.LifePoint, (int)numHp.Value },
                { ResType.OriginiumIngots, (int)numIngots.Value },
                { ResType.Coupon, (int)numCoupon.Value },
                { ResType.Candles, (int)numCandle.Value },
                { ResType.Shield, (int)numShield.Value },
                { ResType.Hope, (int)numHope.Value },
            });
        }

        private void comboBoxSquad_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxItem<SquadType> item = comboBoxSquad.SelectedItem as ComboBoxItem<SquadType>;
            mSelectedSquadType = item?.Value ?? default;
            mPlayerData.SetSquadType(mSelectedSquadType);
            InitTongbaoSlot();
        }

        private void checkBoxFortune_CheckedChanged(object sender, EventArgs e)
        {
            mPlayerData.SetSpecialCondition(SpecialConditionFlag.Collectible_Fortune, checkBoxFortune.Checked);
        }

        private void comboBoxSimMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxItem<SimulationType> item = comboBoxSimMode.SelectedItem as ComboBoxItem<SimulationType>;
            mSelectedSimulationType = item?.Value ?? default;
        }

        // 选择通宝槽位
        private void listViewTongbao_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewTongbao.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewTongbao.SelectedItems[0];
                int slotIndex = listViewTongbao.Items.IndexOf(selectedItem);
                SelectTongbaoSlot(slotIndex);
            }
        }

        // 双击通宝槽位
        private void listViewTongbao_ItemActivate(object sender, EventArgs e)
        {
            if (listViewTongbao.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewTongbao.SelectedItems[0];
                int slotIndex = listViewTongbao.Items.IndexOf(selectedItem);

                // 测试，随机添加通宝
                var configs = TongbaoConfig.GetAllTongbaoConfigs();
                int random = mRandom.Next(0, configs.Count);
                int index = 0;
                int targetId = -1;
                foreach (var item in configs)
                {
                    if (index == random)
                    {
                        targetId = item.Value.Id;
                        break;
                    }
                    index++;
                }
                if (targetId > 0)
                {
                    OnSelectNewRandomTongbao(targetId, slotIndex);
                    //OnSelectNewCustomTongbao(targetId, slotIndex);
                }
            }
        }

        private void btnSwitch_Click(object sender, EventArgs e)
        {
            SwitchOnce();
        }

        private void btnSimulation_Click(object sender, EventArgs e)
        {
            SwitchSimulate(mSelectedSimulationType);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetPlayerData();
            if (mLastSwitchIsSimulation)
            {
                UpdateAllTongbaoView();
            }
            ClearRecord();
            UpdateView();
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            int hp = mPlayerData.GetResValue(ResType.LifePoint);
            int ingots = mPlayerData.GetResValue(ResType.OriginiumIngots);
            int coupon = mPlayerData.GetResValue(ResType.Coupon);
            int candle = mPlayerData.GetResValue(ResType.Candles);
            int shield = mPlayerData.GetResValue(ResType.Shield);
            int hope = mPlayerData.GetResValue(ResType.Hope);

            numHp.Value = hp;
            numIngots.Value = ingots;
            numCoupon.Value = coupon;
            numCandle.Value = candle;
            numShield.Value = shield;
            numHope.Value = hope;
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (mOutputResultChanged)
            {
                mRecordForm.Content = mOutputResult ?? string.Empty;
                mOutputResultChanged = false;
            }
            mRecordForm.Show();
            mRecordForm.WindowState = FormWindowState.Normal;
            mRecordForm.Focus();
        }

        private void ClearRecord()
        {
            mDataCollector.ClearData();
            mOutputResult = string.Empty;
            mRecordForm.Content = string.Empty;
            mOutputResultChanged = false;
            GC.Collect();
        }
    }
}
