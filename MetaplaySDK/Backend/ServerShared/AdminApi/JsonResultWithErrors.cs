// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using Metaplay.Core.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Metaplay.Server.AdminApi;

/// <summary>
/// Result type for controller endpoints that ignores errors during serialization and logs the errors after serialization is complete.
/// </summary>
public class JsonResultWithErrors : JsonResult
{
    ILogger    _logger;
    JsonSerializationErrorLogger _errors;

    internal JsonResultWithErrors(object value, object serializerSettings, ILogger logger) : base(value, serializerSettings)
    {
        _logger = logger;
        if (serializerSettings is JsonSerializerSettings jsonSerializerSettings)
        {
            _errors = new JsonSerializationErrorLogger();
            SerializerSettings = new JsonSerializerSettings(jsonSerializerSettings)
            {
                // \todo Figure out a better solution, may get removed in .NET 9
#pragma warning disable SYSLIB0050 // 'StreamingContextStates' is obsolete: 'Formatter-based serialization is obsolete and should not be used.' (https:aka.ms/dotnet-warnings/SYSLIB0050)
                Context = new StreamingContext(StreamingContextStates.All, _errors),
#pragma warning restore SYSLIB0050
                Error = JsonSerializationErrorUtility.HandleAdminApiJsonError
            };
        }
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        await base.ExecuteResultAsync(context);
        JsonSerializationErrorAdminApiUtility.WriteErrorsToConsole(_errors, _logger, context.HttpContext.Request.Path.ToString());
    }
}
