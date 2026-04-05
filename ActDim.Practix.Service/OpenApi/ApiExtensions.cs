using ActDim.Practix.Abstractions.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Autofac.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public static partial class ApiExtensions
    {
        public static IEnumerable<Type> GetControllerTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return assemblies
                .SelectMany(a => a.GetLoadableTypes())
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
        }

        /// <summary>
        /// GetDocInfo
        /// </summary>
        /// <param name="apiVersionDescription"></param>
        /// <returns></returns>
        public static OpenApiInfo GetOpenApiInfo(this ApiVersionDescription apiVersionDescription)
        {
            return new OpenApiInfo()
            {
                Title = apiVersionDescription.GroupName,
                Version = apiVersionDescription.ApiVersion.ToString()
            }
            .SetDocName(apiVersionDescription.GetDocName());
        }

        public static OpenApiInfo SetDocName(this OpenApiInfo docInfo, string value)
        {
            if (docInfo.Extensions == default)
            {
                docInfo.Extensions = new Dictionary<string, IOpenApiExtension>();
            }
            // new OpenApiString(value)
            docInfo.Extensions[OpenApiInfoExtensions.Name] = new JsonNodeExtension(JsonValue.Create(value));
            return docInfo;
        }

        public static OpenApiInfo SetSchemaPrefix(this OpenApiInfo docInfo, string value)
        {
            if (docInfo.Extensions == default)
            {
                docInfo.Extensions = new Dictionary<string, IOpenApiExtension>();
            }
            // new OpenApiString(value)
            docInfo.Extensions[OpenApiInfoExtensions.SchemaPrefix] = new JsonNodeExtension(JsonValue.Create(value));
            return docInfo;
        }

        public static string GetDocName(this ApiVersionDescription apiVersionDescription)
        {
            var version = apiVersionDescription.ApiVersion.GetName();
            // var version = apiVersionDescription.ApiVersion.ToString();
            return $"{apiVersionDescription.GroupName}_v{version}";
        }

        public static string GetOperationId(this ApiDescription apiDescription)
        {
            // apiDescription.HttpMethod
            if (apiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                // apiDescription.ActionDescriptor.RouteValues["controller"]
                // apiDescription.ActionDescriptor.RouteValues["action"]

                return $"{actionDescriptor.ControllerName}_{actionDescriptor.ActionName}";
            }
            else
            {
                return apiDescription.ActionDescriptor.DisplayName;
            }
        }

        public static string GetName(this ApiVersion apiVersion)
        {
            var majorVersion = apiVersion.MajorVersion == null ? 0 : apiVersion.MajorVersion;
            var minorVersion = apiVersion.MinorVersion == null ? 0 : apiVersion.MinorVersion;
            if (minorVersion == 0)
            {
                return majorVersion.ToString();
            }

            return $"{majorVersion}.{minorVersion}";
        }

        private static string ExtractStringFromExtension(IOpenApiExtension extension)
        {
            using var stringWriter = new StringWriter();
            var writer = new OpenApiJsonWriter(stringWriter);
            extension.Write(writer, OpenApiSpecVersion.OpenApi3_0);

            var result = stringWriter.ToString();

            return result.Trim('"');
        }

        public static string GetDocName(this OpenApiInfo docInfo, ApiVersionDescription apiVersionDescription = default)
        {
            string result = default;
            if (docInfo.Extensions != default && docInfo.Extensions.TryGetValue(OpenApiInfoExtensions.Name, out var value))
            {
                result = ExtractStringFromExtension(value);
            }
            if (string.IsNullOrEmpty(result) && apiVersionDescription != default)
            {
                result = apiVersionDescription.GetDocName();
            }
            return result;
        }

        public static string GetSchemaPrefix(this OpenApiInfo docInfo, ApiVersionDescription apiVersionDescription = default)
        {
            string result = default;
            if (docInfo.Extensions != default && docInfo.Extensions.TryGetValue(OpenApiInfoExtensions.SchemaPrefix, out var value))
            {
                result = ExtractStringFromExtension(value);
            }
            if (string.IsNullOrEmpty(result) && apiVersionDescription != default)
            {
                result = apiVersionDescription.GroupName;
            }
            return result;
        }

        public static string GetSchemaPrefix(this ApiVersionDescription apiVersionDescription)
        {
            return apiVersionDescription?.GroupName;
        }

        public static IApiConfig GetApiConfig(this ApiVersionDescription apiVersionDescription, IConfiguration config, ApiVersion defaultApiVersion = default)
        {
            return apiVersionDescription.GetApiConfig(config.Get<AppSettings>(), defaultApiVersion);
        }

        public static IApiConfig GetApiConfig(this ApiVersionDescription apiVersionDescription, AppSettings appSettings, ApiVersion defaultApiVersion = default)
        {
            var version = apiVersionDescription.ApiVersion;
            if (version == default)
            {
                version = defaultApiVersion ?? new ApiVersion(1, 0);
            }
            var apiConfig = appSettings.Apis.FirstOrDefault(api => string.Equals(api.Key, apiVersionDescription.GroupName, StringComparison.OrdinalIgnoreCase)).Value;
            if (apiConfig == default)
            {
                return apiConfig;
            }

            // TODO: use apiConfig.Overrides

            if (apiConfig.Info == default)
            {
                apiConfig.Info = apiVersionDescription.GetOpenApiInfo();
            }
            apiConfig.Info.Version = version.ToString();
            if (string.IsNullOrEmpty(apiConfig.Info.GetDocName()))
            {
                apiConfig.Info.SetDocName(apiVersionDescription.GetDocName());
            }
            return apiConfig;
        }

        public static IServiceCollection AddApiGen(this IServiceCollection services,
            Func<IServiceProvider> serviceProviderFactory,
            Action<SwaggerGenOptions> setupAction = default
            )
        {
            services.AddSwaggerGen(options =>
            {
                var appOptions = serviceProviderFactory().GetRequiredService<IOptions<AppSettings>>();
                var appSettings = appOptions.Value;

                // TODO: check
                // options.NonNullableReferenceTypesAsRequired();
                // options.SupportNonNullableReferenceTypes();

                options.UseAllOfToExtendReferenceSchemas();
                options.UseAllOfForInheritance();
                options.UseOneOfForPolymorphism();

                options.SelectSubTypesUsing(TypeExtensions.GetOpenApiSubTypes);

                options.CustomOperationIds(GetOperationId);

                // options.SchemaGeneratorOptions.SchemaIdSelector
                options.CustomSchemaIds(type =>
                {
                    return type.GetOpenApiSchemaId(appSettings?.SchemaPrefix, appSettings?.ClassPrefix);
                });

                var apiVersionDescriptionProvider = serviceProviderFactory().GetRequiredService<IApiVersionDescriptionProvider>();

                var docNameMap = new Dictionary<string, string>();
                foreach (var apiDescription in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    var apiConfig = apiDescription.GetApiConfig(appSettings)
                    ;
                    var docInfo = apiConfig.Info;

                    var docName = GetDocName(docInfo);

                    if (apiDescription.GroupName != null)
                    {
                        docNameMap[apiDescription.GroupName] = docName;
                        options.SwaggerDoc(docName, docInfo);
                    }
                }

                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                options.IgnoreObsoleteActions();
                options.IgnoreObsoleteProperties();

                // options.OperationFilter<OperationFilter>();
                options.OperationFilter<ActionResultOperationFilter>();
                options.OperationFilter<ParameterOperationFilter>();

                options.SchemaFilter<ApiEnumSchemaFilter>();

                // var namingPolicy = serviceProviderFactory()
                //     .GetRequiredService<IOptions<JsonOptions>>().Value
                //     .JsonSerializerOptions.PropertyNamingPolicy;
                var namingPolicy = serviceProviderFactory().GetRequiredService<IJsonSerializer>().Options.PropertyNamingPolicy;

                options.AddSchemaFilterInstance(new JsonNamingSchemaFilter(namingPolicy));

                options.SchemaFilter<MakeWritableSchemaFilter>();

                options.SchemaFilter<MakeNullableSchemaFilter>();

                options.SchemaFilter<DictionarySubclassSchemaFilter>();

                options.DocumentFilter<ApiDocumentFilter>();

                // options.MapType<Guid>(() => new OpenApiSchema
                // {
                //     Type = "string",
                //     Format = "uuid",
                //     // Format = null,
                //     // Example = new OpenApiString(Guid.NewGuid().ToString())
                // });

                // extras:
                // options.MapType<Date>(() => new OpenApiSchema
                // {
                //     Type = "string",
                //     Format = "date",
                //     Example = new OpenApiDate(new DateTime(2020, 1, 1))
                // });

                options.DocInclusionPredicate((docName, apiDescription) =>
                {
                    if (!apiDescription.TryGetMethodInfo(out MethodInfo methodInfo)) return false;

                    var versions = methodInfo.DeclaringType?
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

                    // return versions?.Any(v => $"v{v}" == version) ?? false;

                    var actionDescriptor = (ControllerActionDescriptor)apiDescription.ActionDescriptor;
                    var cAttrs = actionDescriptor.ControllerTypeInfo.GetCustomAttributes().ToArray();
                    var mAttrs = actionDescriptor.MethodInfo.GetCustomAttributes().ToArray();
                    return docNameMap[apiDescription.GroupName] == docName && !(
                        cAttrs.OfType<ExcludeFromOpenApiAttribute>().Any() ||
                        (cAttrs.Any(a => a is OpenApiAttribute openApiAttr && openApiAttr.Exclude)) ||
                        mAttrs.OfType<ExcludeFromOpenApiAttribute>().Any() ||
                        (mAttrs.Any(a => a is OpenApiAttribute openApiAttr && openApiAttr.Exclude)) ||
                        mAttrs.OfType<NonActionAttribute>().Any()
                    );
                });

                var controllerAssemblies = GetControllerTypes().Select(t => t.Assembly).Distinct();
                foreach (var assembly in controllerAssemblies)
                {
                    var fileName = $"{assembly.GetName().Name}.xml";
                    fileName = Path.Combine(AppContext.BaseDirectory, fileName);
                    if (File.Exists(fileName))
                    {
                        options.IncludeXmlComments(fileName, true);
                    }
                }

                if (setupAction != null)
                {
                    setupAction(options);
                }
            });

            services.AddEndpointsApiExplorer();

            return services;
        }
    }
}