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
using TongbaoExchangeCalc.Undo;
using TongbaoExchangeCalc.Undo.Commands;
using TongbaoExchangeCalc.View;

namespace TongbaoExchangeCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private RandomGenerator mRandom;
        private TongbaoSelector mTongbaoSelector;
        private SimulationController mSimulationController;
        private ExchangeDataParser mExchangeDataParser;

        private PrintDataCollector mPrintDataCollector;
        private ExchangeDataCollector mExchangeDataCollector;
        //private StatisticDataCollector mStatisticDataCollector;
        //private CompositeDataCollector mCompositeDataCollector;

        private int mSelectedSlotIndex = -1;
        private bool mCanRevertPlayerData = false; // 标记模拟后需要重置PlayerData
        private string mCurrentFilePath = string.Empty;

        private readonly List<Control> mSimulatingDisableControls = new List<Control>();
        private IconGridControl mLockIconGrid;
        private IconGridControl mBoxIconGrid;
        private RuleTreeViewController mRuleTreeViewController;
        private bool mOutputResultChanged = false;
        private RecordForm mRecordForm;
        private readonly StringBuilder mTempStringBuilder = new StringBuilder();

        private readonly List<Image> mTempTongbaoImages = new List<Image>();
        private readonly Dictionary<ResType, int> mTempResValues = new Dictionary<ResType, int>();
        private readonly List<Tongbao> mTempRecordTongbaos = new List<Tongbao>();

        #region Undo/Redo UserCommand Collector
        private readonly struct ScopeUserCommandCollector : IDisposable
        {
            private readonly ScopePlayerDataCommand mSetPlayerDataCommand;
            private readonly ScopeOnSetValueCommand<bool> mSetCanRevertPlayerDataCommand;
            private readonly ScopeOnSetValueCommand<string> mSetCurrentFilePath;

            public ScopeUserCommandCollector(
                ScopePlayerDataCommand setPlayerDataCommand,
                ScopeOnSetValueCommand<bool> setCanRevertPlayerDataCommand,
                ScopeOnSetValueCommand<string> setCurrentFilePath
                )
            {
                mSetPlayerDataCommand = setPlayerDataCommand;
                mSetCanRevertPlayerDataCommand = setCanRevertPlayerDataCommand;
                mSetCurrentFilePath = setCurrentFilePath;
            }

            public void Dispose()
            {
                UndoCommandMgr.Instance.BeginMerge();
                mSetPlayerDataCommand?.Dispose();
                mSetCanRevertPlayerDataCommand?.Dispose();
                mSetCurrentFilePath?.Dispose();
                UndoCommandMgr.Instance.EndMerge();
            }
        }

        private ScopeUserCommandCollector CollectScopeUserCommand(
            bool setPlayerData = false,
            bool setCanRevert = false,
            bool setFilePath = false)
        {
            return new ScopeUserCommandCollector
            (
                setPlayerData ? new ScopePlayerDataCommand(mPlayerData) : null,
                setCanRevert ? new ScopeOnSetValueCommand<bool>(() => mCanRevertPlayerData, value => mCanRevertPlayerData = value, "mCanRevertPlayerData") : null,
                setFilePath ? new ScopeOnSetValueCommand<string>(() => mCurrentFilePath, value => mCurrentFilePath = value, "mCurrentFilePath") : null
            );
        }
        #endregion

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

            //DebugTestDataModel();
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
            //mPrintDataCollector = new PrintDataCollector(2000);
            //mExchangeDataCollector = new ExchangeDataCollector(2000);

            // 旧逻辑，需要同时使用多个Collector的时候就用CompositeDataCollector合在一起用
            //mStatisticDataCollector = new StatisticDataCollector();
            //mCompositeDataCollector = new CompositeDataCollector();
            //mCompositeDataCollector.AddDataCollector(mPrintDataCollector); // 简单文本输出
            //mCompositeDataCollector.AddDataCollector(mExchangeDataCollector); // 详细数据收集
            //mCompositeDataCollector.AddDataCollector(mStatisticDataCollector); // 数据统计
            //mSimulationController = new SimulationController(mPlayerData, new SimulationTimer(), mCompositeDataCollector);

            // 新版逻辑改为先用ExchangeDataCollector收集数据再用ExchangeDataParser解析为文本结果
            mPrintDataCollector = new PrintDataCollector(); // 单次交换的简单文本输出还是用原来的PrintDataCollector
            mExchangeDataCollector = new ExchangeDataCollector(2000);
            mSimulationController = new SimulationController(mPlayerData, new SimulationTimer(), mExchangeDataCollector);
            mExchangeDataParser = new ExchangeDataParser(mExchangeDataCollector, mSimulationController.ExchangeSimulator);
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

        private void InitView()
        {
            mSimulatingDisableControls.Clear();
            mSimulatingDisableControls.AddRange(new Control[]
            {
                tabPage1, tabPage2,
                listViewTongbao, btnRandom, btnRandomEmpty, btnClear,
                checkBoxOptimize, checkBoxAutoRevert, checkBoxEnableRecord,
                btnExchange, btnReset, btnLoadBox, btnSaveBox, btnRecordBox, btnResetBox,
            });

            mLockIconGrid = new IconGridControl();
            mBoxIconGrid = new IconGridControl();
            mRuleTreeViewController = new RuleTreeViewController(treeViewRule, mPlayerData);
            mRecordForm = new RecordForm(this);
            Helper.InitResources();

            Helper.SetupEnumComboBox<SquadType>(comboBoxSquad, (type) => Define.GetSquadName(type));
            Helper.SetupEnumComboBox<SimulationType>(comboBoxSimType, (type) => SimulationDefine.GetSimulationName(type));

            Helper.SetupComboBox(comboBoxMultiSel, () =>
            {
                comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>("默认", TongbaoSelectMode.Default));
                comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>("随机", TongbaoSelectMode.Random));
                foreach (var id in Helper.GetUpgradeSelectTongbaoIds())
                {
                    TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
                    if (config != null)
                    {
                        string name = Helper.GetTongbaoFullName(id);
                        comboBoxMultiSel.Items.Add(new ComboBoxItem<TongbaoSelectMode>(name, TongbaoSelectMode.Specific, config));
                    }
                }
            });

            checkBoxFortune.Checked = false;

            label11.Enabled = checkBoxEnableRecord.Checked; // N次交换后省略记录
            numMaxRecord.Enabled = checkBoxEnableRecord.Checked;

            Helper.SetupIconGridControl(panelLockedList, mLockIconGrid);
            mLockIconGrid.Click -= lockIconGridControl_Click;
            mLockIconGrid.Click += lockIconGridControl_Click;
            UpdateLockedListView();

            Helper.SetupIconGridControl(panelRecordBox, mBoxIconGrid);
            UpdateRecordBoxView();

            Helper.SetupResNumeric(mPlayerData, numHp, ResType.LifePoint, UpdateView);
            Helper.SetupResNumeric(mPlayerData, numIngots, ResType.OriginiumIngots, UpdateView);
            Helper.SetupResNumeric(mPlayerData, numCoupon, ResType.Coupon, UpdateView);
            Helper.SetupResNumeric(mPlayerData, numCandle, ResType.Candles, UpdateView);
            Helper.SetupResNumeric(mPlayerData, numShield, ResType.Shield, UpdateView);
            Helper.SetupResNumeric(mPlayerData, numHope, ResType.Hope, UpdateView);

            // PlayerData相关的由PlayerDataCommand管理，由UpdateUndoView更新
            //UndoHelper.SetupComboBoxUndo(comboBoxSquad);
            //UndoHelper.SetupCheckBoxUndo(checkBoxFortune);

            UndoHelper.SetupNumericUndo(numHp);
            UndoHelper.SetupNumericUndo(numIngots);
            UndoHelper.SetupNumericUndo(numCoupon);
            UndoHelper.SetupNumericUndo(numCandle);
            UndoHelper.SetupNumericUndo(numShield);
            UndoHelper.SetupNumericUndo(numHope);
            UndoHelper.SetupComboBoxUndo(comboBoxSimType);
            UndoHelper.SetupNumericUndo(numSimCnt);
            UndoHelper.SetupNumericUndo(numMinHp);
            UndoHelper.SetupComboBoxUndo(comboBoxMultiSel);
            UndoHelper.SetupCheckBoxUndo(checkBoxOptimize);
            UndoHelper.SetupCheckBoxUndo(checkBoxAutoRevert);
            UndoHelper.SetupCheckBoxUndo(checkBoxLogExchange);
            UndoHelper.SetupCheckBoxUndo(checkBoxEnableRecord);
            UndoHelper.SetupNumericUndo(numMaxRecord);

            InitTongbaoView();
            mRuleTreeViewController.InitRuleTreeView();
            mRuleTreeViewController.BindButtons(btnAdd, btnRemove, btnUp, btnDown);

            mRecordForm.SetClearCallback(ClearRecord);

            UndoCommandMgr.Instance.OnCommandChanged -= UpdateUndoState;
            UndoCommandMgr.Instance.OnCommandChanged += UpdateUndoState;
            UpdateUndoState();
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
                var randomEff = tongbao.RandomEff;
                if (randomEff != null && randomEff.ResType != ResType.None)
                {
                    sb.AppendLine()
                      .Append('(')
                      .Append(randomEff.Name)
                      .Append(": ")
                      .Append(Define.GetResName(randomEff.ResType))
                      .Append('+')
                      .Append(randomEff.ResCount)
                      .Append(')');
                }
                item.Text = sb.ToString();
                item.ImageKey = tongbao.Id.ToString();
                item.ToolTipText = Helper.GetTongbaoToolTip(mPlayerData, tongbao);
            }
            else
            {
                sb.Append('[')
                  .Append(slotIndex + 1)
                  .Append("] (空)");
                item.Text = sb.ToString();
                item.ImageKey = "Empty";
                item.ToolTipText = Helper.GetTongbaoToolTip(mPlayerData, null);
            }
            sb.Clear();
        }

        private void UpdateAllTongbaoToolTip()
        {
            for (int i = 0; i < listViewTongbao.Items.Count; i++)
            {
                ListViewItem item = listViewTongbao.Items[i];
                Tongbao tongbao = mPlayerData.GetTongbao(i);
                item.ToolTipText = Helper.GetTongbaoToolTip(mPlayerData, tongbao);
            }
        }

        private void UpdateView()
        {
            var sb = mTempStringBuilder;
            sb.Clear();

            StringBuilder AppendResValue(ResType type, NumericUpDown numeric)
            {
                int value = mPlayerData.GetResValue(type);
                sb.Append(Define.GetResName(type)).Append(": ").Append(value);
                if (numeric != null)
                {
                    int beforeValue = (int)numeric.Value;
                    int deltaValue = value - beforeValue;
                    sb.Append(" (");
                    if (deltaValue > 0) sb.Append('+');
                    sb.Append(deltaValue).Append(")");
                }
                return sb;
            }

            //sb.AppendLine("当前实时资源:").AppendLine();
            AppendResValue(ResType.LifePoint, numHp).AppendLine();
            AppendResValue(ResType.OriginiumIngots, numIngots).AppendLine();
            AppendResValue(ResType.Coupon, numCoupon).AppendLine();

            AppendResValue(ResType.Candles, numCandle).Append("    [");
            AppendResValue(ResType.PrimalFarmingCandles, null).Append(']').AppendLine();

            AppendResValue(ResType.Shield, numShield).AppendLine();
            AppendResValue(ResType.Hope, numHope);

            lblRes.Text = sb.ToString();
            sb.Clear();

            int slotIndex = mSelectedSlotIndex;
            Tongbao tongbao = mPlayerData.GetTongbao(slotIndex);

            if (tongbao == null)
            {
                sb.AppendLine("选中通宝: (无)");
            }
            else
            {
                sb.Append("选中通宝: [")
                  .Append(slotIndex + 1)
                  .Append("] ");
                Helper.AppendTongbaoFullName(sb, tongbao.Id);
                sb.AppendLine();
            }

            sb.Append("交换次数: ")
              .Append(mPlayerData.ExchangeCount)
              .AppendLine();

            sb.Append("下次交换消耗生命值: ")
              .Append(mPlayerData.NextExchangeCostLifePoint);

            lblCurrent.Text = sb.ToString();
            sb.Clear();
        }

        private void UpdateUndoView()
        {
            foreach (var item in comboBoxSquad.Items)
            {
                if (item is ComboBoxItem<SquadType> squadItem && squadItem.Value == mPlayerData.SquadType)
                {
                    comboBoxSquad.SelectedItem = squadItem;
                    break;
                }
            }
            checkBoxFortune.Checked = mPlayerData.HasSpecialCondition(SpecialConditionFlag.Collectible_Fortune);
            UpdateLockedListView();

            if (mPlayerData.MaxTongbaoCount != listViewTongbao.Items.Count)
            {
                InitTongbaoSlot();
            }
            else
            {
                UpdateAllTongbaoView();
            }

            UpdateView();
            mRuleTreeViewController.UpdateRuleTreeView();
        }

        private void UpdateLockedListView()
        {
            mTempTongbaoImages.Clear();
            foreach (var id in mPlayerData.LockedTongbaoList)
            {
                Image image = Helper.GetTongbaoImage(id);
                mTempTongbaoImages.Add(image);
            }
            mLockIconGrid.SetIcons(mTempTongbaoImages);
        }

        private void UpdateRecordBoxView()
        {
            mTempTongbaoImages.Clear();
            foreach (var tongbao in mTempRecordTongbaos)
            {
                Image image = tongbao != null ? Helper.GetTongbaoImage(tongbao.Id) : null;
                mTempTongbaoImages.Add(image);
            }
            mBoxIconGrid.SetIcons(mTempTongbaoImages);
        }

        private void UpdateAsyncSimulateView(bool asyncSimulating)
        {
            bool enabled = !asyncSimulating;
            foreach (var item in mSimulatingDisableControls)
            {
                item.Enabled = enabled;
            }
            btnSimulation.Text = asyncSimulating ? "停止模拟" : "开始模拟";
            toolStripProgressBar1.Visible = asyncSimulating;
        }

        private void UpdateUndoState()
        {
            UndoToolStripMenuItem.Enabled = UndoCommandMgr.Instance.CanUndo;
            RedoToolStripMenuItem.Enabled = UndoCommandMgr.Instance.CanRedo;
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
            UpdateAllTongbaoToolTip();
            UpdateView();
        }

        private void OnSelectNewCustomTongbao(int id, int slotIndex, RandomEff randomEff = null)
        {
            mCanRevertPlayerData = false;
            Tongbao tongbao = mPlayerData.CreateTongbao(id);
            if (tongbao != null)
            {
                tongbao.ApplyRandomEff(randomEff);
                mPlayerData.InsertTongbao(tongbao, slotIndex);
            }
            else
            {
                mPlayerData.RemoveTongbaoAt(slotIndex);
            }
            UpdateTongbaoView(slotIndex);
            UpdateAllTongbaoToolTip();
            UpdateView();
        }

        private void SelectTongbaoSlot(int slotIndex)
        {
            mSelectedSlotIndex = slotIndex;
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
            int slotIndex = mSelectedSlotIndex;
            mPrintDataCollector?.OnExchangeStepBegin(new SimulateContext(0, mPlayerData.ExchangeCount, slotIndex, mPlayerData));
            mPlayerData.CheckResValue = !force;

            if (!mPlayerData.ExchangeTongbao(slotIndex))
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
                MessageBox.Show("交换失败，没有可以交换的通宝。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            UpdateAllTongbaoToolTip();
            UpdateView();
        }

        private IProgress<int> CreateProgress(string title, int total)
        {
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
                float percent = total > 0 ? value * 100f / total : 0;
                toolStripProgressBar1.Value = value;
                toolStripStatusLabel1.Text = $"{title}: {value}/{total} ({percent:F1}%)";
            });
            return progress;
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
            mPrintDataCollector.MaxExchangeRecord = (int)numMaxRecord.Value;
            mExchangeDataCollector.MaxExchangeRecord = (int)numMaxRecord.Value;
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
                //ExchangeSlotIndex = mSelectedSlotIndex,
                ExchangeSlotIndex = -1,
                RuleController = mRuleTreeViewController,
                UseMultiThreadOptimize = checkBoxOptimize.Checked,
            };

            //mSimulationController.Simulate(options); // 同步

            string simulationName = SimulationDefine.GetSimulationName(options.SimulationType);
            UpdateAsyncSimulateView(true);

            var simulateProgress = CreateProgress($"正在进行[{simulationName}]模拟", options.TotalSimulationCount);
            await mSimulationController.SimulateAsync(options, simulateProgress);

            var resultProgress = CreateProgress($"正在处理数据", mExchangeDataCollector.ExecSimulateStep);
            await mExchangeDataParser.BuildResultAsync(resultProgress);

            //DebugCompareOutputResult(); // 测试

            toolStripStatusLabel1.Text = $"[{simulationName}]模拟结束";

            if (mRecordForm.Visible)
            {
                SetRecordFormText();
            }
            else
            {
                mOutputResultChanged = true;
            }

            UpdateAllTongbaoView();
            UpdateView();
            UpdateAsyncSimulateView(false);
            GC.Collect();

            //MessageBox.Show(mStatisticDataCollector.GetOutputResult(), "模拟统计结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            MessageBox.Show($"{mExchangeDataParser.StatisticResult}\n\n(各个槽位的详细统计数据请在交换记录查看窗口查看。)", "模拟统计结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
            mExchangeDataParser.ClearData();
            mRecordForm.ClearText();
            mOutputResultChanged = false;
            GC.Collect();
        }

        private void SetRandomTongbao(int slotIndex)
        {
            // 测试，随机添加不重复/不互斥通宝
            var list = new List<int>();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                int tongbaoId = item.Key;
                TongbaoConfig config = item.Value;

                // 排除钱盒里的
                if (mPlayerData.IsTongbaoExist(tongbaoId))
                {
                    continue;
                }

                // 排除钱盒里的互斥通宝
                if (config.MutexGroup > 0)
                {
                    bool isMutexExist = false;
                    var mutexTongbaoIds = ExchangePool.GetMutexTongbaoIds(config.MutexGroup);
                    for (int j = 0; j < mutexTongbaoIds.Count; j++)
                    {
                        int mutexTongbaoId = mutexTongbaoIds[j];
                        if (mutexTongbaoId == tongbaoId)
                        {
                            continue;
                        }
                        if (mPlayerData.IsTongbaoExist(mutexTongbaoId))
                        {
                            isMutexExist = true;
                            break;
                        }
                    }
                    if (isMutexExist)
                    {
                        continue;
                    }
                }

                // 排除商店锁定的
                if (mPlayerData.IsTongbaoLocked(tongbaoId))
                {
                    continue;
                }

                list.Add(tongbaoId);
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

        private void SetNumericValue(NumericUpDown numeric, decimal value)
        {
            if (numeric != null)
            {
                if (value > numeric.Maximum) value = numeric.Maximum;
                if (value < numeric.Minimum) value = numeric.Minimum;
                numeric.Value = value;
            }
        }

        private void SetRecordFormText()
        {
            mRecordForm.ClearText();
            if (checkBoxLogExchange.Checked)
            {
                //mRecordForm.SetStringBuilderText(mPrintDataCollector.OutputResultSB);
                mRecordForm.SetStringBuilderText(mExchangeDataParser.OutputResultSB);
                mRecordForm.AppendText($"{new string('=', 64)}{Environment.NewLine}");
            }
            mRecordForm.AppendStringBuilderText(mExchangeDataParser.StatisticResultSB);
            mRecordForm.AppendText(Environment.NewLine);
            mRecordForm.AppendStringBuilderText(mExchangeDataParser.SlotStatisticResultSB);
            mOutputResultChanged = false;
        }

        #region DebugTest
        private void DebugTestDataModel()
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

        private void DebugCompareOutputResult()
        {
            // 测试代码，用于对比两种打印是否相同
            string result1 = mExchangeDataParser.OutputResult;
            string result2 = mPrintDataCollector.OutputResult;
            //string result1 = mExchangeDataParser.StatisticResult;
            //string result2 = mStatisticDataCollector.GetOutputResult();
            for (int i = 0; i < result1.Length; i++)
            {
                if (result1[i] != result2[i])
                {
                    Helper.Log(result1.Substring(i - 50, 200));
                    Helper.Log("\n\n");
                    Helper.Log(result2.Substring(i - 50, 200));
                    break;
                }
            }
        }

        #endregion

        private void comboBoxSquad_SelectedIndexChanged(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
            mCanRevertPlayerData = false;
            if (comboBoxSquad.SelectedItem is ComboBoxItem<SquadType> item)
            {
                mPlayerData.SetSquadType(item.Value);
                InitTongbaoSlot();
            }
        }

        private void checkBoxFortune_CheckedChanged(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true);
            mPlayerData.SetSpecialCondition(SpecialConditionFlag.Collectible_Fortune, checkBoxFortune.Checked);
        }

        private void checkBoxEnableRecord_CheckedChanged(object sender, EventArgs e)
        {
            label11.Enabled = checkBoxEnableRecord.Checked;
            numMaxRecord.Enabled = checkBoxEnableRecord.Checked;
        }

        private void scrollable_MouseWheel(object sender, MouseEventArgs e)
        {
            // 拦截鼠标滚轮事件，防止未聚焦时滚动
            if (!((Control)sender).Focused)
            {
                ((HandledMouseEventArgs)e).Handled = true;
            }
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
                    using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
                    if (selectorForm.SelectedRandomEff != null)
                    {
                        if (selectorForm.SelectedRandomEff.ResType == ResType.None)
                        {
                            OnSelectNewCustomTongbao(selectorForm.SelectedId, slotIndex);
                        }
                        else
                        {
                            OnSelectNewCustomTongbao(selectorForm.SelectedId, slotIndex, selectorForm.SelectedRandomEff);
                        }
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
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
            ExchangeOnce();
        }

        private async void btnSimulation_Click(object sender, EventArgs e)
        {
            if (mSimulationController.IsAsyncSimulating)
            {
                mSimulationController.CancelSimulate();
                return;
            }

            if (mExchangeDataParser.IsAsyncBuilding)
            {
                mExchangeDataParser.CancelBuild();
                return;
            }

            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);

            SimulationType simType = default;
            if (comboBoxSimType.SelectedItem is ComboBoxItem<SimulationType> item)
            {
                simType = item.Value;
            }

            await ExchangeSimulate(simType);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
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

            UndoCommandMgr.Instance.BeginMerge();
            SetNumericValue(numHp, hp);
            SetNumericValue(numIngots, ingots);
            SetNumericValue(numCoupon, coupon);
            SetNumericValue(numCandle, candle);
            SetNumericValue(numShield, shield);
            SetNumericValue(numHope, hope);
            UndoCommandMgr.Instance.EndMerge();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            mRecordForm.Show();
            mRecordForm.WindowState = FormWindowState.Normal;
            mRecordForm.Focus();
            if (mOutputResultChanged)
            {
                SetRecordFormText();
            }
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                SetRandomTongbao(i);
            }
        }

        private void btnRandomEmpty_Click(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
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
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
            mCanRevertPlayerData = false;
            mPlayerData.ClearTongbao();
            UpdateAllTongbaoView();
        }

        private void lockIconGridControl_Click(object sender, EventArgs e)
        {
            SelectorForm selectorForm = new SelectorForm(SelectMode.MultiSelect);
            selectorForm.SetSelectedIds(mPlayerData.LockedTongbaoList);
            if (selectorForm.ShowDialog() == DialogResult.OK)
            {
                using var collector = CollectScopeUserCommand(setPlayerData: true);
                mPlayerData.LockedTongbaoList.Clear();
                mPlayerData.LockedTongbaoList.AddRange(selectorForm.SelectedIds);
                UpdateLockedListView();
            }
        }

        private void comboBoxSimType_SelectedIndexChanged(object sender, EventArgs e)
        {
            SimulationType simType = default;
            if (comboBoxSimType.SelectedItem is ComboBoxItem<SimulationType> item)
            {
                simType = item.Value;
            }

            numMinHp.Enabled = simType != SimulationType.ExpectationTongbao;
        }

        private void btnLoadBox_Click(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true, setFilePath: true);

            string path = Helper.LoadTongbaoBoxData(mPlayerData, mCurrentFilePath);
            if (!string.IsNullOrEmpty(path))
            {
                mCurrentFilePath = path;
                mCanRevertPlayerData = false;
                UpdateAllTongbaoView();
                UpdateView();
            }
        }

        private void btnSaveBox_Click(object sender, EventArgs e)
        {
            string path = Helper.SaveTongbaoBoxData(mPlayerData, mCurrentFilePath);
            if (!string.IsNullOrEmpty(path))
            {
                using var collector = CollectScopeUserCommand(setFilePath: true);
                mCurrentFilePath = path;
            }
        }

        private void btnRecordBox_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < mTempRecordTongbaos.Count; i++)
            {
                mPlayerData.DestroyTongbao(mTempRecordTongbaos[i]);
            }
            mTempRecordTongbaos.Clear();
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                Tongbao tongbao = mPlayerData.GetTongbao(i);
                if (tongbao == null)
                {
                    mTempRecordTongbaos.Add(null);
                    continue;
                }

                // mTempRecordTongbaos中记录的通宝由MainForm管理销毁
                Tongbao clonedTongbao = mPlayerData.CreateTongbao(tongbao.Id);
                clonedTongbao.ApplyRandomEff(tongbao.RandomEff);
                mTempRecordTongbaos.Add(clonedTongbao);
            }
            UpdateRecordBoxView();
        }

        private void btnResetBox_Click(object sender, EventArgs e)
        {
            using var collector = CollectScopeUserCommand(setPlayerData: true, setCanRevert: true);
            mCanRevertPlayerData = false;
            mPlayerData.ClearTongbao();
            for (int i = 0; i < mTempRecordTongbaos.Count; i++)
            {
                Tongbao tongbao = mTempRecordTongbaos[i];
                if (tongbao != null)
                {
                    // Insert到PlayerData里的通宝由PlayerData管理销毁
                    // 要复制一份再Insert，不然PlayerData会把mTempRecordTongbaos里的通宝销毁
                    Tongbao clonedTongbao = mPlayerData.CreateTongbao(tongbao.Id);
                    clonedTongbao.ApplyRandomEff(tongbao.RandomEff);
                    mPlayerData.InsertTongbao(clonedTongbao, i);
                }
            }
            UpdateAllTongbaoView();
            UpdateView();
        }

        private void btnRecordBox_MouseEnter(object sender, EventArgs e)
        {
            mTempTongbaoImages.Clear();
            for (int i = 0; i < mPlayerData.MaxTongbaoCount; i++)
            {
                Tongbao tongbao = mPlayerData.GetTongbao(i);
                Image image = tongbao != null ? Helper.GetTongbaoImage(tongbao.Id) : null;
                mTempTongbaoImages.Add(image);
            }
            mBoxIconGrid.SetIcons(mTempTongbaoImages);
            panelRecordBox.Visible = true;
        }

        private void btnRecordBox_MouseLeave(object sender, EventArgs e)
        {
            panelRecordBox.Visible = false;
        }

        private void btnResetBox_MouseEnter(object sender, EventArgs e)
        {
            UpdateRecordBoxView();
            panelRecordBox.Visible = true;
        }

        private void btnResetBox_MouseLeave(object sender, EventArgs e)
        {
            panelRecordBox.Visible = false;
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoCommandMgr.Instance.Undo();
            UpdateUndoView();
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoCommandMgr.Instance.Redo();
            UpdateUndoView();
        }
    }
}
