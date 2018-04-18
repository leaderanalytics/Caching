using System;
using System.Collections.Generic;
using System.Text;

namespace LeaderAnalytics.Caching.Tests
{
    public class Customer
    {
        public int DatabaseID { get; set; }
        public string FederalEIN { get; set; }
        public string SalesForceID { get; set; }
        public string CompanyName { get; set; }
    }
}
