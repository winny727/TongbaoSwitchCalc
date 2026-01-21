using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.DataModel.Simulation;
using TongbaoSwitchCalc.Impl;
using TongbaoSwitchCalc.Impl.Simulation;
using TongbaoSwitchCalc.Impl.View;
using TongbaoSwitchCalc.View;

namespace TongbaoSwitchCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private RandomGenerator mRandom;
        private TongbaoSelector mTongbaoSelector;
        private SwitchSimulator mSwitchSimulator;
        private PrintDataCollector mPrintDataCollector;
        private StatisticDataCollector mStatisticDataCollector;
        private CompositeDataCollector mCompositeDataCollector;

        private int mSelectedTongbaoSlotIndex = -1;
        private bool mCanRevertPlayerData = false;

        private IconGridControl mIconGrid;
        private RuleTreeViewController RuleTreeViewController;
        private string mOutputResult;
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
            mRandom = new LockThreadSafeRandomGenerator(); //2.3s
            //mRandom = new ThreadSafeRandomGenerator(); //2.46s
            mTongbaoSelector = new TongbaoSelector(mRandom);
            mPlayerData = new PlayerData(mTongbaoSelector, mRandom);

            InitSimulationData();
            InitPlayerData();
        }

        private void InitSimulationData()
        {
            mPrintDataCollector = new PrintDataCollector();
            mStatisticDataCollector = new StatisticDataCollector();
            mCompositeDataCollector = new CompositeDataCollector();
            mCompositeDataCollector.AddDataCollector(mPrintDataCollector);
            mCompositeDataCollector.AddDataCollector(mStatisticDataCollector);

            // 交换耗时测试（16线程）：100w开记录/1000w次开记录/100w次不开记录/1000w次不开记录
            //mSwitchSimulator = new SwitchSimulator(mPlayerData, mCompositeDataCollector); //4.2s，45.9s，3.1s，33.4s
            //mSwitchSimulator = new SwitchSimulator(mPlayerData, new LockThreadSafeDataCollector() { RecordEverySwitch = false }); //100w次就已经21.2s了，锁麻了；不记录每次交换：3.2s，32.1s
            //mSwitchSimulator = new SwitchSimulator(mPlayerData, new ConcurrentThreadSafeDataCollector() { RecordEverySwitch = false }); //7.9s，ConcurrentDict GC很多；不记录每次交换：3.1s，33.3s
            mSwitchSimulator = new SwitchSimulator(mPlayerData, new WrapperThreadSafeDataCollector(mCompositeDataCollector)); //5.5s，55.9s，2.5s，22.8s；若开记录GC很多

            //线程数测试（16核CPU，1000w次无记录WarpperThreadSafeDataCollector）：单线程（主线程）32.5s，单线程（非主线程）35.1s，双线程21.9s，四线程18.2s，八线程19.3s，15线程26.3s，16线程24.3s
            //结论：线程数：处理器数/4

            //4线程情况下，若不开记录最优为ConcurrentThreadSafeDataCollector 1.6s
            //不开记录WarpperThreadSafeDataCollector 1.8s，开记录最优为WarpperThreadSafeDataCollector 4.0s
            //结论，4线程WarpperThreadSafeDataCollector

            //备注：LockThreadSafeDataCollector和ConcurrentThreadSafeDataCollector还未实现最终的数据整理打印逻辑
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
            UpdateMultiThreadOptimize();

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
            groupBox1.Enabled = enabled;
            groupBox2.Enabled = enabled;
            groupBox3.Enabled = enabled;
            groupBox5.Enabled = enabled;
            listViewTongbao.Enabled = enabled;
            btnRandom.Enabled = enabled;
            btnClear.Enabled = enabled;
            checkBoxOptimize.Enabled = enabled;
            checkBoxAutoRevert.Enabled = enabled;
            checkBoxEnableRecord.Enabled = enabled;
            btnSwitch.Enabled = enabled;
            btnReset.Enabled = enabled;
            btnSimulation.Text = asyncSimulating ? "停止模拟" : "开始模拟";
        }

        private void UpdateMultiThreadOptimize()
        {
            if (checkBoxOptimize.Checked)
            {
                mSwitchSimulator.SetDataCollector(new WrapperThreadSafeDataCollector(mCompositeDataCollector));
            }
            else
            {
                mSwitchSimulator.SetDataCollector(mCompositeDataCollector);
            }
        }

        private void OnSelectNewRandomTongbao(int id, int slotIndex)
        {
            mCanRevertPlayerData = false;
            Tongbao tongbao = Tongbao.CreateTongbao(id, mRandom);
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
            Tongbao tongbao = Tongbao.CreateTongbao(id);
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

        private void SwitchOnce(bool force = false)
        {
            if (mCanRevertPlayerData)
            {
                ClearRecord();
            }

            mCanRevertPlayerData = false;
            mPrintDataCollector.RecordEverySwitch = true; //单次交换固定打印
            mPrintDataCollector.InitSimulateStep(0);
            mTongbaoSelector.TongbaoSelectMode = TongbaoSelectMode.Dialog; //弹窗询问
            int slotIndex = mSelectedTongbaoSlotIndex;
            mPrintDataCollector?.OnSwitchStepBegin(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData));
            if (!mPlayerData.SwitchTongbao(slotIndex, force))
            {
                Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);
                if (tongbao == null)
                {
                    mPrintDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.SelectedEmpty);
                    MessageBox.Show("交换失败，请先选中一个通宝。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!tongbao.CanSwitch())
                {
                    mPrintDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.TongbaoCanNotSwitch);
                    MessageBox.Show($"交换失败，选中通宝[{Helper.GetTongbaoFullName(tongbao.Id)}]无法交换。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!force && !mPlayerData.HasEnoughSwitchLife)
                {
                    mPrintDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.LifePointNotEnough);
                    MessageBox.Show($"交换失败，当前生命值不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                mPrintDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.UnknownError);
                MessageBox.Show("交换失败，请检查当前配置和状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = mTempStringBuilder;
            sb.Clear();

            sb.Append('(')
              .Append(mPlayerData.SwitchCount)
              .Append(") ")
              .AppendLine(mPrintDataCollector.LastSwitchResult);

            mPrintDataCollector?.OnSwitchStepEnd(new SimulateContext(0, mPlayerData.SwitchCount, slotIndex, mPlayerData), SwitchStepResult.Success);
            mOutputResult += sb.ToString();
            mOutputResultChanged = true;
            UpdateTongbaoView(slotIndex);
            UpdateView();
        }

        private async void SwitchSimulate(SimulationType type)
        {
            if (checkBoxAutoRevert.Checked)
            {
                ResetPlayerData();
            }
            mCanRevertPlayerData = true;
            mPrintDataCollector.RecordEverySwitch = checkBoxEnableRecord.Checked;
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
            mSwitchSimulator.SimulationType = type;
            mSwitchSimulator.TotalSimulationCount = (int)numSimCnt.Value;
            mSwitchSimulator.MinimumLifePoint = (int)numMinHp.Value;
            mSwitchSimulator.NextSwitchSlotIndex = mSelectedTongbaoSlotIndex;

            // ApplyRule
            mSwitchSimulator.SlotIndexPriority.Clear();
            mSwitchSimulator.TargetTongbaoIds.Clear();
            mSwitchSimulator.ExpectedTongbaoId = -1;
            RuleTreeViewController.ApplySimulationRule(mSwitchSimulator);

            //mSwitchSimulator.Simulate();

            string simulationName = SimulationDefine.GetSimulationName(mSwitchSimulator.SimulationType);
            int total = mSwitchSimulator.TotalSimulationCount;
            UpdateAsyncSimulateView(true);
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = mSwitchSimulator.TotalSimulationCount;
            toolStripProgressBar1.Value = 0;
            int lastPermille = -1; // 0.1% = 1‰
            Progress<int> progress = new Progress<int>((value) =>
            {
                int permille = value * 1000 / total; // 千分比
                if (permille == lastPermille)
                {
                    return;
                }

                lastPermille = permille;

                float percent = permille / 10f;
                toolStripProgressBar1.Value = value;
                toolStripStatusLabel1.Text = $"正在进行[{simulationName}]模拟: {value}/{total} ({percent:F1}%)";
            });
            await mSwitchSimulator.SimulateAsync(progress);

            mOutputResult = mPrintDataCollector.OutputResult;
            mOutputResultChanged = true;
            UpdateAllTongbaoView();
            UpdateView();
            UpdateAsyncSimulateView(false);

            MessageBox.Show(mStatisticDataCollector.GetOutputResult(), "模拟期望", MessageBoxButtons.OK, MessageBoxIcon.Information);

            toolStripStatusLabel1.Text = "单击选中通宝，双击添加/更改通宝";
            toolStripProgressBar1.Value = 0;
        }

        private void ResetPlayerData()
        {
            if (mCanRevertPlayerData)
            {
                mSwitchSimulator.RevertPlayerData();
                mCanRevertPlayerData = false;
            }
            mPlayerData.SwitchCount = 0;

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
            mOutputResult = string.Empty;
            mRecordForm.Content = string.Empty;
            mOutputResultChanged = false;
            GC.Collect();
        }

        private void FillRandomTongbao()
        {
            for (int i = 0; i < mPlayerData.TongbaoBox.Length; i++)
            {
                SetRandomTongbao(i);
            }
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

        private void btnSwitch_Click(object sender, EventArgs e)
        {
            SwitchOnce();
        }

        private void btnSimulation_Click(object sender, EventArgs e)
        {
            if (mSwitchSimulator.IsAsyncSimulating)
            {
                mSwitchSimulator.CancelSimulate();
                return;
            }

            SimulationType simType = default;
            if (comboBoxSimMode.SelectedItem is ComboBoxItem<SimulationType> item)
            {
                simType = item.Value;
            }
            SwitchSimulate(simType);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetPlayerData();
            ClearRecord();
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
            if (mOutputResultChanged)
            {
                mRecordForm.Content = mOutputResult ?? string.Empty;
                mOutputResultChanged = false;
            }
            mRecordForm.Show();
            mRecordForm.WindowState = FormWindowState.Normal;
            mRecordForm.Focus();
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            FillRandomTongbao();
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

        private void checkBoxOptimize_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMultiThreadOptimize();
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
