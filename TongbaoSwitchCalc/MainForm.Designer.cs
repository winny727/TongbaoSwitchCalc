namespace TongbaoSwitchCalc
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.comboBoxSquad = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxFortune = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.panelLockedList = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.numSimCnt = new System.Windows.Forms.NumericUpDown();
            this.listViewTongbao = new System.Windows.Forms.ListView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.numShield = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.numHope = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.numCandle = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numCoupon = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numIngots = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numHp = new System.Windows.Forms.NumericUpDown();
            this.lblRes = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lblCurrent = new System.Windows.Forms.Label();
            this.btnSwitch = new System.Windows.Forms.Button();
            this.btnSimulation = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnRecord = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoRevert = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.numMinHp = new System.Windows.Forms.NumericUpDown();
            this.comboBoxSimMode = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.treeViewRule = new System.Windows.Forms.TreeView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.linkLblSync = new System.Windows.Forms.LinkLabel();
            this.checkBoxEnableRecord = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSimCnt)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numShield)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHope)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCandle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCoupon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIngots)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHp)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinHp)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxSquad
            // 
            this.comboBoxSquad.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSquad.FormattingEnabled = true;
            this.comboBoxSquad.Location = new System.Drawing.Point(44, 20);
            this.comboBoxSquad.Name = "comboBoxSquad";
            this.comboBoxSquad.Size = new System.Drawing.Size(150, 20);
            this.comboBoxSquad.TabIndex = 0;
            this.comboBoxSquad.SelectedIndexChanged += new System.EventHandler(this.comboBoxSquad_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "分队";
            // 
            // checkBoxFortune
            // 
            this.checkBoxFortune.AutoSize = true;
            this.checkBoxFortune.Location = new System.Drawing.Point(44, 46);
            this.checkBoxFortune.Name = "checkBoxFortune";
            this.checkBoxFortune.Size = new System.Drawing.Size(120, 16);
            this.checkBoxFortune.TabIndex = 1;
            this.checkBoxFortune.Text = "持有“福祸相依”";
            this.checkBoxFortune.UseVisualStyleBackColor = true;
            this.checkBoxFortune.CheckedChanged += new System.EventHandler(this.checkBoxFortune_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.panelLockedList);
            this.groupBox1.Controls.Add(this.comboBoxSquad);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.checkBoxFortune);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 122);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "基础设置";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 65);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(77, 12);
            this.label12.TabIndex = 4;
            this.label12.Text = "商店锁定通宝";
            // 
            // panelLockedList
            // 
            this.panelLockedList.BackColor = System.Drawing.SystemColors.Window;
            this.panelLockedList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelLockedList.Location = new System.Drawing.Point(6, 80);
            this.panelLockedList.Name = "panelLockedList";
            this.panelLockedList.Size = new System.Drawing.Size(188, 36);
            this.panelLockedList.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "模拟次数";
            // 
            // numSimCnt
            // 
            this.numSimCnt.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numSimCnt.Location = new System.Drawing.Point(65, 46);
            this.numSimCnt.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numSimCnt.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSimCnt.Name = "numSimCnt";
            this.numSimCnt.Size = new System.Drawing.Size(129, 21);
            this.numSimCnt.TabIndex = 9;
            this.numSimCnt.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // listViewTongbao
            // 
            this.listViewTongbao.GridLines = true;
            this.listViewTongbao.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listViewTongbao.HideSelection = false;
            this.listViewTongbao.Location = new System.Drawing.Point(12, 140);
            this.listViewTongbao.MultiSelect = false;
            this.listViewTongbao.Name = "listViewTongbao";
            this.listViewTongbao.Scrollable = false;
            this.listViewTongbao.ShowItemToolTips = true;
            this.listViewTongbao.Size = new System.Drawing.Size(500, 300);
            this.listViewTongbao.TabIndex = 5;
            this.listViewTongbao.UseCompatibleStateImageBehavior = false;
            this.listViewTongbao.ItemActivate += new System.EventHandler(this.listViewTongbao_ItemActivate);
            this.listViewTongbao.SelectedIndexChanged += new System.EventHandler(this.listViewTongbao_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.linkLblSync);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.numShield);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.numHope);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.numCandle);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.numCoupon);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.numIngots);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.numHp);
            this.groupBox2.Location = new System.Drawing.Point(424, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(230, 122);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "初始资源设置";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(129, 76);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "护盾";
            // 
            // numShield
            // 
            this.numShield.Location = new System.Drawing.Point(164, 74);
            this.numShield.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numShield.Name = "numShield";
            this.numShield.Size = new System.Drawing.Size(60, 21);
            this.numShield.TabIndex = 7;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(129, 49);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 12);
            this.label7.TabIndex = 12;
            this.label7.Text = "希望";
            // 
            // numHope
            // 
            this.numHope.Location = new System.Drawing.Point(164, 47);
            this.numHope.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numHope.Name = "numHope";
            this.numHope.Size = new System.Drawing.Size(60, 21);
            this.numHope.TabIndex = 6;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(129, 22);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(29, 12);
            this.label8.TabIndex = 10;
            this.label8.Text = "烛火";
            // 
            // numCandle
            // 
            this.numCandle.Location = new System.Drawing.Point(164, 20);
            this.numCandle.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numCandle.Name = "numCandle";
            this.numCandle.Size = new System.Drawing.Size(60, 21);
            this.numCandle.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 76);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(29, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "票券";
            // 
            // numCoupon
            // 
            this.numCoupon.Location = new System.Drawing.Point(53, 74);
            this.numCoupon.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numCoupon.Name = "numCoupon";
            this.numCoupon.Size = new System.Drawing.Size(60, 21);
            this.numCoupon.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 6;
            this.label4.Text = "源石锭";
            // 
            // numIngots
            // 
            this.numIngots.Location = new System.Drawing.Point(53, 47);
            this.numIngots.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numIngots.Name = "numIngots";
            this.numIngots.Size = new System.Drawing.Size(60, 21);
            this.numIngots.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "生命值";
            // 
            // numHp
            // 
            this.numHp.Location = new System.Drawing.Point(53, 20);
            this.numHp.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numHp.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHp.Name = "numHp";
            this.numHp.Size = new System.Drawing.Size(60, 21);
            this.numHp.TabIndex = 2;
            this.numHp.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblRes
            // 
            this.lblRes.Location = new System.Drawing.Point(6, 17);
            this.lblRes.Name = "lblRes";
            this.lblRes.Size = new System.Drawing.Size(113, 102);
            this.lblRes.TabIndex = 7;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lblRes);
            this.groupBox4.Location = new System.Drawing.Point(660, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(128, 122);
            this.groupBox4.TabIndex = 7;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "实时资源监测";
            // 
            // lblCurrent
            // 
            this.lblCurrent.Location = new System.Drawing.Point(518, 330);
            this.lblCurrent.Name = "lblCurrent";
            this.lblCurrent.Size = new System.Drawing.Size(144, 113);
            this.lblCurrent.TabIndex = 8;
            // 
            // btnSwitch
            // 
            this.btnSwitch.Location = new System.Drawing.Point(668, 330);
            this.btnSwitch.Name = "btnSwitch";
            this.btnSwitch.Size = new System.Drawing.Size(120, 23);
            this.btnSwitch.TabIndex = 11;
            this.btnSwitch.Text = "交换";
            this.btnSwitch.UseVisualStyleBackColor = true;
            this.btnSwitch.Click += new System.EventHandler(this.btnSwitch_Click);
            // 
            // btnSimulation
            // 
            this.btnSimulation.Location = new System.Drawing.Point(668, 359);
            this.btnSimulation.Name = "btnSimulation";
            this.btnSimulation.Size = new System.Drawing.Size(120, 23);
            this.btnSimulation.TabIndex = 12;
            this.btnSimulation.Text = "开始模拟";
            this.btnSimulation.UseVisualStyleBackColor = true;
            this.btnSimulation.Click += new System.EventHandler(this.btnSimulation_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(668, 388);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(120, 23);
            this.btnReset.TabIndex = 13;
            this.btnReset.Text = "重置";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnRecord
            // 
            this.btnRecord.Location = new System.Drawing.Point(668, 417);
            this.btnRecord.Name = "btnRecord";
            this.btnRecord.Size = new System.Drawing.Size(120, 23);
            this.btnRecord.TabIndex = 14;
            this.btnRecord.Text = "查看交换记录";
            this.btnRecord.UseVisualStyleBackColor = true;
            this.btnRecord.Click += new System.EventHandler(this.btnRecord_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.numMinHp);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.comboBoxSimMode);
            this.groupBox3.Controls.Add(this.numSimCnt);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Location = new System.Drawing.Point(218, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(200, 122);
            this.groupBox3.TabIndex = 5;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "模拟设置";
            // 
            // checkBoxAutoRevert
            // 
            this.checkBoxAutoRevert.AutoSize = true;
            this.checkBoxAutoRevert.Location = new System.Drawing.Point(518, 392);
            this.checkBoxAutoRevert.Name = "checkBoxAutoRevert";
            this.checkBoxAutoRevert.Size = new System.Drawing.Size(132, 16);
            this.checkBoxAutoRevert.TabIndex = 11;
            this.checkBoxAutoRevert.Text = "开始模拟前自动重置";
            this.checkBoxAutoRevert.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 75);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 12);
            this.label9.TabIndex = 6;
            this.label9.Text = "最小生命";
            // 
            // numMinHp
            // 
            this.numMinHp.Location = new System.Drawing.Point(65, 73);
            this.numMinHp.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numMinHp.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMinHp.Name = "numMinHp";
            this.numMinHp.Size = new System.Drawing.Size(129, 21);
            this.numMinHp.TabIndex = 10;
            this.numMinHp.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // comboBoxSimMode
            // 
            this.comboBoxSimMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSimMode.FormattingEnabled = true;
            this.comboBoxSimMode.Location = new System.Drawing.Point(64, 20);
            this.comboBoxSimMode.Name = "comboBoxSimMode";
            this.comboBoxSimMode.Size = new System.Drawing.Size(130, 20);
            this.comboBoxSimMode.TabIndex = 8;
            this.comboBoxSimMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxSimMode_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 23);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(53, 12);
            this.label10.TabIndex = 1;
            this.label10.Text = "模拟类型";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.btnDown);
            this.groupBox5.Controls.Add(this.btnUp);
            this.groupBox5.Controls.Add(this.btnRemove);
            this.groupBox5.Controls.Add(this.btnAdd);
            this.groupBox5.Controls.Add(this.treeViewRule);
            this.groupBox5.Location = new System.Drawing.Point(518, 140);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(270, 184);
            this.groupBox5.TabIndex = 12;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "自定义模拟策略";
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.SystemColors.Window;
            this.label11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label11.Location = new System.Drawing.Point(302, 410);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(200, 20);
            this.label11.TabIndex = 15;
            this.label11.Text = "单击选中通宝，双击添加/更改通宝";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // treeViewRule
            // 
            this.treeViewRule.CheckBoxes = true;
            this.treeViewRule.Location = new System.Drawing.Point(6, 49);
            this.treeViewRule.Name = "treeViewRule";
            this.treeViewRule.Size = new System.Drawing.Size(258, 117);
            this.treeViewRule.TabIndex = 0;
            this.treeViewRule.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewRule_AfterCheck);
            this.treeViewRule.DoubleClick += new System.EventHandler(this.treeViewRule_DoubleClick);
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("宋体", 12F);
            this.btnAdd.Location = new System.Drawing.Point(8, 20);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(23, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "+";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Font = new System.Drawing.Font("宋体", 12F);
            this.btnRemove.Location = new System.Drawing.Point(37, 20);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(23, 23);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "-";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnDown
            // 
            this.btnDown.Font = new System.Drawing.Font("宋体", 9F);
            this.btnDown.Location = new System.Drawing.Point(241, 20);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(23, 23);
            this.btnDown.TabIndex = 4;
            this.btnDown.Text = "↓";
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnUp
            // 
            this.btnUp.Font = new System.Drawing.Font("宋体", 9F);
            this.btnUp.Location = new System.Drawing.Point(212, 20);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(23, 23);
            this.btnUp.TabIndex = 3;
            this.btnUp.Text = "↑";
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // linkLblSync
            // 
            this.linkLblSync.AutoSize = true;
            this.linkLblSync.Location = new System.Drawing.Point(63, 101);
            this.linkLblSync.Name = "linkLblSync";
            this.linkLblSync.Size = new System.Drawing.Size(161, 12);
            this.linkLblSync.TabIndex = 16;
            this.linkLblSync.TabStop = true;
            this.linkLblSync.Text = "同步实时资源到初始资源设置";
            this.linkLblSync.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLblSync_LinkClicked);
            // 
            // checkBoxEnableRecord
            // 
            this.checkBoxEnableRecord.AutoSize = true;
            this.checkBoxEnableRecord.Checked = true;
            this.checkBoxEnableRecord.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableRecord.Location = new System.Drawing.Point(518, 412);
            this.checkBoxEnableRecord.Name = "checkBoxEnableRecord";
            this.checkBoxEnableRecord.Size = new System.Drawing.Size(132, 28);
            this.checkBoxEnableRecord.TabIndex = 16;
            this.checkBoxEnableRecord.Text = "模拟时记录每次交换\r\n(会使模拟性能下降)";
            this.checkBoxEnableRecord.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AcceptButton = this.btnSwitch;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 452);
            this.Controls.Add(this.checkBoxEnableRecord);
            this.Controls.Add(this.checkBoxAutoRevert);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.btnRecord);
            this.Controls.Add(this.lblCurrent);
            this.Controls.Add(this.btnSwitch);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnSimulation);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.listViewTongbao);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "明日方舟-界园筹谋模拟器";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSimCnt)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numShield)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHope)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCandle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCoupon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIngots)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHp)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinHp)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxSquad;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxFortune;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView listViewTongbao;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numSimCnt;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numHp;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numIngots;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numCoupon;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numShield;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numHope;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numCandle;
        private System.Windows.Forms.Label lblRes;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label lblCurrent;
        private System.Windows.Forms.Button btnSwitch;
        private System.Windows.Forms.Button btnSimulation;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnRecord;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox comboBoxSimMode;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numMinHp;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBoxAutoRevert;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panelLockedList;
        private System.Windows.Forms.TreeView treeViewRule;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.LinkLabel linkLblSync;
        private System.Windows.Forms.CheckBox checkBoxEnableRecord;
    }
}