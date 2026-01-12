using TongbaoSwitchCalc.DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace TongbaoSwitchCalc
{
    public static class Helper
    {
        public static readonly Dictionary<string, TongbaoConfig> TongbaoNameDict = new Dictionary<string, TongbaoConfig>();
        public static readonly Dictionary<int, Image> TongbaoImageDict = new Dictionary<int, Image>();
        private static Image mTongbaoSlotImage;
        private static Dictionary<string, Image> mTongbaoImageCache = new Dictionary<string, Image>();

        public static TongbaoConfig GetTongbaoConfigByName(string name)
        {
            if (TongbaoNameDict.TryGetValue(name, out var config))
            {
                return config;
            }
            return null;
        }

        public static Image GetTongbaoImage(int id)
        {
            if (TongbaoImageDict.TryGetValue(id, out var image))
            {
                return image;
            }
            return null;
        }

        public static string GetTongbaoFullName(int id)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config!=null)
            {
                string typeName = Define.GetTongbaoTypeName(config.Type);
                return $"{typeName}-{config.Name}";
            }
            return string.Empty;
        }

        public static void AppendTongbaoFullName(StringBuilder sb, int id)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config != null)
            {
                string typeName = Define.GetTongbaoTypeName(config.Type);
                sb.Append(typeName)
                  .Append('-')
                  .Append(config.Name);
            }
        }

        public static void InitConfig()
        {
            TongbaoConfig.ClearTongbaoConfig();
            InitTongbaoConfig();
            InitTongbaoNameDict();
        }

        public static void InitResources()
        {
            InitTongbaoImage();
            InitTongbaoSlotImage();
            InitTongbaoImageCache();
        }

        private static void InitTongbaoNameDict()
        {
            TongbaoNameDict.Clear();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                TongbaoNameDict[config.Name] = config;
            }
        }

        private static void InitTongbaoImage()
        {
            TongbaoImageDict.Clear();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                string path = Environment.CurrentDirectory + $"/Resources/Image/{config.ImgPath}";
                if (File.Exists(path))
                {
                    try
                    {
                        Image image = Image.FromFile(path);
                        TongbaoImageDict[config.Id] = image;
                    }
                    catch (Exception ex)
                    {
                        Log(ex.ToString());
                    }
                }
            }
        }

        private static void InitTongbaoSlotImage()
        {
            mTongbaoSlotImage?.Dispose();
            string path = Environment.CurrentDirectory + "/Resources/Image/Empty.png";
            if (File.Exists(path))
            {
                try
                {
                    mTongbaoSlotImage = Image.FromFile(path);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }
            }
        }

        private static void InitTongbaoConfig()
        {
            string path = Environment.CurrentDirectory + "/Config/TongbaoConfig.txt";
            TableReader tableReader = LoadConfig(path);

            if (tableReader == null)
            {
                return;
            }

            tableReader.ForEach((key, line) =>
            {
                int id = line.GetValue<int>("Id");
                string name = line.GetValue("Name");
                string description = line.GetValue("Description");
                string imgPath = line.GetValue("ImgPath");
                TongbaoType type = line.GetValue<TongbaoType>("Type");
                int switchInPool = line.GetValue<int>("SwitchInPool");
                List<int> switchOutPools = ParseList<int>(line.GetValue("SwitchOutPools"));
                ResType extraResType = line.GetValue<ResType>("ExtraResType");
                int extraResCount = line.GetValue<int>("ExtraResCount");

                TongbaoConfig tongbao = new TongbaoConfig
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    ImgPath = imgPath,
                    Type = type,
                    SwitchInPool = switchInPool,
                    SwitchOutPools = switchOutPools,
                    ExtraResType = extraResType,
                    ExtraResCount = extraResCount,
                };

                TongbaoConfig.AddTongbaoConfig(tongbao);
            });
        }

        private static List<T> ParseList<T>(string text, char split = '|')
        {
            List<T> list = new List<T>();
            if (string.IsNullOrEmpty(text))
            {
                return list;
            }

            string[] values = text.Split(split);
            for (int i = 0; i < values.Length; i++)
            {
                if (string.IsNullOrEmpty(values[i]))
                {
                    list.Add(default);
                    continue;
                }

                T value = values[i].ConvertTo<T>(default);
                list.Add(value);
            }

            return list;
        }


        public static TableReader LoadConfig(string path)
        {
            try
            {
                TableReader tableReader = new TableReader(path, Encoding.Default);
                return tableReader;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        private static void InitTongbaoImageCache()
        {
            mTongbaoImageCache.Clear();

            if (mTongbaoSlotImage != null)
            {
                mTongbaoImageCache.Add("Empty", mTongbaoSlotImage);
            }
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                Image image = GetTongbaoImage(config.Id);
                if (image != null)
                {
                    mTongbaoImageCache.Add(config.Id.ToString(), image);
                }
            }
        }

        public static ImageList CreateTongbaoImageList(Size imageSize)
        {
            ImageList imageList = new ImageList
            {
                ImageSize = imageSize,
            };

            foreach (var item in mTongbaoImageCache)
            {
                imageList.Images.Add(item.Key, item.Value);
            }

            return imageList;
        }

        public static void SetupResNumberic(PlayerData playerData, NumericUpDown numeric, ResType type)
        {
            if (playerData == null || numeric == null)
            {
                return;
            }

            decimal lastValue = numeric.Value;
            void OnValueChanged(object sender, EventArgs e)
            {
                //if (type == ResType.LifePoint && playerData.SwitchCount > 0 && numeric.Value != lastValue)
                //{
                //    var result = MessageBox.Show("修改生命值会重置当前交换次数，是否继续？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //    if (result == DialogResult.No)
                //    {
                //        return;
                //    }
                //    playerData.SwitchCount = 0;
                //}

                playerData.AddResValue(type, (int)(numeric.Value - lastValue));
                lastValue = numeric.Value;
            }

            numeric.ValueChanged -= OnValueChanged;
            numeric.ValueChanged += OnValueChanged;
        }

        public static void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }
    }
}
