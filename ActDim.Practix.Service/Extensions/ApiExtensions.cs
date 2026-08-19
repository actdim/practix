using ActDim.Practix.Service.Settings;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ActDim.Practix.Service.OpenApi
{
    public static partial class ApiExtensions
    {
        private static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

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
            docInfo.Extensions[OpenApiInfoExtensions.Name] = new JsonNodeExtension(JsonValue.Create(value));
            return docInfo;
        }

        public static OpenApiInfo SetSchemaPrefix(this OpenApiInfo docInfo, string value)
        {
            if (docInfo.Extensions == default)
            {
                docInfo.Extensions = new Dictionary<string, IOpenApiExtension>();
            }
            docInfo.Extensions[OpenApiInfoExtensions.SchemaPrefix] = new JsonNodeExtension(JsonValue.Create(value));
            return docInfo;
        }

        public static string GetDocName(this ApiVersionDescription apiVersionDescription)
        {
            var version = apiVersionDescription.ApiVersion.GetName();
            return $"{apiVersionDescription.GroupName}_v{version}";
        }

        public static string GetOperationId(this ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
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

        public static ApiConfig GetApiConfig(this ApiVersionDescription apiVersionDescription, IConfiguration config, ApiVersion defaultApiVersion = default)
        {
            return apiVersionDescription.GetApiConfig(config.Get<AppSettings>(), defaultApiVersion);
        }

        public static ApiConfig GetApiConfig(this ApiVersionDescription apiVersionDescription, AppSettings appSettings, ApiVersion defaultApiVersion = default)
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
    }
}
