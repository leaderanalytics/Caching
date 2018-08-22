using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LeaderAnalytics.Caching
{
    

    public class Cache<TValue> : ICache<TValue>, IDisposable
    {
        public int Count { get => cache.Count; }
        public bool IsSeeded { get => cache.Any(); }
        public bool IsEnabled { get; set; }
        public EvictionStrategy EvictionStrategy { get; private set; }
        public TimeSpan TimeToLive { get; private set; }
        private Timer EvictionTimer;
        private ConcurrentDictionary<string, CacheItem<TValue>> cache;

        public Cache() : this(null)
        {

        }

        public Cache(IEvictionStrategyArgs args)
        {
            cache = new ConcurrentDictionary<string, CacheItem<TValue>>();
            EvictionStrategy = args?.EvictionStrategy ?? EvictionStrategy.None;

            if (args is TimeSinceAddEvictionStrategyArgs)
            {
                TimeSinceAddEvictionStrategyArgs targs = args as TimeSinceAddEvictionStrategyArgs;
                SetupEvictionTimer(targs.TTL_minutes, targs.Eviction_minutes);
            }
            else if (args is TimeSinceGetEvictionStrategyArgs)
            {
                TimeSinceGetEvictionStrategyArgs targs = args as TimeSinceGetEvictionStrategyArgs;
                SetupEvictionTimer(targs.TTL_minutes, targs.Eviction_minutes);
            }

            IsEnabled = true;
        }

        private void SetupEvictionTimer(int ttLMins, int evictionMins)
        {
            if(ttLMins > 0)
                TimeToLive = new TimeSpan(0, ttLMins, 0);

            if (evictionMins > 0)
                EvictionTimer = new Timer((x) => Evict(), null, evictionMins * 60000, evictionMins * 60000);
        }

        public TValue Get(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            if (!CacheManager.EnableCaching || ! IsEnabled)
                return default(TValue);

            CacheItem<TValue> result = null;
            bool isFound = cache.TryGetValue(key, out result);

            if (isFound && EvictionStrategy == EvictionStrategy.TimeSinceGet)
                result.TimeStamp = DateTime.Now;

            return result == null ? default(TValue) : result.Value;
        }

        public void Set(string key, TValue item)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (!CacheManager.EnableCaching || ! IsEnabled)
                return;

            cache[key] = new CacheItem<TValue>(item);
        }

        public void Remove(string key)
        {
            if (!CacheManager.EnableCaching || !IsEnabled)
                return;

            cache.TryRemove(key, out CacheItem<TValue> obj);
        }

        public void Seed(IEnumerable<KeyValuePair<string, TValue>> items)
        {

            if (items == null || !items.Any())
                return;

            cache = new ConcurrentDictionary<string, CacheItem<TValue>>(items.Select(x => new KeyValuePair<string, CacheItem<TValue>>(x.Key, new CacheItem<TValue>(x.Value))));
        }

        public void Purge()
        {
            cache.Clear();
        }

        public void Evict()
        {
            if (EvictionStrategy == EvictionStrategy.None || EvictionStrategy == EvictionStrategy.MaxCount || TimeToLive.TotalMinutes == 0)
                return;

            // loops through the cache and evicts expired objects
            DateTime cutoff = DateTime.Now.Subtract(TimeToLive);

            List<string> keys = cache.Where(x => cutoff > x.Value.TimeStamp).Select(x => x.Key).ToList();
            keys.ForEach(x => cache.TryRemove(x, out CacheItem<TValue> obj));
        }

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    EvictionTimer?.Dispose();
                    disposed = true;
                }
            }

        }
    }
}
