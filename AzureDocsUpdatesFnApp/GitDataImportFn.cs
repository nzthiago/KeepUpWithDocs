using AzureDocsUpdates.Shared.Model;
using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class GitDataImportFn
    {
        private static GitDataImportService _gitDataImportService = new GitDataImportService();

        [FunctionName("GitDataImportFn")]
        public static async Task RunAsync(
            [TimerTrigger(
                Constants.TimerTriggers.EveryDayAt7305AM,
                RunOnStartup = false)]
                TimerInfo myTimer,
            [CosmosDB(
                Constants.CosmosDbNames.DocsNotification,
                Constants.CosmosDbCollections.ChangesPerDay,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<DayInfo> changesPerDayCollection,
            [CosmosDB(
                Constants.CosmosDbNames.DocsNotification,
                Constants.CosmosDbCollections.TitleWorkerStorage,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJobWorkerData> outputTitleWorkerStorage,
            [Queue(
                Constants.Queues.GitHubTitleWorkerQueueName,
                Connection = Constants.AppProperties.AzureStorageQueueConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJob> outputTitleWorkerQueue,
            TraceWriter log)
        {
            // Idea: only run once a day for the last day
            if (myTimer.ScheduleStatus.Last.Date >= DateTime.UtcNow.Date)
            {
                log.Info($"Skip import - already ran at {myTimer.ScheduleStatus.Last}");
                return;
            }

            // For debugging
            var daysInThePast = 1;

            var yesterdayStartOfDay = DateTime.UtcNow.Date.AddDays(-1 * daysInThePast);
            var yesterdayEndOfDay = DateTime.UtcNow.Date.AddDays(-1 * (daysInThePast - 1)).AddMilliseconds(-1);

            log.Info($"Start import from {yesterdayStartOfDay} to {yesterdayEndOfDay}");

            await _gitDataImportService.RunAsync(
                yesterdayStartOfDay, yesterdayStartOfDay,
                changesPerDayCollection,
                outputTitleWorkerStorage,
                outputTitleWorkerQueue,
                log);
        }
    }
}
