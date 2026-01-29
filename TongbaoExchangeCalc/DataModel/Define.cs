using System;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel
{
    public static class Define
    {
        public static readonly IReadOnlyList<RandomResDefine> RandomResDefines = new List<RandomResDefine>()
        {
            new RandomResDefine(0.0393f, ResType.Shield, 2),
            new RandomResDefine(0.0291f, ResType.Hope, 1),
            new RandomResDefine(0.0094f, ResType.Candles, 1),
        };

        // 不同分队的钱盒容量/交换消耗生命值
        public static readonly IReadOnlyDictionary<SquadType, SquadDefine> SquadDefines = new Dictionary<SquadType, SquadDefine>()
        {
            { SquadType.Flower, new SquadDefine(10, new int[]{ 1 }) },
            { SquadType.Tourist, new SquadDefine(13, new int[]{ 1, 1, 2, 2, 3 }) },
            { SquadType.Other, new SquadDefine(10, new int[]{ 1, 1, 2, 2, 3 }) },
        };

        public static readonly IReadOnlyDictionary<ResType, ResType> ParentResType = new Dictionary<ResType, ResType>()
        {
            { ResType.PrimalFarmingCandles, ResType.Candles },
        };

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
                case ResType.PrimalFarmingCandles:
                    return "鸿蒙开荒烛火";
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
    }

    public enum TongbaoType : sbyte
    {
        Unknown = 0,
        Balance = 1, //衡钱
        Flower = 2, //花钱
        Risk = 3, //厉钱
    }

    public enum ResType : sbyte
    {
        None = 0,
        LifePoint = 1, //生命值
        OriginiumIngots = 2, //源石锭
        Coupon = 3, //票券
        Candles = 4, //烛火
        PrimalFarmingCandles = 5, //鸿蒙开荒烛火
        Hope = 6, //希望
        Shield = 7, //护盾

        Count,

        /*
        HP 目标生命
        HPMAX 生命上限
        GOLD 源石锭
        POPULATION 希望
        SHIELD 护盾
        DIVINATION_KIT 票券
        SPECIAL_ZONE_AP 烛火
        RELIC 收藏品
        PLAYER_LEVEL_UP 指挥等级
         */
    }

    public enum SquadType : sbyte
    {
        Flower = 0, //花团锦簇分队
        Tourist = 1, //游客分队
        Other = 2, //其它分队
    }

    [Flags]
    public enum SpecialConditionFlag : sbyte
    {
        None = 0,
        Collectible_Fortune = 1 << 0, //福祸相依
    }

    public class RandomResDefine
    {
        public readonly float Probability;
        public readonly ResType ResType;
        public readonly int ResCount;

        public RandomResDefine(float probability, ResType resType, int resCount)
        {
            Probability = probability;
            ResType = resType;
            ResCount = resCount;
        }
    }

    public class SquadDefine
    {
        public readonly int MaxTongbaoCount;
        public readonly int[] CostLifePoints;

        public SquadDefine(int maxTongbaoCount, int[] costLifePoints)
        {
            MaxTongbaoCount = maxTongbaoCount;
            CostLifePoints = costLifePoints;
        }

        // 从0开始
        public int GetCostLifePoint(int exchangeCount)
        {
            if (exchangeCount < 0 || CostLifePoints == null || CostLifePoints.Length == 0)
                return 0;

            int index = Math.Min(exchangeCount, CostLifePoints.Length - 1);
            return CostLifePoints[index];
        }
    }
}
