using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public class TimeSinceGetEvictionStrategyArgs : IEvictionStrategyArgs
    {
        public EvictionStrategy EvictionStrategy { get; private set; }
        public int TTL_minutes { get; private set; }
        public int Eviction_minutes { get; private set; }

        public TimeSinceGetEvictionStrategyArgs(int ttl_minutes = 120, int eviction_minutes = 120)
        {
            EvictionStrategy = EvictionStrategy.TimeSinceGet;
            TTL_minutes = ttl_minutes;
            Eviction_minutes = eviction_minutes;
        }
    }
}
