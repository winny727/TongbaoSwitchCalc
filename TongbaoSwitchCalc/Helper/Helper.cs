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

        public static string GetTongbaoName(int id)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config!=null)
            {
                string typeName = GetTongbaoTypeName(config.Type);
                return $"{typeName}-{config.Name}";
            }
            return string.Empty;
        }

        public static string GetTongbaoTypeName(TongbaoType type)
        {
            switch (type)
            {
                case TongbaoType.Balance:
                    return "衡";
                case TongbaoType.Flower:
                    return "花";
                case TongbaoType.Risk:
                    return "厉";
                default:
                    break;
            }
            return string.Empty;
        }

        public static string GetResName(ResType type)
        {
            switch (type)
            {
                case ResType.None:
                    return "无";
                case ResType.LifePoint:
                    return "生命值";
                case ResType.OriginiumIngots:
                    return "源石锭";
                case ResType.Coupon:
                    return "票券";
                case ResType.Candles:
                    return "烛火";
                case ResType.Shield:
                    return "护盾";
                case ResType.Hope:
                    return "希望";
                default:
                    break;
            }
            return string.Empty;
        }

        public static string GetSquadName(SquadType type)
        {
            switch (type)
            {
                case SquadType.Flower:
                    return "花团锦簇分队";
                case SquadType.Tourist:
                    return "游客分队";
                case SquadType.Other:
                    return "其它分队";
                default:
                    break;
            }
            return string.Empty;
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
