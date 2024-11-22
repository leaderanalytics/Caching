using System;
using NUnit;
using NUnit.Framework;
using LeaderAnalytics.Caching;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LeaderAnalytics.Caching.Tests
{

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
            Assert.That(result1.DatabaseID, Is.EqualTo(1));

            Customer result2 = customerCache.Get(x => "1" == x.DatabaseID.ToString());
            Assert.That(result2.DatabaseID, Is.EqualTo(1));

            Customer result3 = customerCache.Get(x => x.FederalEIN == "20");
            Assert.That(result3.FederalEIN, Is.EqualTo("20"));
            Assert.That(result3.DatabaseID, Is.EqualTo(2));

            Customer result4 = customerCache.Get(x => "20" == x.FederalEIN);
            Assert.That(result4.DatabaseID, Is.EqualTo(2));

            string lookupKey2 = "20_200";
            Customer result5 = customerCache.Get(x => x.FederalEIN + "_" + x.SalesForceID == lookupKey2);
            Assert.That(result5.DatabaseID, Is.EqualTo(2));

            Customer result6 = customerCache.Get(x => lookupKey2 == x.FederalEIN + "_" + x.SalesForceID);
            Assert.That(result6.DatabaseID, Is.EqualTo(2));

            Customer result7 = customerCache.Get(x => x.DatabaseID.ToString() == func());
            Assert.That(result7.DatabaseID, Is.EqualTo(3));

            Customer result8 = customerCache.Get(x => func() == x.DatabaseID.ToString());
            Assert.That(result8.DatabaseID, Is.EqualTo(3));

            Customer result9 = customerCache.Get(x => func2(2) == x.FederalEIN);
            Assert.That(result9.DatabaseID, Is.EqualTo(2));

            Customer result10 = customerCache.Get(x => x.FederalEIN == func2(2));
            Assert.That(result10.DatabaseID, Is.EqualTo(2));

            Customer result11 = customerCache.Get(1, "20");
            Assert.That(result11.DatabaseID, Is.EqualTo(2));

            Customer result12 = customerCache.Get(3, "20");
            Assert.That(result12.DatabaseID, Is.EqualTo(3));

        }


        [Test]
        public void Lookup_using_nonexistant_identifier_returns_null()
        {
            Customer result1 = customerCache.Get(x => x.DatabaseID.ToString() == "999");
            Assert.That(result1, Is.Null);
        }

        [Test]
        public void Lookup_using_invalid_key_throws()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => customerCache.Get(x => x.CompanyName == "ABC"));
            Assert.That("An index matching (x.CompanyName == \"ABC\") was not found.", Is.EqualTo(ex.Message));
        }

        [Test]
        public void Adding_partial_duplicate_throws()
        {
            // database ID is unique but other properties are dupes
            Customer cust1 = new Customer { DatabaseID = 9000, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
            Exception ex = Assert.Throws<Exception>(() => customerCache.Set(cust1));
            Assert.That("Duplicate key error. 4 indexes are defined however only 1 keys were created.", Is.EqualTo(ex.Message));
        }

        [Test]
        public void Adding_full_duplicate_replaces_previous_object()
        {
            Customer oldCust1 = customerCache.Get(x => x.DatabaseID.ToString() == "1");
            Assert.That(oldCust1.CompanyName, Is.EqualTo("ABC"));

            Customer newCust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "XYZ" };
            customerCache.Set(newCust1);

            Customer result = customerCache.Get(x => x.DatabaseID.ToString() == "1");
            Assert.That(result.CompanyName, Is.EqualTo("XYZ"));
        }

        [Test]
        public async Task Add_and_remove_on_different_threads_maintains_consistency()
        {
            customerCache.Purge();

            Task t0 = Task.Run(() => {
                for (int i = 0; i < 1000; i++)
                {
                    Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
                    Customer cust2 = new Customer { DatabaseID = 2, FederalEIN = "20", SalesForceID = "200", CompanyName = "ABC" };
                    customerCache.Set(cust1);
                    customerCache.Set(cust2);
                    customerCache.Get(x => x.DatabaseID.ToString() == "1");
                    customerCache.Get(x => x.DatabaseID.ToString() == "2");
                    customerCache.Remove("1");
                    customerCache.Remove("2");
                }

            });


            Task t1 = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
                    Customer cust2 = new Customer { DatabaseID = 2, FederalEIN = "20", SalesForceID = "200", CompanyName = "ABC" };
                    customerCache.Set(cust1);
                    customerCache.Set(cust2);
                    customerCache.Get(x => x.DatabaseID.ToString() == "1");
                    customerCache.Get(x => x.DatabaseID.ToString() == "2");
                    customerCache.Remove("1");
                    customerCache.Remove("2");
                }

            });

            await Task.WhenAll(t1,t0);
            Assert.That(customerCache.KeyCount + customerCache.ObjectCount, Is.EqualTo(0));

        }
    }
}
