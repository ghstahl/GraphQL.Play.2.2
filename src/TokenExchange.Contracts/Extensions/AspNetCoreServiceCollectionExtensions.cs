﻿using Microsoft.Extensions.DependencyInjection;
using TokenExchange.Contracts.Services;
using TokenExchange.Contracts.Stores;

namespace TokenExchange.Contracts.Extensions
{
    public static class AspNetCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddTokenExchangeContracts(this IServiceCollection services)
        {
            services.AddTransient<OIDCTokenValidator>();
            services.AddTransient<IPrincipalEvaluatorRouter, PrincipalEvaluatorRouter>();
            return services;
        }
       
        public static IServiceCollection AddDemoCustomIdentityPrincipalEvaluator(this IServiceCollection services)
        {
            services.AddTransient<ITokenExchangeHandler, AlienCustomIdentityTokenExchangeHandler>();
            services.AddTransient<ITokenExchangeHandler, GoogleMyCustomIdentityTokenExchangeHandler>();
            return services;
        }
        public static IServiceCollection AddSelfIdentityPrincipalEvaluator(this IServiceCollection services)
        {
            services.AddTransient<ITokenExchangeHandler, SelfIdentityTokenExchangeHandler>();
            return services;
        }
        public static IServiceCollection AddInMemoryExternalExchangeStore(this IServiceCollection services)
        {
            services.AddTransient<ExternalExchangeTokenExchangeHandler>();
            services.AddSingleton<IExternalExchangeStore, InMemoryExternalExchangeStore>();
            return services;
        }
    }
}
