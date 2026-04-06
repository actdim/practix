
using ActDim.Practix.Service.OpenApi;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using SmartFormat;
using Swashbuckle.AspNetCore.Swagger;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// using ActDim.Practix.Net.WebSockets;

namespace ActDim.Practix.Service
{
    /// <summary>
    /// ServiceBase
    /// </summary>
    public class CoreService
    {
        private static readonly string[] ConfigVarPrefixes = ["ASPNETCORE_", "DOTNET_"]; // Add app specific prefixes

        private readonly IHost _host;

        public IHost Host
        {
            get { return _host; }
        }

        private readonly IConfiguration _configuration;

        private IServiceProvider _serviceProvider = default;

        private readonly Lock _syncRoot = new();

        private readonly IModule[] _modules;

        public CoreService(string[] args,
            IEnumerable<IModule> modules = default,
            // Action<Action, IConfigurationBuilder> configureAppConfiguration = default,
            Action<Action, ILoggingBuilder, Func<IServiceProvider>> configureLogging = default,
            Action<Action, IServiceCollection, Func<IServiceProvider>> configureServices = default,
            Action<Action, ContainerBuilder, Func<IServiceProvider>> configureContainer = default,
            Action<Action, IWebHostBuilder, Func<IServiceProvider>> configureWebHost = default,
            Action<JsonOptions> configureJsonOptions = default)
        {
            // old way:

            // new CommonModule()
            _modules = [.. (modules ?? [])];

            _configuration = InitConfiguration(new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()), GetEnvName(), args).Build();

            var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);

            hostBuilder.ConfigureAppConfiguration((builderContext, configBuilder) =>
            {
                var config = builderContext.Configuration;
                // var isApiGeneration = IsApiGeneration(config);
                ConfigureAppConfiguration(configBuilder, builderContext.HostingEnvironment.EnvironmentName ?? GetEnvName(config), args);
            });

            // under the hood:
            // var logFactory = LoggerFactory.Create(logBuilder => { });

            hostBuilder.ConfigureLogging((builderContext, logBuilder) =>
            {
                var serviceProviderFactory = () => GetOrUpdateServiceProvider(() => CreateServiceProvider(logBuilder.Services), true);

                void @default() => ConfigureLogging(logBuilder);
                if (configureLogging != default)
                {
                    configureLogging(@default,
                        logBuilder,
                        serviceProviderFactory);
                }
                else
                {
                    @default();
                }
            });

            hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            hostBuilder.ConfigureServices((builderContext, services) =>
            {
                // builderContext.Configuration
                // services.Configure<AppSettings>(_configuration);

                // lazy configuration
                services.AddOptions<AppSettings>()
                    .Configure<IConfiguration>((obj, config) =>
                        config.Bind(obj));

                // services.AddOptions<AppSettings>()
                //    .Configure<IConfiguration>((obj, config) =>
                //        config.GetSection("App").Bind(obj));

                // see also: IOptionsSnapshot, IOptionsMonitor

                /*
                public class MySettingsSetup : IConfigureOptions<MySettings>
                {
                    public void Configure(MySettings options)
                    {
                        options.Setting1 = "String";
                        options.Setting2 = 123;
                    }
                }
                services.AddSingleton<IConfigureOptions<MySettings>, MySettingsSetup>();
                services.AddOptions<MySettings>()
                    .Bind(_configuration.GetSection("MySettings"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                // IOptionsMonitor<MySettings> settingsMonitor
                settingsMonitor.OnChange(settings =>
                {                    
                });
                */

                var serviceProviderFactory = () => GetOrUpdateServiceProvider(() => CreateServiceProvider(services), false);

                void @default() => ConfigureServices(services, serviceProviderFactory, configureJsonOptions ?? ConfigureJsonOptions);
                if (configureServices != default)
                {
                    configureServices(@default,
                        services,
                        serviceProviderFactory
                        );
                }
                else
                {
                    @default();
                }

                // GetOrUpdateServiceProvider(() => CreateServiceProvider(services), true);
            });

            hostBuilder.ConfigureContainer<ContainerBuilder>((builderContext, containerBuilder) =>
            {
                var serviceProviderFactory = () => ServiceProvider;

                void @default() => ConfigureContainer(containerBuilder);
                if (configureContainer != default)
                {
                    configureContainer(@default,
                        containerBuilder,
                        serviceProviderFactory
                        );
                }
                else
                {
                    @default();
                }
            });

            hostBuilder.ConfigureHostOptions((builderContext, hostOptions) =>
            {

            });

            // ConfigureWebHost
            hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
            {
                var serviceProviderFactory = () =>
                {
                    var serviceCollection = new ServiceCollection();
                    serviceCollection.Configure<AppSettings>(_configuration);
                    // serviceCollection.Configure<AppSettings>(builderContext.Configuration.GetSection("AppSettings"));

                    return GetOrUpdateServiceProvider(() => CreateServiceProvider(serviceCollection, containerBuilder =>
                    {
                        containerBuilder.RegisterInstance(_configuration).As<IConfiguration>();
                    }), true);
                };

                void @default() => ConfigureWebHost(webHostBuilder, serviceProviderFactory);
                if (configureWebHost != default)
                {
                    configureWebHost(@default, webHostBuilder, serviceProviderFactory);
                }
                else
                {
                    @default();
                }
            });

            var host = hostBuilder.Build();

            GetOrUpdateServiceProvider(() => host.Services, true);

            // var config = host.Services.GetRequiredService<IConfiguration>();
            // var env = host.Services.GetRequiredService<IWebHostEnvironment>();

            _host = host;

            /*
            // new way
            var options = new WebApplicationOptions()
            {
                Args = args,
                // ContentRootPath = GetContentRoot(config),
                // WebRootPath = GetWebRoot(config)
            };

            var builder = WebApplication.CreateBuilder(options);

            // var config = builder.Configuration;

            var serviceProvider = GetOrUpdateServiceProvider(() => CreateServiceProvider(builder.Services), false);

            ConfigureAppConfiguration(builder.Configuration, builder.Environment.EnvironmentName, args);

            ConfigureLogging(builder.Logging, config, serviceProvider); // instead of builder.Host.ConfigureLogging or builder.WebHost.ConfigureLogging            

            builder.Host.UseServiceProviderFactory(serviceProviderFactory);

            ConfigureServices(builder.Services, config, serviceProvider, defaultApiVersion);

            builder.Host.ConfigureContainer<ContainerBuilder>((builderContext, containerBuilder) =>
            {
                ConfigureContainer(containerBuilder, modules, builderContext.Configuration);
            });

            // builder.Host.ConfigureWebHostDefaults(hostBuilder =>
            // {
            //     ConfigureWebHost(hostBuilder, config);
            // });

            ConfigureWebHost(builder.WebHost, config);

            var app = builder.Build();

            // var url = GetUrl(config);
            // app.Urls.Add(url);

            ConfigureApp(app, config);
            app.MapControllers();

            Host = app;
            */

            // use "dotnet run --no-launch-profile" to explicitly disable using the launchSettings.json

            /*
                <PropertyGroup>
                    <PostBuildEvent>xcopy "$(ProjectDir)Xml" "$(ProjectDir)$(OutDir)Xml" /S /F /I /R /Y</PostBuildEvent>
                </PropertyGroup>
            */
        }

        private static void ConfigureJsonOptions(JsonOptions jsonOptions)
        {
            var options = jsonOptions.JsonSerializerOptions;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.WriteIndented = false;
            // options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            // options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        }

        private IServiceProvider GetOrUpdateServiceProvider(Func<IServiceProvider> factory = default, bool forceUpdate = false)
        {
            lock (_syncRoot)
            {
                if (factory != default && (_serviceProvider == default || forceUpdate))
                {
                    _serviceProvider = factory();
                }
                return _serviceProvider;
            }
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                lock (_syncRoot)
                {
                    return _serviceProvider;
                }
            }
        }

        private AutofacServiceProvider CreateServiceProvider(IServiceCollection services = default, Action<ContainerBuilder> setup = null)
        {
            var containerBuilder = new ContainerBuilder();
            if (services != default)
            {
                containerBuilder.Populate(services); // important for logging!
            }
            ConfigureContainer(containerBuilder);
            if (setup != default)
            {
                setup(containerBuilder);
            }
            var container = containerBuilder.Build();
            return new AutofacServiceProvider(container);
        }

        protected bool IsDevelopment(string env)
        {
            return string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase);
        }

        protected T GetConfigValue<T>(Func<string, T> select, string name) where T : IConvertible
        {
            return GetConfigValue(select, name, ConfigVarPrefixes);
        }

        protected T GetConfigValue<T>(Func<string, T> select, string name, IEnumerable<string> prefixes) where T : IConvertible
        {
            var keys = new List<string>();
            if (prefixes == default)
            {
                keys.AddRange(prefixes.Select(prefix => $"{prefix}{name}"));
            }
            keys.Add(name);
            foreach (var key in keys)
            {
                var value = select(key);
                if (!EqualityComparer<T>.Default.Equals(value, default))
                {
                    return value;
                }
            }
            return default;
        }

        protected string GetEnvName(IConfiguration config = default)
        {
            if (config == default)
            {
                return GetConfigValue(key => Environment.GetEnvironmentVariable(key), WebHostDefaults.EnvironmentKey);
            }

            // config.GetValue<string>(key)
            return GetConfigValue(key => config[key], WebHostDefaults.EnvironmentKey);
        }

        protected void ConfigureAppConfiguration(IConfigurationBuilder configBuilder, string env, string[] args = default)
        {
            InitConfiguration(configBuilder, env, args);
        }

        protected virtual IConfigurationBuilder InitConfiguration(IConfigurationBuilder configBuilder, string env, string[] args = default)
        {
            // configBuilder.Sources.Clear();

            DotNetEnv.Env.Load();
            configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configBuilder.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

            foreach (var prefix in ConfigVarPrefixes)
            {
                configBuilder.AddEnvironmentVariables(prefix: prefix);
            }

            configBuilder.AddEnvironmentVariables();
            configBuilder.AddCommandLine(args);

            return configBuilder;
        }

        private void ConfigureContainer(ContainerBuilder builder)
        {
            if (_modules != default)
            {
                foreach (var module in _modules)
                {
                    builder.RegisterModule(module);
                }
            }

            // var assemblies = DirectoryExtensions.LoadAssemblies(Directory.GetCurrentDirectory()).ToArray();
            // var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // builder.RegisterAssemblyModules(assemblies);

            // builder.RegisterAssemblyTypes(assemblies)
            //    // .AsImplementedInterfaces()
            //    .AsSelf()
            //    .InstancePerDependency();
        }

        protected virtual void ConfigureLogging(ILoggingBuilder logBuilder)
        {
            logBuilder.ClearProviders();

            // logBuilder.Services.Configure<LoggerFactoryOptions>(options =>
            // {
            //     options.ActivityTrackingOptions = ActivityTrackingOptions.None;
            // });

            // DiagnosticListener.AllListeners.Subscribe
            // if (listener.Name == "Microsoft.AspNetCore")
            // {
            //     listener.Dispose();
            // }
        }

        protected virtual void ConfigureServices(IServiceCollection services,
            Func<IServiceProvider> serviceProviderFactory,
            Action<JsonOptions> configureJsonOptions)
        {
            var appOptions = serviceProviderFactory().GetRequiredService<IOptions<AppSettings>>();
            var appSettings = appOptions.Value;

            // we configure logging before configuring services
            // services.AddLogging(logBuilder =>
            // {
            // 
            // });

            services.AddResponseCompression(options =>
            {
                // options.EnableForHttps = true;
                // options.Providers.Clear();
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.AddHttpClient();
            services.AddHttpContextAccessor();

            services.AddMemoryCache();

            services.AddDistributedMemoryCache();

            // chrome.exe --user-data-dir="C://chrome-dev-disabled-security" --disable-web-security --disable-site-isolation-trials

            services.AddCors(options =>
            {
                // by default - same origin allowed
                // options.AddDefaultPolicy(...)
                options.AddPolicy(CorsPolicies.Unrestricted, corsPolicyBuilder =>
                {
                    corsPolicyBuilder
                        .SetIsOriginAllowed(_ => true)
                        // .WithOrigins("*")
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                    // .AllowCredentials()
                    // .WithExposedHeaders("content-disposition", "content-type")
                    // .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                    ;
                });
                options.AddPolicy(CorsPolicies.FromSettings, corsPolicyBuilder =>
                {
                    // TODO: implement
                    // corsPolicyBuilder
                    // .SetIsOriginAllowed(origin => IsOriginAllowed(origin, builder.Environment))
                    // .WithOrigins(...)
                    // .WithHeaders(...)
                    // .WithMethods(...) 
                    // .AllowCredentials()
                    ;
                });
            });

            if (appSettings.AuthSchemes?.Count > 0)
            {
                // var config = serviceProviderFactory().GetRequiredService<IConfiguration>();
                // var jwtBearerConfig = config.GetSection("AuthSchemes:Default:JwtBearer").Get<JwtBearerConfig>();

                // var defaultSchemeName = JwtBearerDefaults.AuthenticationScheme;
                var defaultSchemeName = appSettings.AuthSchemes.First(x => x.Value.JwtBearer != default).Key;
                var authenticationBuilder = services.AddAuthentication(authenticationOptions =>
                {
                    authenticationOptions.DefaultAuthenticateScheme = defaultSchemeName;
                    authenticationOptions.DefaultChallengeScheme = defaultSchemeName;
                    authenticationOptions.DefaultScheme = defaultSchemeName;
                });

                foreach (var authScheme in appSettings.AuthSchemes)
                {
                    if (authScheme.Value.JwtBearer != default)
                    {
                        var jwtBearerConfig = authScheme.Value.JwtBearer;
                        authenticationBuilder.AddJwtBearer(authScheme.Key, jwtBearerOptions =>
                        {
                            // jwtBearerOptions.EventsType?
                            jwtBearerOptions.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                            {
                                OnMessageReceived = context =>
                                {
                                    // resolve token manually
                                    var accessToken = context.Request.Query["access_token"];
                                    if (!string.IsNullOrEmpty(accessToken))
                                    {
                                        context.Token = accessToken;
                                    }
                                    return Task.CompletedTask;
                                },
                                OnTokenValidated = context =>
                                {
                                    // add claims and logs
                                    return Task.CompletedTask;
                                },
                                OnAuthenticationFailed = context =>
                                {
                                    // log
                                    return Task.CompletedTask;
                                }
                            };

                            jwtBearerOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                            {
                                ValidIssuer = jwtBearerConfig.Issuer,
                                ValidAudience = jwtBearerConfig.Audience,
                                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBearerConfig.Key)),
                                // LifetimeValidator = 
                                // ClockSkew = 
                                // ValidateIssuer = true,
                                // ValidateAudience = true,
                                // ValidateLifetime = true,
                                // ValidateIssuerSigningKey = true,
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = false,
                                ValidateIssuerSigningKey = false
                            };
                        });
                    }
                }

                // services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);
                // AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
            }

            services.AddAuthorization(authorizationOptions =>
            {
                // authorizationOptions.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
                // authorizationOptions.DefaultPolicy = new AuthorizationPolicyBuilder("default").RequireAuthenticatedUser().Build();
            });

            // full MVC support (controllers, views, bindings, Razor Pages)
            // var mvcBuilder = services.AddMvc(options =>
            // {
            // });

            // services.AddControllersWithViews(); // without Razor Pages
            // services.AddRazorPages(); // Razor Pages only

            // WEB API

            services.ConfigureHttpJsonOptions(jsonOptions =>
            {

            });

            var mvcBuilder = services.AddControllers(options =>
            {
                // options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.XmlSerializerOutputFormatter>();

                // options.Conventions
                options.AllowEmptyInputInBodyModelBinding = true;
                options.EnableEndpointRouting = true;
                // options.RespectBrowserAcceptHeader = false;

                /*
                options.CacheProfiles.Add(CacheProfileNames.Disabled, new CacheProfile()
                {
                    Location = ResponseCacheLocation.None,
                    NoStore = true
                });
                options.CacheProfiles.Add(CacheProfileNames.Hourly, new CacheProfile()
                {
                    Duration = 60 * 60,
                    Location = ResponseCacheLocation.Any,
                    NoStore = false
                    // VaryByHeader = Microsoft.Net.Http.Headers.HeaderNames.AcceptEncoding
                    // VaryByQueryKeys = new[] { "*" }
                });
                options.CacheProfiles.Add(CacheProfileNames.Dayly, new CacheProfile()
                {
                    Duration = 60 * 60 * 24,
                    Location = ResponseCacheLocation.Any,
                    NoStore = false
                });
                options.CacheProfiles.Add(CacheProfileNames.Weekly, new CacheProfile()
                {
                    Duration = 60 * 60 * 24 * 7,
                    Location = ResponseCacheLocation.Any,
                    NoStore = false
                });
                */
            });

            // AddApplicationParts(mvcBuilder.PartManager);

            mvcBuilder.ConfigureApplicationPartManager(partManager =>
            {
                // manager.FeatureProviders.Clear();
                AddApplicationParts(partManager);
            });

            mvcBuilder.AddControllersAsServices();

            // mvcBuilder.Services.Configure<MvcOptions>(options =>
            // {
            //     options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.XmlSerializerOutputFormatter>();
            // });

            mvcBuilder.AddJsonOptions(jsonOptions =>
            {
                configureJsonOptions(jsonOptions);
            });

            // mvcBuilder.AddXmlSerializerFormatters();

            // System.Text.Json serializer settings
            // mvcBuilder.AddJsonOptions(options =>
            // {
            //     options.JsonSerializerOptions.PropertyNamingPolicy = null;
            //     options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            // });

            // foreach (var type in ApiExtensions.GetControllerTypes())
            // {
            //     // services.AddScoped(type);
            //     services.AddTransient(type);
            // }

            /*
            public class ClientBasedApiVersionSelector : IApiVersionSelector
            {
                public ApiVersion SelectVersion(
                    ApiVersioningOptions options,
                    HttpRequest request,
                    IReadOnlyList<ApiVersion> availableVersions)
                {
                    if (availableVersions.Count == 0)
                        return options.DefaultApiVersion;

                    var userAgent = request.Headers["User-Agent"].ToString().ToLower();

                    if (userAgent.Contains("mobile"))
                    {
                        return availableVersions.FirstOrDefault(v => v.MajorVersion == 2) ?? availableVersions.Max();
                    }

                    return availableVersions.Max();
                }
            }
            */

            // TODO: use custom ApiVersionSelector
            var defaultApiVersion = new ApiVersion(1, 0);
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = defaultApiVersion;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader("version"),
                    new HeaderApiVersionReader(OpenApiInfoExtensions.ApiVersion, "api-version")
                // new MediaTypeApiVersionReader("v")
                );
                // HttpContext.GetRequestedApiVersion();
                // options.UnsupportedApiVersionStatusCode
                // options.ApiVersionSelector 
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = defaultApiVersion;
                options.AddApiVersionParametersWhenVersionNeutral = true;

                // options.ApiVersionSelector
                // options.ApiVersionParameterSource
                // options.DefaultApiVersionParameterDescription

            });

            services.AddApiGen(serviceProviderFactory);

            // TODO: OpenTelemetry, Jaeger (Zipkin, Honeycomb)
            // dotnet add package OpenTelemetry.Extensions.Hosting
            // dotnet add package OpenTelemetry.Exporter.Console
            // dotnet add package OpenTelemetry.Instrumentation.AspNetCore
            // dotnet add package OpenTelemetry.Instrumentation.Http
            // dotnet add package OpenTelemetry.Exporter.Jaeger
            // services.AddOpenTelemetry()
            //    .WithTracing(tracing =>
            //    {
            //        tracing
            //            .AddAspNetCoreInstrumentation()
            //            .AddHttpClientInstrumentation()
            //            .AddConsoleExporter()
            //            .AddJaegerExporter(options =>
            //                {
            //                    options.AgentHost = "localhost";
            //                    options.AgentPort = 6831;
            //                });
            //    });
        }

        private void AddApplicationParts(ApplicationPartManager partManager)
        {
            // var assemblies = Directory.GetFiles(AppContext.BaseDirectory, "*.dll").Select(Assembly.LoadFrom);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                assembly.GetTypes().Any(type =>
                    typeof(ControllerBase).IsAssignableFrom(type) ||
                    type.GetCustomAttributes(typeof(ControllerAttribute), inherit: true).Any() ||
                    type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).Any()))
            .ToList();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var hasControllers = assembly.GetTypes()
                .Any(type =>
                    typeof(ControllerBase).IsAssignableFrom(type) ||
                    type.GetCustomAttributes(typeof(ControllerAttribute), inherit: true).Length != 0);

                if (hasControllers)
                {
                    partManager.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            }
        }

        protected virtual void ConfigureWebHost(IWebHostBuilder webHostBuilder,
            Func<IServiceProvider> serviceProviderFactory,
            bool configureApp = true)
        {
            var config = serviceProviderFactory().GetRequiredService<IConfiguration>();

            // WEBHOST-SPECIFIC START
            /*
            webHostBuilder.ConfigureAppConfiguration((builderContext, configBuilder) =>
            {
                // builderContext.Configuration                
                // ConfigureAppConfiguration(configBuilder, builderContext.HostingEnvironment.EnvironmentName ?? GetEnvName(config));
            });

            webHostBuilder.ConfigureLogging((builderContext, logBuilder) =>
            {
                // ConfigureLogging(logBuilder, builderContext.Configuration);
            });

            webHostBuilder.ConfigureServices((builderContext, services) =>
            {
                // ConfigureServices(services, builderContext.Configuration, openApiInfo);
            });
            */
            // WEBHOST-SPECIFIC END

            // or UseKestrel
            webHostBuilder.ConfigureKestrel((builderContext, serverOptions) =>
            {
                // builderContext.Configuration
                ConfigureWebServer(serverOptions, config);
            });

            // webHostBuilder.UseKestrel(options =>
            // {                
            // });

            // webHostBuilder.UseHttpSys(httpSysOptions =>
            // {
            //     httpSysOptions.Authentication.AllowAnonymous = true;
            //     httpSysOptions.MaxConnections = -1;
            //     httpSysOptions.EnableKernelResponseBuffering = true;
            // });

            webHostBuilder.CaptureStartupErrors(true);

            // webHostBuilder.UseSetting(WebHostDefaults.PreferHostingUrlsKey, true.ToString());
            webHostBuilder.PreferHostingUrls(true);

            // var url = GetUrl(config);

            // webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, url);            
            // webHostBuilder.UseUrls(url);

            webHostBuilder.UseContentRoot(GetContentRoot(config));
            webHostBuilder.UseWebRoot(GetWebRoot(config));

            if (configureApp)
            {
                webHostBuilder.Configure((builderContext, appBuilder) =>
                {
                    // var config = builderContext.Configuration;
                    // var envName = GetEnvName(config);
                    var envName = builderContext.HostingEnvironment.EnvironmentName;
                    ConfigureApp(appBuilder, envName);
                });
            }
        }

        protected virtual void ConfigureApp(
            IApplicationBuilder appBuilder,
            string envName
            )
        {
            var serviceProvider = GetOrUpdateServiceProvider(() => appBuilder.ApplicationServices, false);

            if (IsDevelopment(envName))
            {
                appBuilder.UseDeveloperExceptionPage();
            }
            else
            {
                appBuilder.UseExceptionHandler("/Home/Error");
                appBuilder.UseHsts();
            }

            appBuilder.UseCors(CorsPolicies.Unrestricted);

            var appOptions = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
            var appSettings = appOptions.Value;

            var apiVersionDescriptionProvider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
            var apiGroups = apiVersionDescriptionProvider.ApiVersionDescriptions.ToLookup(d => d.GroupName);

            if (!string.IsNullOrEmpty(appSettings.ApiDocRouteTemplate))
            {
                var swaggerProvider = serviceProvider.GetService<ISwaggerProvider>();

                appBuilder.UseSwagger(options =>
                {
                    // options.SerializeAsV2 = true;
                    options.RouteTemplate = $"/{appSettings.ApiDocRouteTemplate.Trim('/')}";
                });

                if (!string.IsNullOrEmpty(appSettings.ApiExplorerPath))
                {
                    appBuilder.UseSwaggerUI(options =>
                    {
                        // TODO:
                        // options.DocumentTitle
                        // options.InjectJavascript
                        // options.InjectStylesheet

                        // debug:
                        // options.SupportedSubmitMethods([
                        //     SubmitMethod.Get,
                        //     SubmitMethod.Post,
                        //     SubmitMethod.Put,
                        //     SubmitMethod.Delete,
                        //     SubmitMethod.Patch
                        // ]);  

                        var appOptions = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
                        var appSettings = appOptions.Value;

                        foreach (var apiGroup in apiGroups)
                        {
                            // var apiGroupConfig = appSettings.Apis.FirstOrDefault(kv => string.Equals(kv.Key, apiGroup.Key, StringComparison.OrdinalIgnoreCase)).Value;
                            foreach (var apiDescription in apiGroup)
                            {
                                var apiConfig = apiDescription.GetApiConfig(appSettings);
                                var docInfo = apiConfig?.Info;

                                if (docInfo == default && swaggerProvider != default)
                                {
                                    var docName = apiDescription.GetDocName();
                                    var doc = swaggerProvider.GetSwagger(docName);
                                    docInfo = doc.Info;
                                }

                                var url = GetApiUrl(appSettings.ApiDocRouteTemplate, docInfo);
                                options.SwaggerEndpoint(url, docInfo.Title);
                            }
                        }
                        options.RoutePrefix = $"{appSettings.ApiExplorerPath.Trim('/')}";
                    });
                }
            }

            appBuilder.UseRouting();

            appBuilder.UseAuthentication();

            appBuilder.UseAuthorization();

            AddWebHooks(appBuilder);

            // appBuilder.Use(async (context, next) =>
            // {
            //     context.Request.EnableBuffering();
            //     await next();
            // });

            appBuilder.UseEndpoints(endpointRouteBuilder =>
            {
                // endpointRouteBuilder.MapDefaultControllerRoute();
                endpointRouteBuilder.MapControllers()
                    // .RequireCors(CorsPolicies.Unrestricted)
                    ;
            });

            // appBuilder.UseWebSockets(new WebSocketOptions());

            // appBuilder.UseMiddleware<WebSocketMiddleware>();

            var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            // webHostEnvironment.ContentRootPath = GetContentRoot(config);
            // webHostEnvironment.WebRootPath = GetWebRoot(config);

            /*
            var stdPath = webHostEnvironment.WebRootPath;
            var moduleWebRoot = Path.Combine(stdPath, path);
            webHostEnvironment.WebRootFileProvider = new PhysicalFileProvider(moduleWebRoot);
            webHostEnvironment.WebRootPath = moduleWebRoot;

            appBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(webHostEnvironment.WebRootPath)),
                RequestPath = "",
                ServeUnknownFileTypes = true,
            });
            */

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".json"] = "application/json";

            provider.Mappings[".glb"] = "model/vnd.gtf+binary";
            provider.Mappings[".gltf"] = "model/vnd.gltf+json";
            provider.Mappings[".dae"] = "model/vnd.collada+xml";
            provider.Mappings[".json"] = "application/json";
            provider.Mappings[".fbx"] = "application/octet-stream";
            provider.Mappings[".obj"] = "application/object"; // "text/plain"

            appBuilder.UseDefaultFiles();

            appBuilder.UseStaticFiles();

            /*
            appBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(WebRootPath, "content")),
                RequestPath = "/content",
                // RequestPath = new PathString(""),
                ContentTypeProvider = provider,
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream"
            });
            */

            appBuilder.UseResponseCompression();

            // cache all GET requests?
            // app.UseResponseCaching();
            // app.Use(async (context, next) =>
            // {
            //     context.Response.GetTypedHeaders().CacheControl =
            //         new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            //         {
            //             Public = true,
            //             MaxAge = TimeSpan.FromMinutes(60)
            //         };
            //     context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
            //         new string[] { "Accept-Encoding" };
            //     await next();
            // });
        }

        private IApplicationBuilder AddWebHooks(IApplicationBuilder appBuilder) // AddWebHandlers
        {
            // appBuilder.UseRouter(router =>
            // {
            // });

            appBuilder.Use(async (context, next) =>
            {
                // context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                // context.Response.Headers["Pragma"] = "no-cache";
                // context.Response.Headers["Expires"] = "0";

                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Routing");

                logger.LogDebug($"Request path: {context.Request.Path}");

                logger = context.RequestServices.GetRequiredService<ILogger<CoreService>>();
                logger.LogDebug($"Request path: {context.Request.Path}");

                await next();
            });

            // appBuilder.MapGet("/...", async r => {
            //     // r.Response...
            // }).ExcludeFromDescription();

            return appBuilder;
        }

        protected void ConfigureWebServer(KestrelServerOptions options, IConfiguration config)
        {
            // var port = GetPort(config);
            // options.ListenAnyIP(port, listenOptions =>
            // {
            // });

            var kestrelSection = config.GetSection("Kestrel");

            options.Limits.MaxResponseBufferSize = 64 * 1024;
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;

            options.Configure(kestrelSection)
                .Endpoint("HTTP", endpointConfig =>
                {

                });

            // options.ListenAnyIP(port, listenOptions =>
            // {
            //     listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            // });

            // {
            //   "Kestrel": {
            //     "Endpoints": {
            //       "Https": {
            //         "Url": "https://localhost:5001",
            //         "Protocols": "Http1AndHttp2",
            //         "Certificate": {
            //           "Path": "certificates/mycert.pfx",
            //           "Password": "mycertpassword"
            //         }
            //       }
            //     }
            //   }
            // }
        }

        protected virtual int GetPort(IConfiguration config)
        {
            // config.GetValue<string>
            var configValue = GetConfigValue(key => config[key], WebHostDefaults.HttpPortsKey);
            if (string.IsNullOrEmpty(configValue))
            {
                return 80;
            }
            else return int.Parse(configValue.Split(";")[0]);
        }

        protected virtual string GetUrl(IConfiguration config)
        {
            // var port = GetPort(config);
            // var url = $"http://127.0.0.1:{port}";
            // return url;

            // "KESTREL:ENDPOINTS:HTTP:URL"
            var configValue = GetConfigValue(key => config[key], WebHostDefaults.ServerUrlsKey) ?? "http://*:80";
            return configValue.Split(";")[0];
        }

        public virtual bool IsApiGeneration(IConfiguration config)
        {
            // Environment.GetEnvironmentVariable(...);
            var configValue = GetConfigValue(key => config.GetValue<bool>(key, false), "API_GENERATION");
            return configValue;
        }

        public virtual string GetContentRoot(IConfiguration config)
        {
            // config.GetValue<string>
            var configValue = GetConfigValue(key => config[key], WebHostDefaults.ContentRootKey)
                // ?? AppDomain.CurrentDomain.BaseDirectory
                ?? Directory.GetCurrentDirectory()
                ;
            return configValue;
        }

        public virtual string GetWebRoot(IConfiguration config)
        {
            // config.GetValue<string>
            var configValue = GetConfigValue(key => config[key], WebHostDefaults.WebRootKey)
            ?? "wwwroot";
            return configValue;
        }

        protected static string GetApiUrl(string template,
            OpenApiInfo docInfo = default)
        {
            // https://github.com/Handlebars-Net/Handlebars.Net
            // https://github.com/axuno/SmartFormat

            // result
            var url = Smart.Format(template,
                new
                {
                    version = docInfo.Version,
                    documentName = docInfo.GetDocName(),
                    extension = "json"
                });

            return $"/{url.Trim('/')}";
        }

        /*
        private static bool IsOriginAllowed(string origin, IWebHostEnvironment env)
        {
            var uri = new Uri(origin);
            // uri.Scheme == Uri.UriSchemeHttps?

            var isAllowed = uri.Host.Equals("...", StringComparison.OrdinalIgnoreCase);
            if (!isAllowed && env != null && env.IsDevelopment())
            {
                isAllowed = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            }

            return isAllowed;
        }
        */
    }

    /*
    public class CorsMiddleware
    {
        private readonly RequestDelegate _next;

        public CorsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true"); // ?
            // Added "Accept-Encoding" to this list
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Accept-Encoding, Content-Length, Content-MD5, Date, X-Api-Version, X-File-Name");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, PUT, PATCH, DELETE, OPTIONS");
            // New Code Starts here
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent; // HttpStatusCode.OK
                await context.Response.WriteAsync(string.Empty);
            }
            // New Code Ends here

            await _next(context);
        }
    }
    */
}