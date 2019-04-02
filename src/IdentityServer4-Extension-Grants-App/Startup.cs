using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using CustomerLoyaltyStore.Extensions;
using CustomerLoyalyStore.GraphQL.Extensions;
using DemoIdentityServerio.Validator.Extensions;
using Google.Validator.Extensions;
using IdentityModelExtras;
using IdentityModelExtras.Extensions;
using IdentityServer4ExtensionGrants.Rollup.Extensions;
using IdentityServerRequestTracker.Extensions;
using IdentityTokenExchangeGraphQL.Extensions;
using Memstate.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MultiAuthority.AccessTokenValidation;
using Norton.Validator.Extensions;
using Orders.Extensions;
using P7Core.BurnerGraphQL.Extensions;
using P7Core.BurnerGraphQL2.Extensions;
using P7Core.GraphQLCore.Extensions;
using P7Core.GraphQLCore.Stores;
using P7Core.ObjectContainers.Extensions;
using P7IdentityServer4.Validator.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using TokenExchange.Contracts.Extensions;
using TokenMintingService.Extensions;
using Utils.Extensions;

namespace IdentityServer4_Extension_Grants_App
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            _hostingEnvironment = env;
            Configuration = configuration;
        }

       

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddLazier();
            services.AddObjectContainer();  // use this vs a static to cache class data.
            services.AddOptions();
            services.AddMemoryCache();
            services.AddIdentityModelExtrasTypes();
            services.AddSingleton<ConfiguredDiscoverCacheContainerFactory>();
            services.AddGraphQLCoreTypes();
            services.AddGraphQLCoreCustomLoyaltyTypes();
            services.AddGraphQLOrders();
            services.AddBurnerGraphQL();
            services.AddBurnerGraphQL2();

            services.AddInMemoryOAuth2ConfigurationStore();

            services.AddGraphQLIdentityTokenExchangeTypes();
            services.AddP7IdentityServer4OIDCTokenValidator();
            services.AddDemoIdentityServerioOIDCTokenValidator();
            services.AddGoogleOIDCTokenValidator();
            services.AddGoogleMyCustomOIDCTokenValidator();
            services.AddNortonOIDCTokenValidator();
            services.TryAddSingleton<IGraphQLFieldAuthority, InMemoryGraphQLFieldAuthority>();
            services.RegisterGraphQLCoreConfigurationServices(Configuration);

            services.AddPrincipalEvaluatorRouter();
            services.AddGoogleIdentityPrincipalEvaluator();
            services.AddSelfIdentityPrincipalEvaluator();
            services.AddGoogleMyCustomIdentityPrincipalEvaluator();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    corsBuilder => corsBuilder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
            services.AddExtensionGrantsRollup(Configuration);
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

            var scheme = Configuration["authValidation:scheme"];

            var section = Configuration.GetSection("oauth2");
            var oAuth2SchemeRecords = new List<OAuth2SchemeRecord>();
            section.Bind(oAuth2SchemeRecords);
            var query = from item in oAuth2SchemeRecords
                        where item.Scheme == scheme
                        select item;
            var oAuth2SchemeRecord = query.FirstOrDefault();

            var authority = oAuth2SchemeRecord.Authority;
            List<SchemeRecord> schemeRecords = new List<SchemeRecord>()
            {  new SchemeRecord()
                {
                    Name = scheme,
                    JwtBearerOptions = options =>
                    {
                        options.Authority = authority;
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
                            OnMessageReceived = context =>
                            {
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {

                                ClaimsIdentity identity = context.Principal.Identity as ClaimsIdentity;
                                if (identity != null)
                                {
                                    // Add the access_token as a claim, as we may actually need it
                                    var accessToken = context.SecurityToken as JwtSecurityToken;
                                    if (accessToken != null)
                                    {
                                        if (identity != null)
                                        {
                                            identity.AddClaim(new Claim("access_token", accessToken.RawData));
                                        }
                                    }
                                }

                                return Task.CompletedTask;
                            }
                        };
                    }

                },
            };

            services.AddAuthentication("Bearer")
                .AddMultiAuthorityAuthentication(schemeRecords);

            services.AddInProcTokenMintingService();

            services.AddLogging();
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddCustomerLoyalty();
            // Build the intermediate service provider then return it
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            

            app.UseRewriter(new RewriteOptions().Add(new RewriteLowerCaseRule()));

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
            app.UseAuthentication();

            app.UseMvc();

            //MEMSTATE Journal stays in memory
            Config.Current.UseInMemoryFileSystem();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

        }
    }
}
