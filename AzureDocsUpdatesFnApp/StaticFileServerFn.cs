using AzureDocsUpdatesFnApp.Repositories;
using AzureDocsUpdatesFnApp.Utils;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Net.Http.Headers;
using MimeTypes;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class StaticFileServerFn
    {
        private static string key = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient _telemetryClient = new TelemetryClient() { InstrumentationKey = key };

        const string staticFilesFolder = "www";
        static string defaultPage = string.IsNullOrEmpty(GetEnvironmentVariable("DEFAULT_PAGE")) ? "index.html" : GetEnvironmentVariable("DEFAULT_PAGE");

        [FunctionName("StaticFileServerFn")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{*path}")] HttpRequest req, // {*path:regex(^(?!admin).*$)}
            ExecutionContext context, TraceWriter log)
        {
            _telemetryClient.Context.Cloud.RoleName = context.FunctionName;

            var requestTelemetry = new RequestTelemetry
            {
                Name = $"{req.Method} {req.Path}",
                Url = new Uri(req.GetDisplayUrl())
            };
            requestTelemetry.Properties["httpMethod"] = req.Method;
            requestTelemetry.Context.Operation.Id = context.InvocationId.ToString();
            requestTelemetry.Context.Operation.Name = context.FunctionName;

            var operation = _telemetryClient.StartOperation(requestTelemetry);

            IActionResult result;
            try
            {
                var functionAppDirectory = context.FunctionAppDirectory;
#if DEBUG
                if (Debugger.IsAttached)
                {
                    functionAppDirectory = functionAppDirectory + "\\..\\..\\..";
                }
#endif
                var filePath = GetFilePath(req.Path, functionAppDirectory);
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (filePath.EndsWith("index.html"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var html = await reader.ReadToEndAsync();
                       // html = html.Replace("{{productfilter-options-placeholder}}", GetProductfilterOptions(context.FunctionAppDirectory));

                        result = new CacheHeaderAction(TimeSpan.FromHours(2), new FileContentResult(Encoding.UTF8.GetBytes(html), new MediaTypeHeaderValue(GetMimeType(filePath)))
                        {
                            LastModified = File.GetLastWriteTimeUtc(filePath)
                        });
                    }
                }
                else
                {
                    result = new CacheHeaderAction(TimeSpan.FromHours(2), new FileStreamResult(stream, new MediaTypeHeaderValue(GetMimeType(filePath)))
                    {
                        LastModified = File.GetLastWriteTimeUtc(filePath)
                    });
                }

                operation.Telemetry.Success = true;
                operation.Telemetry.ResponseCode = "200";
                _telemetryClient.StopOperation(operation);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);

                operation.Telemetry.Success = false;
                operation.Telemetry.ResponseCode = "400";
                _telemetryClient.StopOperation(operation);

                result = new NotFoundResult();
            }

            return result;
        }

        private static string GetProductfilterOptions(string functionAppDirectory)
        {
            var options = new StringBuilder();
            foreach (var mapping in ProductTitleRepository.GetMapping(functionAppDirectory).OrderBy(_ => _.Value))
            {
                options.Append($"<option value=\"{mapping.Key}\">{mapping.Value}</option>");
            }
            return options.ToString();
        }

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        private static string GetFilePath(string path, string functionAppDirectory)
        {
            path = path.TrimStart('/');

            var staticFilesPath = Path.GetFullPath(Path.Combine(functionAppDirectory, staticFilesFolder));
            var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

            if (!IsInDirectory(staticFilesPath, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            var isDirectory = Directory.Exists(fullPath);
            if (isDirectory)
            {
                fullPath = Path.Combine(fullPath, defaultPage);
            }

            return fullPath;
        }

        private static bool IsInDirectory(string parentPath, string childPath)
        {
            var parent = new DirectoryInfo(parentPath);
            var child = new DirectoryInfo(childPath);

            var dir = child;
            do
            {
                if (dir.FullName == parent.FullName)
                {
                    return true;
                }
                dir = dir.Parent;
            } while (dir != null);

            return false;
        }

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypeMap.GetMimeType(fileInfo.Extension);
        }
    }
}
