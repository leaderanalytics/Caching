# LeaderAnalytics.Caching

4/17/2018

## MultiIndexCache

Thread safe cache that allows object retrieval using multiple indexes.  



    public class Customer
    {
        public int DatabaseID { get; set; }
        public string FederalEIN { get; set; }
        public string SalesForceID { get; set; }
        public string CompanyName { get; set; }
    }


Create a multi index cache for customer objects defining DatabaseID and SalesForceID as indexes:

    
    customerCache = new MultiIndexCache<Customer>(x => x.DatabaseID.ToString(), x => x.SalesForceID);


Add a customer to the cache:

    Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
    customerCache.Set(cust1);


Find a customer by index expression:

    Customer result1 = customerCache.Get(x => x.DatabaseID.ToString() == "10");
    Customer result2 = customerCache.Get(x => x.SalesFroceID == "100");


Find a customer by index key (faster):

    // Search the first index (DatabaseID)
    Customer result1 = customerCache.Get(0, "10");

    // Search the second index (SalesForceID)
    Customer result2 = customerCache.Get(1, "100");


Remove a customer object from the cache:

    customerCache.Remove("10");
    // or
    customerCache.Remove(cust1);


Note:  Properties used as keys for MultiIndexCache must be treated as immutable.

## Cache

Thread safe cache backed by `ConcurrentDictionary`.

Supports the following eviction strategies:

* Time since add - evicts based on time elapsed since object was added to cache.
* Time since get - evicts based on time elapsed since object was last accessed.

Supports global enabling/disabling via CacheManager master switch:

    Cache<Product> productCache = new Cache<Product>();
    Cache<Customer> customerCache = new Cache<Customer>();

    // disable only productCache:
    productCache.IsEnabled = false;    

    // disable all caches:
    CacheManager.EnableCaching = false;

     
Create a cache for customer objects:

    Cache<Customer> customerCache = new Cache<Customer>();

Add a customer to the cache:

    Customer cust1 = new Customer { DatabaseID = 1, FederalEIN = "10", SalesForceID = "100", CompanyName = "ABC" };
    customerCache.Set(cust1.DatabaseID.ToString(), cust1);

Seed the cache with a collection:

    IEnumerable<KeyValuePair<string, Customer>> customers = db.Customers.Select( x => new KeyValuePair<string, Customer>(x.ID.ToString(), x));
    customerCache.Seed(customers);

Get an object from the cache:

    Customer customer = customerCache.Get("5");

Remove an object from the cache:

    customerCache.Remove("5");

Remove all objects from the cache:

    customerCache.Purge();

Manually invoke selected eviction strategy, if any:

    customerCache.Evict();
