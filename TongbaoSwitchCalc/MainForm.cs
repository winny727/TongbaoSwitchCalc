using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TongbaoSwitchCalc.DataModel;
using TongbaoSwitchCalc.Impl;

namespace TongbaoSwitchCalc
{
    public partial class MainForm : Form
    {
        private PlayerData mPlayerData;
        private IRandomGenerator mRandom;

        private int mSelectedTongbaoPosIndex = -1;

        public MainForm()
        {
            InitializeComponent();
            InitDataModel();

            DataModelTest();
        }

        private void InitDataModel()
        {
            Helper.InitConfig();
            mRandom = new RandomGenerator();
            mPlayerData = new PlayerData(mRandom);
        }

        private void InitPlayerData()
        {
            mPlayerData.Init(SquadType.Flower, new Dictionary<ResType, int>()
            {
                { ResType.LifePoint, 101 },
                { ResType.OriginiumIngots, 1000 },
                { ResType.Coupon, 10 },
                { ResType.Candles, 5 },
                { ResType.Shield, 2 },
                { ResType.Hope, 1 },
            });
        }

        private void DataModelTest()
        {
            InitPlayerData();

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

            using (CodeTimer ct = new CodeTimer("Test"))
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
                    Helper.GetTongbaoName(mPlayerData.TongbaoBox[i].Id) : "Empty";
                Helper.Log($"[{i}]={tongbaoName}");
            }

            foreach (ResType type in Enum.GetValues(typeof(ResType)))
            {
                Helper.Log($"[{Helper.GetResName(type)}]={mPlayerData.GetResValue(type)}");
            }
        }

        private void UpdateView()
        {

        }

        private void OnSelectNewTongbao(int id, int posIndex)
        {
            Tongbao tongbao = Tongbao.CreateTongbao(id);
            mPlayerData.InsertTongbao(tongbao, posIndex);
        }

        private void SelectTongbaoPos(int posIndex)
        {
            mSelectedTongbaoPosIndex = posIndex;
            UpdateView();
        }

        private void StartSwitchSimulation()
        {

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
                    MessageBox.Show($"交换失败，选中通宝[{Helper.GetTongbaoName(tongbao.Id)}]无法交换。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!force && !mPlayerData.HasEnoughSwitchLife())
                {
                    MessageBox.Show($"交换失败，当前生命值不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                MessageBox.Show("交换失败，请检查当前配置和状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            UpdateView();
        }
    }
}
