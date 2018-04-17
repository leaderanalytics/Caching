using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public class TimeSinceAddEvictionStrategyArgs : IEvictionStrategyArgs
    {
        public EvictionStrategy EvictionStrategy { get; private set; }
        public int TTL_minutes { get; private set; }
        public int Eviction_minutes { get; private set; }

        public TimeSinceAddEvictionStrategyArgs(int ttl_minutes = 120, int eviction_minutes = 120)
        {
            EvictionStrategy = EvictionStrategy.TimeSinceAdd;
            TTL_minutes = ttl_minutes;
            Eviction_minutes = eviction_minutes;
        }
    }
}
