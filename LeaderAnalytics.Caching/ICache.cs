using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderAnalytics.Caching
{
    public interface ICache<TValue>
    {
        /// <summary>
        /// Finds an item in the cache based on passed key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        TValue Get(string key);
        /// <summary>
        /// Adds Item to cache using provided key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        void Set(string key, TValue item);
        /// <summary>
        /// Removes item with provided key if it exists.
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);
        /// <summary>
        /// Populates the cache with
        /// </summary>
        /// <param name="items"></param>
        void Seed(IEnumerable<KeyValuePair<string, TValue>> items);
        /// <summary>
        /// Clears all items from the cache
        /// </summary>
        void Purge();
        /// <summary>
        /// Removes items that are eligible for eviction based  
        /// </summary>
        void Evict();
        /// <summary>
        /// Returns true if one or more items exists in the Cache
        /// </summary>
        /// <returns></returns>
        bool IsSeeded { get; }
        /// <summary>
        /// Enables/Disables Get, Set, and Remove.  CacheManager.EnableCaching must ALSO be true for caching to be enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Maximum amount of time item can live in the cache
        /// </summary>
        TimeSpan TimeToLive { get;  }
    }
}
