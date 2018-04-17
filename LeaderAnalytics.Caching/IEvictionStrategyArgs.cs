using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public interface IEvictionStrategyArgs
    {
        EvictionStrategy EvictionStrategy { get; }
    }
}
