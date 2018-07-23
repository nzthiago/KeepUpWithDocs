using AzureDocsUpdates.Shared.Model;
using AzureDocsUpdatesFnApp.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class GitHubTitleWorkerFn
    {
        private const int BatchSize = 30;

        private const int LinesToInspect = 5;

        private static HttpClient _httpClient = new HttpClient();

        [FunctionName("GitHubTitleWorkerFn")]
        public static async Task Run(
            [QueueTrigger(
                Constants.Queues.GitHubTitleWorkerQueueName, 
                Connection = Constants.AppProperties.AzureStorageQueueConnectionString)]
                ProcessDayInfoForTitlesJob inputQueueItem,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates,
                Constants.CosmosDbCollections.TitleWorkerStorage,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString,
                Id = "{id}")]
                ProcessDayInfoForTitlesJobWorkerData titlesJobWorkerData,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates,
                Constants.CosmosDbCollections.TitleWorkerStorage,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJobWorkerData> outgogingTitlesJobWorkerData,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates, 
                Constants.CosmosDbCollections.ChangesPerDay, 
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString, 
                Id = "{id}")]
                DayInfo incomingDayInfo,
            [CosmosDB(
                Constants.CosmosDbNames.AzureDocUpdates,
                Constants.CosmosDbCollections.ChangesPerDay,
                ConnectionStringSetting = Constants.AppProperties.CosmosDbConnectionString)]
                IAsyncCollector<DayInfo> outgoingDayInfos,
            [Queue(
                Constants.Queues.GitHubTitleWorkerQueueName, 
                Connection = Constants.AppProperties.AzureStorageQueueConnectionString)]
                IAsyncCollector<ProcessDayInfoForTitlesJob> outputProcessCommitForTitleQueue,
            TraceWriter log)
        {
            log.Info($"GitHubTitleWorkerFn processing {inputQueueItem.DayInfoId} with {titlesJobWorkerData.FilesToProcess.Count} files");

            var processed = 0;
            var filesToProcess = titlesJobWorkerData.FilesToProcess.ToList();

            for (int i = 0; i < Math.Min(filesToProcess.Count, BatchSize); i++)
            {
                var fileToProcess = filesToProcess[i];

                var response = await _httpClient.GetAsync("https://raw.githubusercontent.com/" + fileToProcess.Replace("/contents/", "/master/"));
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    using (var reader = new StreamReader(stream))
                    {
                        for (int j = 0; j < LinesToInspect && !reader.EndOfStream; j++)
                        {
                            var line = reader.ReadLine().Trim();
                            if (line.StartsWith("title: ", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var title = line
                                    .Replace("title: ", "")
                                    .Replace("| Microsoft Docs", "")
                                    .Trim()
                                    .Trim('"', '\'');

                                incomingDayInfo.DocChanges
                                    .Where(_ => _.Url == fileToProcess)
                                    .ToList()
                                    .ForEach(_ => _.Title = title);

                                break;
                            }
                        }
                    }
                }

                titlesJobWorkerData.FilesToProcess.Remove(fileToProcess);
                processed++;
            }

            await outgogingTitlesJobWorkerData.AddAsync(titlesJobWorkerData);

            if (titlesJobWorkerData.FilesToProcess.Count > 0)
            {
                await outgoingDayInfos.AddAsync(incomingDayInfo);
                await outputProcessCommitForTitleQueue.AddAsync(inputQueueItem);
            }
            else
            {
                incomingDayInfo.TitlesGenerated = true;
                await outgoingDayInfos.AddAsync(incomingDayInfo);
            }
        }
    }
}
