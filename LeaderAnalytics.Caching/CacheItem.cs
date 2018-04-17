using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching
{
    internal class CacheItem<TValue>
    {
        public TValue Value { get; set; }
        public DateTime TimeStamp { get; set; }

        public CacheItem(TValue val)
        {
            Value = val;
            TimeStamp = DateTime.Now;
        }
    }
}
