
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using AzureDocsUpdatesFnApp.Repositories;
using System.Linq;
using AzureDocsUpdatesFnApp.Utils;
using System;
using System.Net.Http;

namespace AzureDocsUpdatesFnApp
{
    public static class ProductMappingFn
    {
        [FunctionName("ProductMappingFn")]
        public static CacheHeaderAction Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "api/ProductMapping")] HttpRequest req,
            ExecutionContext context,
            TraceWriter log)
        {
            var productMap = ProductTitleRepository.GetMapping(context.FunctionAppDirectory).OrderBy(_ => _.Value);
            CacheHeaderAction result = new CacheHeaderAction(TimeSpan.FromHours(2), new JsonResult(productMap.ToList()));
            return result;
        }
    }
}
