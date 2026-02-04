using TongbaoExchangeCalc.DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using TongbaoExchangeCalc.DataModel.Simulation;

namespace TongbaoExchangeCalc
{
    public static class Helper
    {
        public static readonly Dictionary<string, TongbaoConfig> TongbaoNameDict = new Dictionary<string, TongbaoConfig>();
        public static readonly Dictionary<int, Image> TongbaoImageDict = new Dictionary<int, Image>();
        private static Image mTongbaoSlotImage;
        private static readonly Dictionary<string, Image> mTongbaoImageCache = new Dictionary<string, Image>();

        public static readonly Dictionary<SimulationRuleType, List<SimulationRule>> SimulationRulePresets = new Dictionary<SimulationRuleType, List<SimulationRule>>()
        {
            { SimulationRuleType.ExchangeableSlot, new List<SimulationRule>() },
            { SimulationRuleType.PriorityExchangeTongbao, new List<SimulationRule>() },
            { SimulationRuleType.UnexchangeableTongbao, new List<SimulationRule>() },
            { SimulationRuleType.ExpectationTongbao, new List<SimulationRule>() },
        };

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
            if (config != null)
            {
                return config.Name;
            }
            return string.Empty;
        }

        public static string GetTongbaoFullName(int id)
        {
            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config != null)
            {
                string typeName = Define.GetTongbaoTypeName(config.Type);
                return $"{typeName}-{config.Name}";
            }
            return string.Empty;
        }

        public static StringBuilder AppendTongbaoFullName(StringBuilder sb, int id)
        {
            if (sb == null)
            {
                return sb;
            }

            TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(id);
            if (config != null)
            {
                string typeName = Define.GetTongbaoTypeName(config.Type);
                sb.Append(typeName)
                  .Append('-')
                  .Append(config.Name);
            }
            return sb;
        }

        public static void InitConfig()
        {
            TongbaoConfig.ClearTongbaoConfig();
            InitTongbaoConfig();
            InitTongbaoNameDict();
            InitSimulationRulePresets();
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

        private static void InitSimulationRulePresets()
        {
            void AddTongbaoRuleByName(string name, SimulationRuleType type, Func<int, SimulationRule> newRuleFunc)
            {
                TongbaoConfig config = GetTongbaoConfigByName(name);
                if (config != null)
                {
                    SimulationRule rule = newRuleFunc(config.Id);
                    SimulationRulePresets[type].Add(rule);
                }
                else
                {
                    Log($"Invalid Tongbao Name: {name}");
                }
            }

            SimulationRulePresets[SimulationRuleType.ExchangeableSlot].Clear();
            SimulationRulePresets[SimulationRuleType.PriorityExchangeTongbao].Clear();
            SimulationRulePresets[SimulationRuleType.UnexchangeableTongbao].Clear();
            SimulationRulePresets[SimulationRuleType.ExpectationTongbao].Clear();

            // 可交换槽位
            for (int i = 0; i < Define.SquadDefines[SquadType.Flower].MaxTongbaoCount; i++)
            {
                SimulationRulePresets[SimulationRuleType.ExchangeableSlot].Add(new ExchangeableSlotRule(i, i == 0));
            }

            // 优先交换通宝
            string[] priorityTongbaoNames = new string[] { "聚力则强", "人间长存", /*"茧成绢",*/
                "火上之灶", "鸭爵金币", "鸿蒙开荒", "圣诏封神", "神秘商贾", "诛邪雷法", 
                /*"商路难行",*/ "孜孜不倦", "平沙之盾" };
            foreach (var name in priorityTongbaoNames)
            {
                AddTongbaoRuleByName(name, SimulationRuleType.PriorityExchangeTongbao,
                    (id) => new PriorityExchangeTongbaoRule(id));
            }

            // 不可交换通宝
            string[] unexchangeableTongbaoNames = new string[] { "驰道长", "武人之争", "百业俱兴",
                "寒窗志", "志欲遂", "慧避灾", "茧成绢" };
            foreach (var name in unexchangeableTongbaoNames)
            {
                AddTongbaoRuleByName(name, SimulationRuleType.UnexchangeableTongbao, 
                    (id) => new UnexchangeableTongbaoRule(id));
            }

            // 期望通宝
            string[] expectationTongbaoNames = new string[] { "茧成绢" };
            foreach (var name in expectationTongbaoNames)
            {
                AddTongbaoRuleByName(name, SimulationRuleType.ExpectationTongbao,
                    (id) => new ExpectationTongbaoRule(id));
            }
        }

        private static void InitTongbaoConfig()
        {
            string path = Environment.CurrentDirectory + "/Resources/Config/TongbaoConfig.txt";
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
                int rarity = line.GetValue<int>("Rarity");
                int dlcVersion = line.GetValue<int>("DlcVersion");
                string imgPath = line.GetValue("ImgPath");
                TongbaoType type = line.GetValue<TongbaoType>("Type");
                int exchangeInPool = line.GetValue<int>("ExchangeInPool");
                List<int> exchangeOutPools = ParseList<int>(line.GetValue("ExchangeOutPools"));
                bool isUpgrade = line.GetValue<int>("IsUpgrade") != 0;
                int mutexGroup = line.GetValue<int>("MutexGroup");
                List<ResType> extraResType = ParseList<ResType>(line.GetValue("ExtraResTypes"));
                List<int> extraResCount = ParseList<int>(line.GetValue("ExtraResCounts"));

                TongbaoConfig tongbao = new TongbaoConfig
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    Rarity = rarity,
                    DlcVersion = dlcVersion,
                    ImgPath = imgPath,
                    Type = type,
                    ExchangeInPool = exchangeInPool,
                    ExchangeOutPools = exchangeOutPools,
                    IsUpgrade = isUpgrade,
                    MutexGroup = mutexGroup,
                    ExtraResTypes = extraResType,
                    ExtraResCounts = extraResCount,
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
            // 先尝试 GB2312
            try
            {
                return new TableReader(path, Encoding.GetEncoding("GB2312"));
            }
            catch (Exception ex)
            {
                Log($"通过GB2312编码读取配置失败，尝试UTF-8: {ex.Message}");
            }

            // 再尝试 UTF-8
            try
            {
                return new TableReader(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log($"通过UTF-8编码读取配置失败，尝试系统默认编码: {ex.Message}");
            }

            // 再尝试 Default
            try
            {
                return new TableReader(path, Encoding.Default);
            }
            catch (Exception ex)
            {
                Log($"通过系统默认编码读取配置失败: {ex.Message}");
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

        public static void SetupResNumeric(PlayerData playerData, NumericUpDown numeric, ResType type, Action updateViewCallback = null)
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
                updateViewCallback?.Invoke();
            }

            numeric.ValueChanged -= OnValueChanged;
            numeric.ValueChanged += OnValueChanged;
        }

        public static string GetTongbaoToolTip(PlayerData playerData, Tongbao tongbao)
        {
            if (tongbao == null)
            {
                return "双击添加通宝";
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("双击修改通宝")
              .AppendLine()
              .Append("效果: ")
              .AppendLine(tongbao.Description);
            var exchangeOutTongbaoIds = ExchangePool.GetExchangeOutTongbaoIds(tongbao.ExchangeInPool);
            if (tongbao.CanExchange() && exchangeOutTongbaoIds != null && exchangeOutTongbaoIds.Count > 0)
            {
                sb.AppendLine().Append("可交换出以下通宝");
                if (tongbao.IsUpgrade)
                {
                    if (exchangeOutTongbaoIds.Count == 1)
                    {
                        sb.Append("(固定获得升级通宝)");
                    }
                    else
                    {
                        sb.Append("(玩家选择其中一个升级通宝)");
                    }
                }
                sb.Append(": ");
                foreach (var tongbaoId in exchangeOutTongbaoIds)
                {
                    TongbaoConfig config = TongbaoConfig.GetTongbaoConfigById(tongbaoId);
                    if (config == null)
                    {
                        continue;
                    }

                    sb.AppendLine();
                    AppendTongbaoFullName(sb, tongbaoId);

                    if (playerData == null)
                    {
                        continue;
                    }

                    // 可以自己换出自己
                    if (playerData.IsTongbaoExist(tongbaoId) && tongbaoId != tongbao.Id)
                    {
                        sb.Append("(该通宝已在钱盒)");
                    }

                    if (config.MutexGroup > 0)
                    {
                        var mutexTongbaoIds = ExchangePool.GetMutexTongbaoIds(config.MutexGroup);
                        for (int i = 0; i < mutexTongbaoIds.Count; i++)
                        {
                            int mutexTongbaoId = mutexTongbaoIds[i];
                            if (mutexTongbaoId == tongbao.Id || mutexTongbaoId == tongbaoId)
                            {
                                continue;
                            }
                            if (playerData.IsTongbaoExist(mutexTongbaoId))
                            {
                                sb.Append("(互斥通宝[");
                                AppendTongbaoFullName(sb, mutexTongbaoId);
                                sb.Append("]已在钱盒)");
                                break;
                            }
                        }
                    }

                    if (playerData.IsTongbaoLocked(tongbaoId))
                    {
                        sb.Append("(已被商店锁定)");
                    }
                }
            }
            else
            {
                sb.AppendLine().Append("通宝无法交换");
            }



            return sb.ToString();
        }

        // 获取需要多选一的升级通宝
        public static List<int> GetUpgradeSelectTongbaoIds()
        {
            List<int> result = new List<int>();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                if (!config.IsUpgrade) continue;

                var exchangeOutTongbaoIds = ExchangePool.GetExchangeOutTongbaoIds(config.ExchangeInPool);
                if (exchangeOutTongbaoIds != null && exchangeOutTongbaoIds.Count > 1)
                {
                    foreach (var tongbaoId in exchangeOutTongbaoIds)
                    {
                        if (!result.Contains(tongbaoId))
                        {
                            result.Add(tongbaoId);
                        }
                    }
                }
            }
            return result;
        }

        public static string LoadTongbaoBoxData(PlayerData playerData, string defaultPath = null)
        {
            string path = string.IsNullOrEmpty(defaultPath)
                ? Path.Combine(Application.StartupPath, "Save", "TongbaoBox.sav")
                : defaultPath;

            string dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
            {
                dir = Application.StartupPath;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "请选择要打开的文件",
                Filter = "Save文件 (*.sav)|*.sav",
                InitialDirectory = dir,
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    string text = File.ReadAllText(filePath);
                    playerData.ClearTongbao();
                    string[] items = text.Split(',');
                    for (int i = 0; i < items.Length; i++)
                    {
                        string item = items[i];
                        string name = string.Empty;
                        string randomEffName = string.Empty;
                        string[] parts = item.Split('|');
                        if (parts.Length > 0)
                        {
                            name = parts[0].Trim();
                        }
                        if (parts.Length > 1)
                        {
                            randomEffName = parts[1].Trim();
                        }

                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        TongbaoConfig config = Helper.GetTongbaoConfigByName(name);
                        if (config == null)
                        {
                            continue;
                        }

                        Tongbao tongbao = playerData.CreateTongbao(config.Id);
                        if (!string.IsNullOrEmpty(randomEffName))
                        {
                            foreach (var define in Define.RandomEffDefines)
                            {
                                if (define.Name == randomEffName)
                                {
                                    tongbao.ApplyRandomEff(define);
                                    break;
                                }
                            }
                        }
                        playerData.InsertTongbao(tongbao, i);
                    }
                    return filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"文件读取失败: {ex.Message}\n{dir}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return null;
        }

        public static string SaveTongbaoBoxData(PlayerData playerData, string defaultPath = null)
        {
            string path = string.IsNullOrEmpty(defaultPath)
                ? Path.Combine(Application.StartupPath, "Save", "TongbaoBox.sav")
                : defaultPath;

            string dir = Path.GetDirectoryName(path);
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch //(Exception ex)
            {

            }

            // 如果没啥别的复杂的东西要存就先不用json了
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "请选择文件保存路径",
                Filter = "Save文件 (*.sav)|*.sav",
                InitialDirectory = dir,
                FileName = Path.GetFileName(path),
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string text = string.Empty;
                    for (int i = 0; i < playerData.MaxTongbaoCount; i++)
                    {
                        Tongbao tongbao = playerData.GetTongbao(i);
                        if (tongbao != null)
                        {
                            // 存名字避免ID修改
                            text += tongbao.Name;
                            var randomEff = tongbao.RandomEff;
                            if (randomEff != null)
                            {
                                text += "|" + randomEff.Name;
                            }
                        }
                        text += ',';
                    }

                    File.WriteAllText(filePath, text);
                    return filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"文件保存失败: {ex.Message}\n{dir}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return null;
        }

        public static void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public static void Log(object msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }
    }
}
