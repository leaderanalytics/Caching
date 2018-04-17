using System;
using NUnit;
using NUnit.Framework;
using LeaderAnalytics.Caching;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LeaderAnalytics.Caching.Tests
{
    public class Customer
    {
        public int DatabaseID { get; set; }
        public string FederalEIN { get; set; }
        public string SalesForceID { get; set; }
        public string CompanyName { get; set; }
    }


    [TestFixture]
    public class MultiIndexCacheTests
    {
        private MultiIndexCache<Customer> customerCache;

        [SetUp]
        public void Setup()
        {
            // All indexes must be unique.
            Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
            Customer cust2 = new Customer { DatabaseID = 2, FederalEIN = "20", SalesForceID = "200", CompanyName = "ABC" };
            Customer cust3 = new Customer { DatabaseID = 3, FederalEIN = "30", SalesForceID = "20", CompanyName = "ABC" }; // SalesForceID is same as FederalEIN of previous customer

            customerCache = new MultiIndexCache<Customer>(x => x.DatabaseID.ToString(), x => x.FederalEIN, x => x.FederalEIN + "_" + x.SalesForceID, x => x.SalesForceID);

            customerCache.Set(cust1);
            customerCache.Set(cust2);
            customerCache.Set(cust3);
        }


        [Test]
        public void Resolve_Cached_Object_For_All_Expression_Types()
        {
            int lookupKey = 1;
            Func<string> func = () => "3";
            Func<int,string> func2 = z => "20";

            Customer result1 = customerCache.Get(x => x.DatabaseID.ToString() == lookupKey.ToString());
            Assert.AreEqual(1, result1.DatabaseID);

            Customer result2 = customerCache.Get(x => "1" == x.DatabaseID.ToString());
            Assert.AreEqual(1, result2.DatabaseID);

            Customer result3 = customerCache.Get(x => x.FederalEIN == "20");
            Assert.AreEqual("20", result3.FederalEIN);
            Assert.AreEqual(2, result3.DatabaseID);

            Customer result4 = customerCache.Get(x => "20" == x.FederalEIN);
            Assert.AreEqual(2, result4.DatabaseID);

            string lookupKey2 = "20_200";
            Customer result5 = customerCache.Get(x => x.FederalEIN + "_" + x.SalesForceID == lookupKey2);
            Assert.AreEqual(2, result5.DatabaseID);

            Customer result6 = customerCache.Get(x => lookupKey2 == x.FederalEIN + "_" + x.SalesForceID);
            Assert.AreEqual(2, result6.DatabaseID);

            Customer result7 = customerCache.Get(x => x.DatabaseID.ToString() == func());
            Assert.AreEqual(3, result7.DatabaseID);

            Customer result8 = customerCache.Get(x => func() == x.DatabaseID.ToString());
            Assert.AreEqual(3, result8.DatabaseID);

            Customer result9 = customerCache.Get(x => func2(2) == x.FederalEIN);
            Assert.AreEqual(2, result9.DatabaseID);

            Customer result10 = customerCache.Get(x => x.FederalEIN == func2(2));
            Assert.AreEqual(2, result10.DatabaseID);

            Customer result11 = customerCache.Get(1, "20");
            Assert.AreEqual(2, result11.DatabaseID);

            Customer result12 = customerCache.Get(3, "20");
            Assert.AreEqual(3, result12.DatabaseID);

        }


        [Test]
        public void Lookup_using_nonexistant_identifier_returns_null()
        {
            Customer result1 = customerCache.Get(x => x.DatabaseID.ToString() == "999");
            Assert.AreEqual(null, result1);
        }

        [Test]
        public void Lookup_using_invalid_key_throws()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => customerCache.Get(x => x.CompanyName == "ABC"));
            Assert.AreEqual(ex.Message, "An index matching (x.CompanyName == \"ABC\") was not found.");
        }
    }
}
