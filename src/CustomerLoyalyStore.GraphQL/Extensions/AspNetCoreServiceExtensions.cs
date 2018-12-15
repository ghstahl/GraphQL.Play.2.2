﻿using System;
using System.Collections.Generic;
using System.Text;
using CustomerLoyalyStore.GraphQL.Mutation;
using CustomerLoyalyStore.GraphQL.Query;
using Microsoft.Extensions.DependencyInjection;
using P7.GraphQLCore;

namespace CustomerLoyalyStore.GraphQL.Extensions
{
    public static class AspNetCoreServiceExtensions
    {
        public static void AddGraphQLCoreCustomLoyaltyTypes(this IServiceCollection services)
        {
            services.AddTransient<IdentityModelType>();
            services.AddTransient<ClaimModelType>();
          
            services.AddTransient<IQueryFieldRecordRegistration, AuthRequiredQuery>();

            services.AddTransient<CustomerMutationInput>();
            services.AddTransient<CustomerType>();
            services.AddTransient<IMutationFieldRecordRegistration, CustomerMutation>();

        }
    }
}
