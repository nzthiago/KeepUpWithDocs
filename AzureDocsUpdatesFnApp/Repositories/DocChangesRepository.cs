using AzureDocsUpdatesFnApp.Model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp.Repositories
{
    public class DocChangesRepository
    {
        const int PageSize = 2;

        public int MaxPages => 3;

        private DocumentClient _cosmosDbClient = DocumentDbAccount.Parse(Environment.GetEnvironmentVariable("CosmosDB", EnvironmentVariableTarget.Process));

        private static Dictionary<int, string> _pageContinuationTokens = new Dictionary<int, string>();

        public DocumentClient GetCosmosDbClient()
        {
            return _cosmosDbClient;
        }

        public async Task<DocumentCollection> GetFileChangesCollectionAsync()
        {
            DocumentCollection documentCollection = new DocumentCollection { Id = "ChangesPerDay" };
            documentCollection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(Constants.CosmosDbNames.AzureDocUpdates), documentCollection)).Resource;
            documentCollection.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            await _cosmosDbClient.ReplaceDocumentCollectionAsync(documentCollection);

            return documentCollection;
        }

        internal async Task<IEnumerable<DayInfo>> GetChangesPerDayAsync(int page, string dateFilter)
        {
            int resultPage = 1;

            var cosmosDbClient = GetCosmosDbClient();
            var collection = await GetFileChangesCollectionAsync();

            if (!string.IsNullOrWhiteSpace(dateFilter))
            {
                var parsedDate = DateTime.Parse(dateFilter);
                var query = cosmosDbClient.CreateDocumentQuery<DayInfo>(collection.SelfLink)
                                          .Where(_ => _.Id == parsedDate)
                                          .AsDocumentQuery();

                return await query.ExecuteNextAsync<DayInfo>();
            }

            string continuationToken = null;
            if (page > 1)
            {
                if (_pageContinuationTokens.TryGetValue(page, out continuationToken))
                {
                    resultPage = page;
                }
            }

            FeedResponse<DayInfo> result = null;
            while (resultPage <= page)
            {
                var query = cosmosDbClient.CreateDocumentQuery<DayInfo>(collection.SelfLink,
                    new FeedOptions
                    {
                        MaxItemCount = PageSize,
                        RequestContinuation = continuationToken
                    })
                    .Where(_ => _.TitlesGenerated)
                    .OrderByDescending(_ => _.Date)
                    .Take(PageSize * MaxPages)
                    .AsDocumentQuery();

                result = await query.ExecuteNextAsync<DayInfo>();

                continuationToken = result.ResponseContinuation;
                if (!string.IsNullOrWhiteSpace(continuationToken))
                {
                    _pageContinuationTokens[resultPage + 1] = continuationToken;
                }
                resultPage++;
            }

            return result;
        }
    }
}
