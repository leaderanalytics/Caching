using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public enum EvictionStrategy
    {
        None,
        TimeSinceAdd,
        TimeSinceGet,
        MaxCount
    }
}
