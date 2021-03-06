﻿using System.Collections.Generic;
using System.Text;
using GraphQL.Types;

namespace CustomerLoyalyStore.GraphQL.Mutation
{
    public class CustomerMutationInput : InputObjectGraphType
    {
        public CustomerMutationInput()
        {
            Name = "customerMutationInput";
            Field<NonNullGraphType<StringGraphType>>("ID");
            Field<NonNullGraphType<IntGraphType>>("LoyaltyPointBalance");

        }
    }
}
