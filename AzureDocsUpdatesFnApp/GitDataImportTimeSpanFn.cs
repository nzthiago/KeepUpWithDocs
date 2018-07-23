using AzureDocsUpdates.Shared.Model;
using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class GitDataImportTimeSpanFn
    {
        private static GitDataImportService _gitDataImportService = new GitDataImportService();

        // Sample: https://azuredocsupdates.azurewebsites.net/GitDataImportTimeSpanFn?startDate=2018-06-21T00:00:00Z&endDate=2018-06-21T23:59:59Z
        // Sample: http://localhost:7071/GitDataImportTimeSpanFn?startDate=2018-03-27T00:00:00Z&endDate=2018-03-27T23:59:59Z
        [FunctionName("GitDataImportTimeSpanFn")]
        public async static Task RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
                HttpRequest req,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates,
                Constants.CosmosDbCollections.ChangesPerDay,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<DayInfo> changesPerDayCollection,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates,
                Constants.CosmosDbCollections.TitleWorkerStorage,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJobWorkerData> outputTitleWorkerStorage,
            [Queue(
                Constants.Queues.GitHubTitleWorkerQueueName,
                Connection = Constants.AppProperties.AzureStorageQueueConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJob> outputTitleWorkerQueue,
            TraceWriter log)
        {
            StringValues startDateString;
            StringValues endDateString;
            req.Query.TryGetValue("startDate", out startDateString);
            req.Query.TryGetValue("endDate", out endDateString);

            // ISO 8601: 2018-03-20T00:00:00Z
            // ISO 8601: 2018-03-23T23:59:59Z
            var startDate = DateTime.Parse(startDateString.First(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            var endDate = DateTime.Parse(endDateString.First(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            
            await _gitDataImportService.RunAsync(
                    startDate, endDate,
                    changesPerDayCollection,
                    outputTitleWorkerStorage,
                    outputTitleWorkerQueue,
                    log);
        }
    }
}
