﻿using System.Linq;
using Memstate;
 

namespace CustomerLoyaltyStore.Queries
{
    public class Top10Customers : Query<LoyaltyDB, Customer[]>
    {
        public override Customer[] Execute(LoyaltyDB db) => db.Customers
            .OrderByDescending(c => c.Value.LoyaltyPointBalance)
            .Take(10).Select(c => c.Value).ToArray();
    }
}