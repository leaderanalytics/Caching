using System;
using System.Collections.Generic;
using System.Text;
using NUnit;
using NUnit.Framework;
using LeaderAnalytics.Caching;
using System.Threading.Tasks;

namespace LeaderAnalytics.Caching.Tests
{
    [TestFixture]
    public class CacheTests
    {
        private ICache<Customer> customerCache;

        [SetUp]
        public void Setup()
        {
            customerCache = new Cache<Customer>();
            Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
            Customer cust2 = new Customer { DatabaseID = 2, FederalEIN = "20", SalesForceID = "200", CompanyName = "ABC" };
            Customer cust3 = new Customer { DatabaseID = 3, FederalEIN = "30", SalesForceID = "20", CompanyName = "ABC" };
            customerCache.Set(cust1.DatabaseID.ToString(), cust1);
            customerCache.Set(cust2.DatabaseID.ToString(), cust2);
            customerCache.Set(cust3.DatabaseID.ToString(), cust3);
        }

        [Test]
        public async Task Timed_eviction_stratagies_evict()
        {
            // run these guys in parallel since they each take 1 min.
            Task t0 = Task.Run(() => TimeSinceAdd_eviction_strategy_evicts());
            Task t1 = Task.Run(() => TimeSinceGet_eviction_strategy_evicts());

            try
            {
                await Task.WhenAll(t0, t1);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void TimeSinceAdd_eviction_strategy_evicts()
        {
            ICache<Customer> cache = new Cache<Customer>(new TimeSinceAddEvictionStrategyArgs(1, 1));
            Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
            cache.Set(cust1.DatabaseID.ToString(), cust1);
            Assert.AreEqual(1, cache.Count);
            System.Threading.Thread.Sleep(61000);
            Assert.AreEqual(0, cache.Count);
        }

        public void TimeSinceGet_eviction_strategy_evicts()
        {
            ICache<Customer> cache = new Cache<Customer>(new TimeSinceGetEvictionStrategyArgs(1, 1));
            Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
            Customer cust2 = new Customer { DatabaseID = 2, FederalEIN = "20", SalesForceID = "200", CompanyName = "ABC" };
            cache.Set(cust1.DatabaseID.ToString(), cust1);
            cache.Set(cust2.DatabaseID.ToString(), cust2);
            Assert.AreEqual(2, cache.Count);
            System.Threading.Thread.Sleep(31000);                           // sleep for thirty seconds
            Customer c1 = cache.Get(cust1.DatabaseID.ToString());           // get cust1 to reset its timestamp
            Assert.IsNotNull(c1);
            System.Threading.Thread.Sleep(35000);                           // sleep for another thirty seconds
            Assert.AreEqual(1, cache.Count);                                // verify that cust1 remains in the cache ache cust2 was evicted 
            c1 = cache.Get(cust1.DatabaseID.ToString());
            Assert.IsNotNull(c1);
            Customer c2 = cache.Get(cust2.DatabaseID.ToString());
            Assert.IsNull(c2);
        }
    }
}
