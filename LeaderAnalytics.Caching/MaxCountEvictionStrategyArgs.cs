using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public class MaxCountEvictionStrategyArgs : IEvictionStrategyArgs
    {
        public EvictionStrategy EvictionStrategy { get; private set; }
        public int MaxCount { get; private set; }

        public MaxCountEvictionStrategyArgs(int maxCount = int.MaxValue)
        {
            EvictionStrategy = EvictionStrategy.MaxCount;
            MaxCount = maxCount;
        }
    }
}
