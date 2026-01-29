using System;
using System.Collections;
using System.Collections.Generic;

namespace TongbaoExchangeCalc.DataModel.Simulation
{
    public class UniqueRuleCollection : IEnumerable<SimulationRule>
    {
        public UniqueRuleCollection(SimulationRuleType type)
        {
            Type = type;
        }

        public SimulationRuleType Type { get; private set; }

        private readonly List<SimulationRule> mItems = new List<SimulationRule>();
        public IReadOnlyList<SimulationRule> Items => mItems;

        public int Count => mItems.Count;
        public SimulationRule this[int index] => mItems[index];

        public bool IsUniqueRule(SimulationRule rule)
        {
            if (rule == null)
            {
                return false;
            }

            foreach (var item in mItems)
            {
                if (item.Equals(rule))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Add(SimulationRule item)
        {
            if (IsUniqueRule(item))
            {
                mItems.Add(item);
                return true;
            }
            return false;
        }

        public bool Insert(int index, SimulationRule item)
        {
            if (IsUniqueRule(item))
            {
                mItems.Insert(index, item);
                return true;
            }
            return false;
        }

        public bool Contains(SimulationRule item)
        {
            return mItems.Contains(item);
        }

        public int IndexOf(SimulationRule item)
        {
            return mItems.IndexOf(item);
        }

        public bool Remove(SimulationRule item)
        {
            return mItems.Remove(item);
        }

        public void RemoveAt(int index)
        {
            mItems.RemoveAt(index);
        }

        public void Clear()
        {
            mItems.Clear();
        }

        public bool MoveToIndex(SimulationRule item, int index)
        {
            if (index >= 0 && index < mItems.Count)
            {
                mItems.Remove(item);
                mItems.Insert(index, item);
                return true;
            }
            return false;
        }

        public IEnumerator<SimulationRule> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
