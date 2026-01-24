using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TongbaoExchangeCalc.DataModel;
using TongbaoExchangeCalc.DataModel.Simulation;
using TongbaoExchangeCalc.Impl;
using TongbaoExchangeCalc.Impl.Simulation;
using TongbaoExchangeCalc.Impl.View;
using TongbaoExchangeCalc.View;

namespace TongbaoExchangeCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private RandomGenerator mRandom;
        private TongbaoSelector mTongbaoSelector;
        private SimulationController mSimulationController;
        private PrintDataCollector mPrintDataCollector;
        private ExchangeDataCollector mExchangeDataCollector;
        private StatisticDataCollector mStatisticDataCollector;
        private CompositeDataCollector mCompositeDataCollector;

        private int mSelectedTongbaoSlotIndex = -1;
        private bool mCanRevertPlayerData = false;

        private readonly List<Control> mSimulatingDisableControls = new List<Control>();
        private IconGridControl mIconGrid;
        private RuleTreeViewController RuleTreeViewController;
        private bool mOutputResultChanged = false;
        private RecordForm mRecordForm;
        private readonly StringBuilder mTempStringBuilder = new StringBuilder();

        private readonly List<Image> mTempTongbaoImages = new List<Image>();
        private readonly Dictionary<ResType, int> mTempResValues = new Dictionary<ResType, int>();

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
            mTongbaoSelector = new TongbaoSelector(mRandom);
            mPlayerData = new PlayerData(mTongbaoSelector, mRandom);

            InitSimulationData();
            InitPlayerData();
        }

        private void InitSimulationData()
        {
            // 单轮循环内交换次数超过2000就不收集详细信息
            mPrintDataCollector = new PrintDataCollector(2000);
            mExchangeDataCollector = new ExchangeDataCollector(2000);
            mStatisticDataCollector = new StatisticDataCollector();
            mCompositeDataCollector = new CompositeDataCollector();
            mCompositeDataCollector.AddDataCollector(mPrintDataCollector); // 简单文本输出
            //mCompositeDataCollector.AddDataCollector(mExchangeDataCollector); // 详细数据收集 
            mCompositeDataCollector.AddDataCollector(mStatisticDataCollector);

            mSimulationController = new SimulationController(mPlayerData, mCompositeDataCollector);
        }

        private void InitPlayerData()
        {
            mCanRevertPlayerData = false;
            mTempResValues.Clear();
            mTempResValues.Add(ResType.LifePoint, (int)numHp.Value);
            mTempResValues.Add(ResType.OriginiumIngots, (int)numIngots.Value);
            mTempResValues.Add(ResType.Coupon, (int)numCoupon.Value);
            mTempResValues.Add(ResType.Candles, (int)numCandle.Value);
            mTempResValues.Add(ResType.Shield, (int)numShield.Value);
            mTempResValues.Add(ResType.Hope, (int)numHope.Value);

            SquadType squadType = default;
            if (comboBoxSquad.SelectedItem is ComboBoxItem<SquadType> item)
            {
                squadType = item.Value;
            }
            mPlayerData.Init(squadType, mTempResValues);
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
                    Tongbao tongbao = mPlayerData.CreateTongbao(config.Id);
                    mPlayerData.AddTongbao(tongbao);
                }
            }

            using (CodeTimer.StartNew("Test"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    SelectTongbaoSlot(i % names.Length);
                    ExchangeOnce(true);
                }
            }

            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                Tongbao tongbao = mPlayerData.GetTongbao(i);
                string tongbaoName = tongbao != null ?
                    Helper.GetTongbaoFullName(tongbao.Id) : "Empty";
                Helper.Log($"[{i}]={tongbaoName}");
            }

            foreach (ResType type in Enum.GetValues(typeof(ResType)))
            {
                Helper.Log($"[{Define.GetResName(type)}]={mPlayerData.GetResValue(type)}");
            }
        }

        private void InitView()
        {
            mSimulatingDisableControls.AddRange(new Control[]
            {
                groupBox1, groupBox2, groupBox3, groupBox5,
                listViewTongbao, btnRandom, btnRandomEmpty, btnClear,
                checkBoxOptimize, checkBoxAutoRevert, checkBoxEnableRecord,
                btnExchange, btnReset,
            });

            mIconGrid = new IconGridControl();
            RuleTreeViewController = new RuleTreeViewController(treeViewRule, mPlayerData);
            mRecordForm = new RecordForm(this);
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

            comboBoxMultiSel.DisplayMember = "Key";
            comboBoxMultiSel.ValueMember = "Value";
            comboBoxMultiSel.Items.Clear();
            comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>("默认", TongbaoSelectMode.Default));
            comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>("随机", TongbaoSelectMode.Random));
            foreach (var id in Helper.GetUpgradeSelectTongbaoIds())
            {
                TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
                if (config == null) continue;
                comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>(config.Name, TongbaoSelectMode.Specific, config));
            }
            comboBoxMultiSel.SelectedIndex = 0;

            checkBoxFortune.Checked = false;

            panelLockedList.Controls.Clear();
            panelLockedList.Controls.Add(mIconGrid);
            mIconGrid.CellSize = 22;
            mIconGrid.Spacing = -4;
            mIconGrid.Width = panelLockedList.Width;
            mIconGrid.Height = panelLockedList.Height;
            mIconGrid.Click -= iconGridControl_Click;
            mIconGrid.Click += iconGridControl_Click;
            UpdateLockedListView();

            Helper.SetupResNumberic(mPlayerData, numHp, ResType.LifePoint);
            Helper.SetupResNumberic(mPlayerData, numIngots, ResType.OriginiumIngots);
            Helper.SetupResNumberic(mPlayerData, numCoupon, ResType.Coupon);
            Helper.SetupResNumberic(mPlayerData, numCandle, ResType.Candles);
            Helper.SetupResNumberic(mPlayerData, numShield, ResType.Shield);
            Helper.SetupResNumberic(mPlayerData, numHope, ResType.Hope);

            InitTongbaoView();
            RuleTreeViewController.InitRuleTreeView();
            RuleTreeViewController.BindButtons(btnAdd, btnRemove, btnUp, btnDown);

            mRecordForm.SetClearCallback(ClearRecord);
        }

        private void InitTongbaoView()
        {
            // InitImageList
            listViewTongbao.LargeImageList?.Dispose();
            //listViewTongbao.SmallImageList?.Dispose();
            listViewTongbao.LargeImageList = Helper.CreateTongbaoImageList(new Size(54, 54));
            //listViewTongbao.SmallImageList = Helper.CreateTongbaoImageList(new Size(24, 24));

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

            void AppendResValue(ResType resType, NumericUpDown numeric)
            {
                int value = mPlayerData.GetResValue(resType);
                sb.Append(Define.GetResName(resType)).Append(": ").Append(value);
                if (numeric != null)
                {
                    int beforeValue = (int)numeric.Value;
                    int deltaValue = value - beforeValue;
                    sb.Append('(');
                    if (deltaValue > 0) sb.Append('+');
                    sb.Append(deltaValue).Append(")");
                }
                sb.AppendLine();
            }

            AppendResValue(ResType.LifePoint, numHp);
            AppendResValue(ResType.OriginiumIngots, numIngots);
            AppendResValue(ResType.Coupon, numCoupon);
            AppendResValue(ResType.Candles, numCandle);
            AppendResValue(ResType.PrimalFarmingCandles, null);
            AppendResValue(ResType.Shield, numShield);
            AppendResValue(ResType.Hope, numHope);

            lblRes.Text = sb.ToString();
            sb.Clear();

            int slotIndex = mSelectedTongbaoSlotIndex;
            Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);

            if (tongbao == null)
            {
                sb.AppendLine("选中通宝:");
            }
            else
            {
                sb.Append("选中通宝:[")
                  .Append(slotIndex + 1)
                  .Append(']');
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                sb.AppendLine();
            }

            sb.Append("当前交换次数: ")
              .Append(mPlayerData.ExchangeCount)
              .AppendLine();

            sb.Append("下次交换消耗生命值: ")
              .Append(mPlayerData.NextExchangeCostLifePoint);

            lblCurrent.Text = sb.ToString();
            sb.Clear();
        }

        private void UpdateLockedListView()
        {
            mTempTongbaoImages.Clear();
            foreach (var id in mPlayerData.LockedTongbaoList)
            {
                Image image = Helper.GetTongbaoImage(id);
                mTempTongbaoImages.Add(image);
            }
            mIconGrid.SetIcons(mTempTongbaoImages);
        }

        private void UpdateAsyncSimulateView(bool asyncSimulating)
        {
            bool enabled = !asyncSimulating;
            foreach (var item in mSimulatingDisableControls)
            {
                item.Enabled = enabled;
            }
            btnSimulation.Text = asyncSimulating ? "停止模拟" : "开始模拟";
        }

        private void OnSelectNewRandomTongbao(int id, int slotIndex)
        {
            mCanRevertPlayerData = false;
            Tongbao tongbao = mPlayerData.CreateTongbao(id, mRandom);
            if (tongbao != null)
            {
                mPlayerData.InsertTongbao(tongbao, slotIndex);
            }
            else
            {
                mPlayerData.RemoveTongbaoAt(slotIndex);
            }
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private void OnSelectNewCustomTongbao(int id, int slotIndex,
            ResType randomResType = ResType.None, int randomResCount = 0)
        {
            mCanRevertPlayerData = false;
            Tongbao tongbao = mPlayerData.CreateTongbao(id);
            if (tongbao != null)
            {
                tongbao.ApplyRandomRes(randomResType, randomResCount);
                mPlayerData.InsertTongbao(tongbao, slotIndex);
            }
            else
            {
                mPlayerData.RemoveTongbaoAt(slotIndex);
            }
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private void SelectTongbaoSlot(int slotIndex)
        {
            mSelectedTongbaoSlotIndex = slotIndex;
            UpdateView();
        }

        private void ExchangeOnce(bool force = false)
        {
            if (mCanRevertPlayerData)
            {
                ClearRecord();
            }

            mCanRevertPlayerData = false;
            mPrintDataCollector.RecordEachExchange = true; //单次交换固定打印
            mPrintDataCollector.InitSimulateStep(0);
            mTongbaoSelector.TongbaoSelectMode = TongbaoSelectMode.Dialog; //弹窗询问
            int slotIndex = mSelectedTongbaoSlotIndex;
            mPrintDataCollector?.OnExchangeStepBegin(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData));
            if (!mPlayerData.ExchangeTongbao(slotIndex, force))
            {
                Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);
                if (tongbao == null)
                {
                    mPrintDataCollector?.OnExchangeStepEnd(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData), ExchangeStepResult.SelectedEmpty);
                    MessageBox.Show("交换失败，请先选中一个通宝。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!tongbao.CanExchange())
                {
                    mPrintDataCollector?.OnExchangeStepEnd(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData), ExchangeStepResult.TongbaoUnexchangeable);
                    MessageBox.Show($"交换失败，选中通宝[{Helper.GetTongbaoFullName(tongbao.Id)}]无法交换。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!force && !mPlayerData.HasEnoughExchangeLife)
                {
                    mPrintDataCollector?.OnExchangeStepEnd(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData), ExchangeStepResult.LifePointNotEnough);
                    MessageBox.Show($"交换失败，当前生命值不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                mPrintDataCollector?.OnExchangeStepEnd(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData), ExchangeStepResult.UnknownError);
                MessageBox.Show("交换失败，请检查当前配置和状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mPrintDataCollector?.OnExchangeStepEnd(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData), ExchangeStepResult.Success);

            var sb = mTempStringBuilder;
            sb.Clear();
            sb.Append('(')
              .Append(mPlayerData.ExchangeCount)
              .Append(") ")
              .AppendLine(mPrintDataCollector.LastExchangeResult);

            mRecordForm.AppendText(sb.ToString());

            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private async Task ExchangeSimulate(SimulationType type)
        {
            if (checkBoxAutoRevert.Checked)
            {
                ResetPlayerData();
            }
            mCanRevertPlayerData = true;
            mPrintDataCollector.RecordEachExchange = checkBoxEnableRecord.Checked;
            mExchangeDataCollector.RecordEachExchange = checkBoxEnableRecord.Checked;
            mTongbaoSelector.TongbaoSelectMode = TongbaoSelectMode.Default;
            if (comboBoxMultiSel.SelectedItem is ComboBoxItem<TongbaoSelectMode> cbItem)
            {
                if (cbItem.Value != TongbaoSelectMode.Specific)
                {
                    mTongbaoSelector.TongbaoSelectMode = cbItem.Value;
                }
                else if (cbItem.Args != null && cbItem.Args.Length > 0 && cbItem.Args[0] is TongbaoConfig config)
                {
                    mTongbaoSelector.TongbaoSelectMode = cbItem.Value;
                    mTongbaoSelector.SpecificTongbaoId = config.Id;
                }
            }

            SimulationOptions options = new SimulationOptions
            {
                SimulationType = type,
                TotalSimulationCount = (int)numSimCnt.Value,
                MinimumLifePoint = (int)numMinHp.Value,
                ExchangeSlotIndex = mSelectedTongbaoSlotIndex,
                RuleController = RuleTreeViewController,
                UseMultiThreadOptimize = checkBoxOptimize.Checked,
            };

            //mSimulationController.Simulate(options);

            string simulationName = SimulationDefine.GetSimulationName(options.SimulationType);
            int total = options.TotalSimulationCount;
            UpdateAsyncSimulateView(true);
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = total;
            toolStripProgressBar1.Value = 0;

            int lastUpdateTick = 0;
            Progress<int> progress = new Progress<int>(value =>
            {
                int now = Environment.TickCount;
                if (lastUpdateTick != 0 && now - lastUpdateTick < 50 && value != total)
                {
                    return;
                }

                lastUpdateTick = now;
                float percent = value * 100f / total;
                toolStripProgressBar1.Value = value;
                toolStripStatusLabel1.Text = $"正在进行[{simulationName}]模拟: {value}/{total} ({percent:F1}%)";
            });

            await mSimulationController.SimulateAsync(options, progress);

            if (mRecordForm.Visible)
            {
                mRecordForm.SetStringBuilderText(mPrintDataCollector.OutputResultSB);
                mOutputResultChanged = false;
            }
            else
            {
                mOutputResultChanged = true;
            }

            UpdateAllTongbaoView();
            UpdateView();
            UpdateAsyncSimulateView(false);
            GC.Collect();

            MessageBox.Show(mStatisticDataCollector.GetOutputResult(), "模拟期望", MessageBoxButtons.OK, MessageBoxIcon.Information);

            toolStripStatusLabel1.Text = "单击选中通宝，双击添加/更改通宝";
            toolStripProgressBar1.Value = 0;
        }

        private void ResetPlayerData()
        {
            if (mCanRevertPlayerData)
            {
                mSimulationController.RevertPlayerData();
                mCanRevertPlayerData = false;
            }
            mPlayerData.ExchangeCount = 0;

            mTempResValues.Clear();
            mTempResValues.Add(ResType.LifePoint, (int)numHp.Value);
            mTempResValues.Add(ResType.OriginiumIngots, (int)numIngots.Value);
            mTempResValues.Add(ResType.Coupon, (int)numCoupon.Value);
            mTempResValues.Add(ResType.Candles, (int)numCandle.Value);
            mTempResValues.Add(ResType.Shield, (int)numShield.Value);
            mTempResValues.Add(ResType.Hope, (int)numHope.Value);
            mPlayerData.InitResValues(mTempResValues);
        }

        private void ClearRecord()
        {
            mPrintDataCollector.ClearData();
            mRecordForm.ClearText();
            mOutputResultChanged = false;
            GC.Collect();
        }

        private void SetRandomTongbao(int slotIndex)
        {
            // 测试，随机添加不重复通宝
            var list = new List<int>();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                int tongbaoId = item.Key;
                if (!mPlayerData.IsTongbaoExist(tongbaoId))
                {
                    list.Add(item.Key);
                }
            }

            int targetId = -1;
            if (list.Count > 0)
            {
                int random = mRandom.Next(0, list.Count);
                targetId = list[random];
            }

            if (targetId > 0)
            {
                OnSelectNewRandomTongbao(targetId, slotIndex);
            }
        }

        private void SetNumbericValue(NumericUpDown numeric, decimal value)
        {
            if (numeric != null)
            {
                if (value > numeric.Maximum) value = numeric.Maximum;
                if (value < numeric.Minimum) value = numeric.Minimum;
                numeric.Value = value;
            }
        }

        private void comboBoxSquad_SelectedIndexChanged(object sender, EventArgs e)
        {
            mCanRevertPlayerData = false;
            if (comboBoxSquad.SelectedItem is ComboBoxItem<SquadType> item)
            {
                mPlayerData.SetSquadType(item.Value);
                InitTongbaoSlot();
            }
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

                SelectorForm selectorForm = new SelectorForm(SelectMode.SingleSelectWithComboBox);
                Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);
                if (tongbao != null)
                {
                    selectorForm.SetSelectedIds(new List<int> { tongbao.Id });
                }
                if (selectorForm.ShowDialog() == DialogResult.OK)
                {
                    if (selectorForm.SelectedRandomRes != null)
                    {
                        ResType resType = selectorForm.SelectedRandomRes.ResType;
                        int resCount = selectorForm.SelectedRandomRes.ResCount;
                        OnSelectNewCustomTongbao(selectorForm.SelectedId, slotIndex, resType, resCount);
                    }
                    else
                    {
                        OnSelectNewRandomTongbao(selectorForm.SelectedId, slotIndex);
                    }
                }

                // 测试，随机添加通宝
                //SetRandomTongbao(slotIndex);
            }
        }

        private void btnExchange_Click(object sender, EventArgs e)
        {
            ExchangeOnce();
        }

        private void btnSimulation_Click(object sender, EventArgs e)
        {
            if (mSimulationController.IsAsyncSimulating)
            {
                mSimulationController.CancelSimulate();
                return;
            }

            SimulationType simType = default;
            if (comboBoxSimMode.SelectedItem is ComboBoxItem<SimulationType> item)
            {
                simType = item.Value;
            }
            _ = ExchangeSimulate(simType);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetPlayerData();
            //ClearRecord();
            UpdateAllTongbaoView();
            UpdateView();
        }

        private void linkLblSync_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int hp = mPlayerData.GetResValue(ResType.LifePoint);
            int ingots = mPlayerData.GetResValue(ResType.OriginiumIngots);
            int coupon = mPlayerData.GetResValue(ResType.Coupon);
            int candle = mPlayerData.GetResValue(ResType.Candles);
            int shield = mPlayerData.GetResValue(ResType.Shield);
            int hope = mPlayerData.GetResValue(ResType.Hope);

            SetNumbericValue(numHp, hp);
            SetNumbericValue(numIngots, ingots);
            SetNumbericValue(numCoupon, coupon);
            SetNumbericValue(numCandle, candle);
            SetNumbericValue(numShield, shield);
            SetNumbericValue(numHope, hope);
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            mRecordForm.Show();
            mRecordForm.WindowState = FormWindowState.Normal;
            mRecordForm.Focus();
            if (mOutputResultChanged)
            {
                mRecordForm.SetStringBuilderText(mPrintDataCollector.OutputResultSB);
                mOutputResultChanged = false;
            }
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                SetRandomTongbao(i);
            }
        }

        private void btnRandomEmpty_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                if (mPlayerData.GetTongbao(i) == null)
                {
                    SetRandomTongbao(i);
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            mCanRevertPlayerData = false;
            mPlayerData.ClearTongbao();
            UpdateAllTongbaoView();
        }

        private void iconGridControl_Click(object sender, EventArgs e)
        {
            SelectorForm selectorForm = new SelectorForm(SelectMode.MultiSelect);
            selectorForm.SetSelectedIds(mPlayerData.LockedTongbaoList);
            if (selectorForm.ShowDialog() == DialogResult.OK)
            {
                mPlayerData.LockedTongbaoList.Clear();
                mPlayerData.LockedTongbaoList.AddRange(selectorForm.SelectedIds);
                UpdateLockedListView();
            }
        }

        private void comboBoxSimMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            SimulationType simType = default;
            if (comboBoxSimMode.SelectedItem is ComboBoxItem<SimulationType> item)
            {
                simType = item.Value;
            }

            numMinHp.Enabled = simType != SimulationType.ExpectationTongbao;
        }
    }
}
