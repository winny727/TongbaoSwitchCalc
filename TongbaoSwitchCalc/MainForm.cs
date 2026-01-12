using ArknightsRoguelikeRec;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.Impl;
using TongbaoSwitchCalc.Simulation;

namespace TongbaoSwitchCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private IRandomGenerator mRandom;
        private SwitchSimulator mSwitchSimulator;

        private SquadType mSelectedSquadType = SquadType.Flower;
        private int mSelectedTongbaoPosIndex = -1;

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
            mSwitchSimulator = new SwitchSimulator(mPlayerData);
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
                    SelectTongbaoPos(i % names.Length);
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
                comboBoxSquad.Items.Add(new SquadComboBoxItem(type));
            }
            comboBoxSquad.SelectedIndex = 0;

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

        private void UpdateTongbaoView(int posIndex)
        {
            if (posIndex < 0 || posIndex >= listViewTongbao.Items.Count)
            {
                return;
            }

            var sb = mTempStringBuilder;
            sb.Clear();

            Tongbao tongbao = mPlayerData.GetTongbao(posIndex);
            ListViewItem item = listViewTongbao.Items[posIndex];
            if (tongbao != null)
            {
                sb.Append('[')
                  .Append(posIndex + 1)
                  .Append(']');
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                if (tongbao.RandomResType != ResType.None)
                {
                    sb.Append("\n(")
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
                  .Append(posIndex + 1)
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
            sb.Append(")\n");

            sb.Append("源石锭: ").Append(ingots).Append('(');
            AppendSigned(sb, deltaIngots);
            sb.Append(")\n");

            sb.Append("票券: ").Append(coupon).Append('(');
            AppendSigned(sb, deltaCoupon);
            sb.Append(")\n");

            sb.Append("烛火: ").Append(candle).Append('(');
            AppendSigned(sb, deltaCandle);
            sb.Append(")\n");

            sb.Append("鸿蒙开荒烛火: ").Append(primalFarmingCandle).Append('\n');

            sb.Append("护盾: ").Append(shield).Append('(');
            AppendSigned(sb, deltaShield);
            sb.Append(")\n");

            sb.Append("希望: ").Append(hope).Append('(');
            AppendSigned(sb, deltaHope);
            sb.Append(")\n");

            lblRes.Text = sb.ToString();
            sb.Clear();

            int posIndex = mSelectedTongbaoPosIndex;
            Tongbao tongbao = mPlayerData.GetTongbao(posIndex);

            if (tongbao == null)
            {
                sb.Append("当前未选中通宝\n");
            }
            else
            {
                sb.Append("当前选中通宝: [")
                  .Append(posIndex + 1)
                  .Append(']');
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                sb.Append('\n');
            }

            sb.Append("当前交换次数: ")
              .Append(mPlayerData.SwitchCount)
              .Append('\n');

            sb.Append("下次交换消耗生命值: ")
              .Append(mPlayerData.GetNextSwitchCostLifePoint());

            lblCurrent.Text = sb.ToString();
            sb.Clear();

            if (mRecordForm.Visible && mOutputResultChanged)
            {
                mRecordForm.Content = mOutputResult ?? string.Empty;
                mOutputResultChanged = false;
            }
        }

        private void OnSelectNewRandomTongbao(int id, int posIndex)
        {
            Tongbao tongbao = Tongbao.CreateTongbao(id, mRandom);
            mPlayerData.InsertTongbao(tongbao, posIndex);
            UpdateTongbaoView(posIndex);
            UpdateView();
        }

        private void OnSelectNewCustomTongbao(int id, int posIndex,
            ResType randomResType = ResType.None, int randomResCount = 0)
        {
            Tongbao tongbao = Tongbao.CreateTongbao(id);
            tongbao.ApplyRandomRes(randomResType, randomResCount);
            mPlayerData.InsertTongbao(tongbao, posIndex);
            UpdateTongbaoView(posIndex);
            UpdateView();
        }

        private void SelectTongbaoPos(int posIndex)
        {
            mSelectedTongbaoPosIndex = posIndex;
            UpdateView();
        }

        private void SwitchSimulation()
        {
            mSwitchSimulator.MaxSimulateCount = (int)numSimCnt.Value;
            mSwitchSimulator.NextSwitchPosIndex = mSelectedTongbaoPosIndex;
            mSwitchSimulator.ForceSwitch = checkBoxForceSwitch.Checked;
            mSwitchSimulator.Simulate();
            mOutputResult = mSwitchSimulator.OutputResult;
            mOutputResultChanged = true;
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                UpdateTongbaoView(i);
            }
            UpdateView();
        }

        private void SwitchOnce(bool force = false)
        {
            int posIndex = mSelectedTongbaoPosIndex;
            if (!mPlayerData.SwitchTongbao(posIndex, force))
            {
                Tongbao tongbao = mPlayerData.GetTongbao(posIndex);
                if (tongbao == null)
                {
                    MessageBox.Show("交换失败，请先选中一个通宝。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!tongbao.CanSwitch())
                {
                    MessageBox.Show($"交换失败，选中通宝[{Helper.GetTongbaoFullName(tongbao.Id)}]无法交换。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!force && !mPlayerData.HasEnoughSwitchLife())
                {
                    MessageBox.Show($"交换失败，当前生命值不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                MessageBox.Show("交换失败，请检查当前配置和状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!string.IsNullOrEmpty(mOutputResult))
            {
                mOutputResult += "\n";
            }
            mOutputResult = $"({mPlayerData.SwitchCount}) {mPlayerData.LastSwitchResult}";
            mOutputResultChanged = true;
            UpdateTongbaoView(posIndex);
            UpdateView();
        }

        private void cbSquad_SelectedIndexChanged(object sender, EventArgs e)
        {
            SquadComboBoxItem item = comboBoxSquad.SelectedItem as SquadComboBoxItem;
            //if (mPlayerData.SwitchCount > 0)
            //{
            //    var result = MessageBox.Show("切换分队会重置当前交换次数，是否继续？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //    if (result == DialogResult.No)
            //    {
            //        return;
            //    }
            //    mPlayerData.SwitchCount = 0;
            //}

            mSelectedSquadType = item?.Value ?? SquadType.Flower;
            mPlayerData.SetSquadType(mSelectedSquadType);
            InitTongbaoSlot();
        }

        private void checkBoxFortune_CheckedChanged(object sender, EventArgs e)
        {
            mPlayerData.SetSpecialCondition(SpecialConditionFlag.Collectible_Fortune, checkBoxFortune.Checked);
        }

        // 选择通宝槽位
        private void listViewTongbao_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewTongbao.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewTongbao.SelectedItems[0];
                int posIndex = listViewTongbao.Items.IndexOf(selectedItem);
                SelectTongbaoPos(posIndex);
            }
        }

        // 双击通宝槽位
        private void listViewTongbao_ItemActivate(object sender, EventArgs e)
        {
            if (listViewTongbao.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewTongbao.SelectedItems[0];
                int posIndex = listViewTongbao.Items.IndexOf(selectedItem);

                //if (mPlayerData.SwitchCount > 0)
                //{
                //    var result = MessageBox.Show("添加/更改通宝会重置当前交换次数，是否继续？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //    if (result == DialogResult.No)
                //    {
                //        return;
                //    }
                //    mPlayerData.SwitchCount = 0;
                //}

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
                    OnSelectNewRandomTongbao(targetId, posIndex);
                    //OnSelectNewCustomTongbao(targetId, posIndex);
                }
            }
        }

        private void btnSwitch_Click(object sender, EventArgs e)
        {
            SwitchOnce(checkBoxForceSwitch.Checked);
        }

        private void btnSimulation_Click(object sender, EventArgs e)
        {
            SwitchSimulation();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
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
            mRecordForm.Activate();
        }

        private void ClearRecord()
        {
            mSwitchSimulator.ClearOutputResult();
            mOutputResult = string.Empty;
            mRecordForm.Content = string.Empty;
            mOutputResultChanged = false;
        }
    }
}
