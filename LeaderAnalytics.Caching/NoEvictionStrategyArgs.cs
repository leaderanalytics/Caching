using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public class NoEvictionStrategyArgs : IEvictionStrategyArgs
    {
        public EvictionStrategy EvictionStrategy { get; private set; } = EvictionStrategy.None;
    }
}
