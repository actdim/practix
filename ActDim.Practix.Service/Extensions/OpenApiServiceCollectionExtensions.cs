using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Service;
using ActDim.Practix.Service.OpenApi;
using ActDim.Practix.Service.Settings;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up OpenAPI/Swagger generator services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class OpenApiServiceCollectionExtensions
    {
        public static IServiceCollection AddApiGen(
            this IServiceCollection services,
            Func<IServiceProvider> serviceProviderFactory,
            Action<SwaggerGenOptions> setupAction = default)
        {
            services.AddSwaggerGen(options =>
            {
                var appOptions = serviceProviderFactory().GetRequiredService<IOptions<AppSettings>>();
                var appSettings = appOptions.Value;

                options.UseAllOfToExtendReferenceSchemas();
                options.UseAllOfForInheritance();
                options.UseOneOfForPolymorphism();

                options.SelectSubTypesUsing(ActDim.Practix.Service.OpenApi.TypeExtensions.GetOpenApiSubTypes);

                options.CustomOperationIds(ApiExtensions.GetOperationId);

                options.CustomSchemaIds(type =>
                {
                    return type.GetOpenApiSchemaId(appSettings?.SchemaPrefix, appSettings?.ClassPrefix);
                });

                var apiVersionDescriptionProvider = serviceProviderFactory().GetRequiredService<IApiVersionDescriptionProvider>();

                var docNameMap = new Dictionary<string, string>();
                foreach (var apiDescription in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    var apiConfig = apiDescription.GetApiConfig(appSettings);
                    var docInfo = apiConfig.Info;

                    var docName = ApiExtensions.GetDocName(docInfo);

                    if (apiDescription.GroupName != null)
                    {
                        docNameMap[apiDescription.GroupName] = docName;
                        options.SwaggerDoc(docName, docInfo);
                    }
                }

                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                options.IgnoreObsoleteActions();
                options.IgnoreObsoleteProperties();

                options.OperationFilter<ActionResultOperationFilter>();
                options.OperationFilter<ParameterOperationFilter>();

                options.SchemaFilter<ApiEnumSchemaFilter>();

                var namingPolicy = serviceProviderFactory().GetRequiredService<IJsonSerializer>().Options.PropertyNamingPolicy;

                options.AddSchemaFilterInstance(new JsonNamingSchemaFilter(namingPolicy));

                options.SchemaFilter<MakeWritableSchemaFilter>();

                options.SchemaFilter<MakeNullableSchemaFilter>();

                options.SchemaFilter<DictionarySubclassSchemaFilter>();

                options.DocumentFilter<ApiDocumentFilter>();

                options.DocInclusionPredicate((docName, apiDescription) =>
                {
                    if (!apiDescription.TryGetMethodInfo(out MethodInfo methodInfo))
                    {
                        return false;
                    }

                    var versions = methodInfo.DeclaringType?
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

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

                var controllerAssemblies = ApiExtensions.GetControllerTypes().Select(t => t.Assembly).Distinct();
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
