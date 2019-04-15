﻿
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQLPlay.IdentityModelExtras.Extensions
{
    public static class AspNetCoreExtensions
    {
        public static void AddGraphQLPlayIdentityModelExtrasTypes(this IServiceCollection services)
        {
            services.TryAddTransient<IDefaultHttpClientFactory, NullDefaultHttpClientFactory>();
        }
        public static IServiceCollection AddInMemoryOAuth2ConfigurationStore(this IServiceCollection services)
        {
            services.AddSingleton<IOAuth2ConfigurationStore, InMemoryOAuth2ConfigurationStore>();
            return services;
        }
    }
}