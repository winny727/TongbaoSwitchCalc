using System;
using System.Collections.Generic;

namespace TongbaoSwitchCalc.DataModel
{
    public static class Define
    {
        public static readonly List<RandomResDefine> RandomResDefines = new List<RandomResDefine>()
        {
            new RandomResDefine(0.0393f, ResType.Shield, 2),
            new RandomResDefine(0.0291f, ResType.Hope, 1),
            new RandomResDefine(0.0094f, ResType.Candles, 1),
        };

        // 不同分队的钱盒容量/交换消耗生命值
        public static readonly Dictionary<SquadType, SquadDefine> SquadDefines = new Dictionary<SquadType, SquadDefine>()
        {
            { SquadType.Flower, new SquadDefine(10, new int[]{ 1 }) },
            { SquadType.Tourist, new SquadDefine(13, new int[]{ 1, 1, 2, 2, 3 }) },
            { SquadType.Other, new SquadDefine(10, new int[]{ 1, 1, 2, 2, 3 }) },
        };
    }

    public enum ResType
    {
        None = 0,
        LifePoint = 1, //生命值
        OriginiumIngots = 2, //源石锭
        Coupon = 3, //票券
        Candles = 4, //烛火
        TongbaoCandles = 5, //鸿蒙开荒烛火
        Hope = 6, //希望
        Shield = 7, //护盾
    }

    public enum SquadType
    {
        Flower = 0, //花团锦簇分队
        Tourist = 1, //游客分队
        Other = 2, //其它分队
    }

    [Flags]
    public enum SpecialConditionFlag
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
        public int GetCostLifePoint(int switchCount)
        {
            if (switchCount < 0 || CostLifePoints == null || CostLifePoints.Length == 0)
                return 0;

            int index = Math.Min(switchCount, CostLifePoints.Length - 1);
            return CostLifePoints[index];
        }
    }
}
