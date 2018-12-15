﻿using GraphQL.Types;

namespace CustomerLoyalyStore.GraphQL
{
    public class IdentityModelType : ObjectGraphType<Models.IdentityModel>
    {
        public IdentityModelType()
        {
            Name = "identity";
            Field<ListGraphType<ClaimModelType>>("claims", "The Claims of the identity");
        }
    }
}