using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AppIdentity.Extensions;
using AppIdentity.Models;
using ArbitraryIdentityExtensionGrant;
using AuthRequiredDemoGraphQL.Extensions;
using Cosmonaut;
using DiscoveryHub.Extensions;
using GraphQLPlay.IdentityModelExtras;
using GraphQLPlay.IdentityModelExtras.Extensions;
using GraphQLPlayTokenExchangeOnlyApp.Filter;
using IdentityServer4.Configuration;
using IdentityServer4.Contrib.Cosmonaut.Extensions;
using IdentityServer4ExtensionGrants.Rollup.Extensions;
using IdentityServer4Extras.Extensions;
using IdentityServerRequestTracker.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MultiAuthority.AccessTokenValidation;
using P7Core.Extensions;
using P7Core.GraphQLCore.Extensions;
using P7Core.GraphQLCore.Stores;
using P7Core.ObjectContainers.Extensions;
using P7IdentityServer4.Extensions;
using Self.Validator.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using TokenExchange.Contracts;
using TokenExchange.Contracts.Extensions;
using TokenExchange.Contracts.Services;
using TokenExchange.Contracts.Stores;
using static GraphQLPlay.Rollup.Extensions.AspNetCoreExtensions;
using static TokenExchange.Rollup.Extensions.AspNetCoreExtensions;

namespace GraphQLPlayTokenExchangeOnlyApp
{
    class OIDCSchemeEnabled
    {
        public bool Enabled { get; set; }
        public string Scheme { get; set; }
    }
    public class Startup :
        IExtensionGrantsRollupRegistrations,
        IGraphQLRollupRegistrations,
        ITokenExchangeRegistrations
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public IConfiguration Configuration { get; }

        private ILogger<Startup> _logger;

        public Startup(IHostingEnvironment env, IConfiguration configuration, ILogger<Startup> logger)
        {
            _hostingEnvironment = env;
            Configuration = configuration;
            _logger = logger;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            services.AddLogging();
            services.AddObjectContainer();  // use this vs a static to cache class data.
            services.AddOptions();
            services.Configure<ArbitraryIdentityExtensionGrantOptions>(options => { options.IdentityProvider = "Demo"; });

            services.AddDistributedMemoryCache();
            services.AddGraphQLPlayRollup(this);
            services.AddExtensionGrantsRollup(this);
            services.AddGraphQLDiscoveryTypes();
            services.AddInMemoryDiscoveryHubStore();
            services.AddGraphQLAuthRequiredQuery();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    corsBuilder => corsBuilder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Daffy Duck",
                    policy => { policy.RequireClaim("client_namespace", "Daffy Duck"); });
            });

            //  var scheme = Configuration["authValidation:scheme"];
            var schemes = Configuration
                .GetSection("authValidation:schemes")
                .Get<List<string>>();

            var section = Configuration.GetSection("InMemoryOAuth2ConfigurationStore:oauth2");
            var oauth2Section = new Oauth2Section();
            section.Bind(oauth2Section);

            var schemeRecords = SchemeRecords(oauth2Section, schemes);

            services.AddAuthentication("Bearer")
                .AddMultiAuthorityAuthentication(schemeRecords);

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.TryAddTransient<IDefaultHttpClientFactory, DefaultHttpClientFactory>();

            // Build the intermediate service provider then return it
            services.AddSwaggerGen(config =>
            {
                config.SwaggerDoc("v1", new Info { Title = "GraphQLPlayTokenExchangeOnlyApp", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                config.IncludeXmlComments(xmlPath);
                config.OperationFilter<MultiAuthorityOperationFilter>();
            });
            services.AddInMemoryAppIdentityConfiguration(new AppIdentityConfigurationModel()
            {
                MaxAppIdLength = Guid.NewGuid().ToString().Length * 2,
                MaxMachineIdLength = Guid.NewGuid().ToString().Length * 2,
                MaxSubjectLength = Guid.NewGuid().ToString().Length * 2
            });
            return services.BuildServiceProvider();

        }

        private static List<SchemeRecord> SchemeRecords(Oauth2Section oauth2Section, List<string> schemes)
        {
            var authSchemes = oauth2Section.Authorities.Where(c => schemes.Any(c2 => c2 == c.Scheme));

            List<SchemeRecord> schemeRecords = new List<SchemeRecord>();
            foreach (var authScheme in authSchemes)
            {
                Func<TokenValidatedContext, Task> tokenValidationHandler = context =>
                {
                    ClaimsIdentity identity = context.Principal.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Add the access_token as a claim, as we may actually need it
                        var accessToken = context.SecurityToken as JwtSecurityToken;
                        if (accessToken != null)
                        {
                            identity.AddClaim(new Claim("access_token", accessToken.RawData));
                        }
                    }
                    return Task.CompletedTask;
                };

                var schemeRecord = new SchemeRecord()
                {
                    Name = authScheme.Scheme,
                    JwtBearerOptions = options =>
                    {
                        options.Authority = authScheme.Authority;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true
                        };
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context => Task.CompletedTask,
                            OnTokenValidated = tokenValidationHandler
                        };
                    }
                };
                schemeRecords.Add(schemeRecord);
            }

            return schemeRecords;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseCors("CorsPolicy");
            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseIdentityServerRequestTrackerMiddleware();
            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GraphQLPlayTokenExchangeOnlyApp V1");
            });
        }

        public void AddIdentityResources(IServiceCollection services, IIdentityServerBuilder builder)
        {
            _logger.LogInformation("AddIdentityResources to services");
            var identityResources = Configuration.LoadIdentityResourcesFromSettings();
            builder.AddInMemoryIdentityResources(identityResources);
        }

        public void AddClients(IServiceCollection services, IIdentityServerBuilder builder)
        {
            _logger.LogInformation("AddClients to services");
            var clients = Configuration.LoadClientsFromSettings();
            builder.AddInMemoryClientsExtra(clients);
        }

        public void AddApiResources(IServiceCollection services, IIdentityServerBuilder builder)
        {
            _logger.LogInformation("AddApiResources to services");
            var apiResources = Configuration.LoadApiResourcesFromSettings();
            builder.AddInMemoryApiResources(apiResources);
        }

        private readonly ConnectionPolicy _connectionPolicy = new ConnectionPolicy
        {
            ConnectionProtocol = Protocol.Tcp,
            ConnectionMode = ConnectionMode.Direct
        };
        public void AddOperationalStore(IServiceCollection services, IIdentityServerBuilder builder)
        {
            bool useRedis = Convert.ToBoolean(Configuration["appOptions:redis:useRedis"]);
            bool useCosmos = Convert.ToBoolean(Configuration["appOptions:cosmos:useCosmos"]);
            if (useCosmos)
            {
                /*
                 *
                 "identityServerOperationalStore": {
                    "database": "identityServer",
                    "collection": "operational"
                  }
                 */
                var uri = Configuration["appOptions:cosmos:uri"];
                var primaryKey = Configuration["appOptions:cosmos:primaryKey"];
                var databaseName = Configuration["appOptions:cosmos:identityServerOperationalStore:database"];
                var collection = Configuration["appOptions:cosmos:identityServerOperationalStore:collection"];
                var cosmosStoreSettings = new CosmosStoreSettings(
                    databaseName,
                    uri,
                    primaryKey,
                    s =>
                    {
                        s.ConnectionPolicy = _connectionPolicy;
                    });
                builder.AddOperationalStore(cosmosStoreSettings, collection);
                builder.AddCosmonautIdentityServerCacheStore(cosmosStoreSettings, collection);
            }
            else if (useRedis)
            {
                var redisConnectionString = Configuration["appOptions:redis:redisConnectionString"];
                builder.AddOperationalStore(options =>
                {
                    options.RedisConnectionString = redisConnectionString;
                    options.Db = 1;
                })
                .AddRedisCaching(options =>
                {
                    options.RedisConnectionString = redisConnectionString;
                    options.KeyPrefix = "prefix";
                });

                services.AddDistributedRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }
            else
            {
                builder.AddInMemoryCaching();
                builder.AddInMemoryPersistedGrants();
                services.AddDistributedMemoryCache();
            }
        }


        public void AddSigningServices(IServiceCollection services, IIdentityServerBuilder builder)
        {
            _logger.LogInformation("AddSigningServices to services");

            bool useKeyVault = Convert.ToBoolean(Configuration["appOptions:keyVault:useKeyVault"]);
            bool useKeyVaultSigning = Convert.ToBoolean(Configuration["appOptions:keyVault:useKeyVaultSigning"]);

            _logger.LogInformation($"AddSigningServices:useKeyVault:{useKeyVault}");
            _logger.LogInformation($"AddSigningServices:useKeyVaultSigning:{useKeyVaultSigning}");

            if (useKeyVault)
            {
                builder.AddKeyVaultCredentialStore();
                services.AddKeyVaultTokenCreateServiceTypes();
                services.AddKeyVaultTokenCreateServiceConfiguration(Configuration);
                if (useKeyVaultSigning)
                {
                    // this signs the token using azure keyvault to do the actual signing
                    builder.AddKeyVaultTokenCreateService();
                }
            }
            else
            {
                _logger.LogInformation("AddSigningServices AddDeveloperSigningCredential");
                builder.AddDeveloperSigningCredential();
            }
        }

        public void AddGraphQLFieldAuthority(IServiceCollection services)
        {
            services.TryAddSingleton<IGraphQLFieldAuthority, InMemoryGraphQLFieldAuthority>();
            services.RegisterGraphQLCoreConfigurationServices(Configuration);
        }

        public void AddTokenValidators(IServiceCollection services)
        {
            services.AddInMemoryOAuth2ConfigurationStore();

            services.AddSelfTokenExchangeHandler();
            services.AddDemoTokenExchangeHandlers();

            services.AddSelfOIDCTokenValidator();
            var schemes = Configuration
                .GetSection("oidcSchemes")
                .Get<List<OIDCSchemeEnabled>>();
           
            foreach (var scheme in schemes)
            {
                if (scheme.Enabled)
                {
                    services.AddSingleton<ISchemeTokenValidator>(x =>
                    {
                        var oidcTokenValidator = x.GetRequiredService<OIDCTokenValidator>();
                        oidcTokenValidator.TokenScheme = scheme.Scheme;
                        return oidcTokenValidator;
                    });
                }
            }

            services.AddInMemoryExternalExchangeStore();
            var tempExternalExchangeStore = InMemoryExternalExchangeStore.MakeStore(Configuration);
            ExternalExchangeTokenExchangeHandler.RegisterServices(services, tempExternalExchangeStore);

            services.AddInMemoryPipelineExchangeStore();
            var pipelineExchangeStore = InMemoryPipelineExchangeStore.MakeStore(Configuration);
            PipelineTokenExchangeHandler.RegisterServices(services, pipelineExchangeStore);
        }

        public void AddGraphQLApis(IServiceCollection services)
        {
            // APIS
            services.AddTokenExchangeRollup(this);
            services.AddGraphQLAppIdentityTypes();
        }

        public Action<IdentityServerOptions> GetIdentityServerOptions()
        {
            Action<IdentityServerOptions> identityServerOptions = options =>
            {
                options.InputLengthRestrictions.RefreshToken = 256;
                options.Caching.ClientStoreExpiration = TimeSpan.FromMinutes(1);
            };
            return identityServerOptions;
        }
    }
}
