using TongbaoSwitchCalc.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace TongbaoSwitchCalc
{
    public static class Helper
    {
        public static readonly Dictionary<string, TongbaoConfig> TongbaoNameDict = new Dictionary<string, TongbaoConfig>();

        public static TongbaoConfig GetTongbaoConfigByName(string name)
        {
            if (TongbaoNameDict.TryGetValue(name, out var config))
            {
                return config;
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

        private static void InitTongbaoNameDict()
        {
            TongbaoNameDict.Clear();
            foreach (var item in TongbaoConfig.GetAllTongbaoConfigs())
            {
                TongbaoConfig config = item.Value;
                TongbaoNameDict[config.Name] = config;
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

        public static void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }
    }
}
