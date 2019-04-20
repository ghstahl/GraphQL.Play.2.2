﻿using Microsoft.Extensions.DependencyInjection;
using TokenExchange.Contracts.Services;
using TokenExchange.Contracts.Stores;

namespace TokenExchange.Contracts.Extensions
{
    public static class AspNetCoreServiceCollectionExtensions
    {

        public static IServiceCollection AddTokenExchangeContracts(this IServiceCollection services)
        {
            services.AddTransient<ITokenExchangeHandlerPreProcessor, ValidateAndStripSignatureTokenExchangeHandlerPreProcessor>();
            services.AddTransient<ITokenExchangeHandlerPreProcessor, StripSignatureTokenExchangeHandlerPreProcessor>();
            services.AddTransient<ITokenExchangeHandlerPreProcessorStore, TokenExchangeHandlerPreProcessorStore>();
            services.AddTransient<OIDCTokenValidator>();
            services.AddTransient<ITokenExchangeHandlerRouter, TokenExchangeHandlerRouter>();
            return services;
        }
        public static IServiceCollection AddDemoTokenExchangeHandlers(this IServiceCollection services)
        {
            services.AddTransient<ITokenExchangeHandler, AlienCustomIdentityTokenExchangeHandler>();
            services.AddTransient<ITokenExchangeHandler, GoogleMyCustomIdentityTokenExchangeHandler>();
            return services;
        }
        public static IServiceCollection AddSelfTokenExchangeHandler(this IServiceCollection services)
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
        public static IServiceCollection AddInMemoryPipelineExchangeStore(this IServiceCollection services)
        {
            services.AddTransient<PipelineTokenExchangeHandler>();
            services.AddSingleton<IPipelineExchangeStore, InMemoryPipelineExchangeStore>();
            return services;
        }

    }
}
