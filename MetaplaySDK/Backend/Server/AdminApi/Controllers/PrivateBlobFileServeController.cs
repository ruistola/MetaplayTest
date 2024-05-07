// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Akka.Actor;
using Metaplay.Cloud.RuntimeOptions;
using Metaplay.Cloud.Services;
using Metaplay.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Metaplay.Server.AdminApi.Controllers
{
    public class PrivateBlobFileServeControllerEnabledCondition : MetaplayFeatureEnabledConditionAttribute
    {
        public override bool IsEnabled => !string.IsNullOrEmpty(RuntimeOptionsRegistry.Instance.GetCurrent<BlobStorageOptions>().ExposedPrivatePathPrefix);
    }

    [Route("file")]
    [PrivateBlobFileServeControllerEnabledCondition]
    public class PrivateBlobFileServeController : MetaplayAdminApiController
    {
        static readonly Lazy<IBlobStorage> _storage = new Lazy<IBlobStorage>(InitStorage);

        public PrivateBlobFileServeController(ILogger<PrivateBlobFileServeController> logger, IActorRef adminApi) : base(logger, adminApi)
        {
        }

        static IBlobStorage InitStorage()
        {
            BlobStorageOptions opts = RuntimeOptionsRegistry.Instance.GetCurrent<BlobStorageOptions>();
            return RuntimeOptionsRegistry.Instance.GetCurrent<BlobStorageOptions>().CreatePrivateBlobStorage(opts.ExposedPrivatePathPrefix);
        }

        [HttpGet("{*filePath}")]
        [RequirePermission(MetaplayPermissions.PrivateBlobFileServeRead)]
        public async Task<IActionResult> Get(string filePath)
        {
            filePath = Path.GetRelativePath(".", filePath);
            if (filePath.StartsWith("..", StringComparison.Ordinal))
                return NotFound();
            filePath = filePath.Replace('\\', '/');
            string       contentType = "application/octet-stream";
            IBlobStorage blobStorage = _storage.Value;

            _logger.LogDebug("Forwarding file from private blob storage: {Path}", filePath);

            if (blobStorage is S3BlobStorage s3BlobStorage)
            {
                (Stream contentStream, Dictionary<string, string> headers) = await s3BlobStorage.GetContentAndHeadersAsync(filePath);
                if (contentStream == null)
                    return NotFound();
                if (headers.TryGetValue("Content-Type", out string contentTypeFromHeader))
                    contentType = contentTypeFromHeader;
                foreach (KeyValuePair<string, string> header in headers)
                    Response.Headers[header.Key] = header.Value;
                return new FileStreamResult(contentStream, contentType);
            }

            if (new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string contentTypeFromExtension))
                contentType = contentTypeFromExtension;
            byte[] content = await blobStorage.GetAsync(filePath);
            if (content == null)
                return NotFound();
            return new FileContentResult(content, contentType);
        }
    }
}

