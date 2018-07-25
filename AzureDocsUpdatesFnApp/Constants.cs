namespace AzureDocsUpdatesFnApp
{
    public class Constants
    {
        public class TimerTriggers
        {
            public const string EveryDayAt7305AM = "0 30 7 * * *";
        }

        public class AppProperties
        {
            public const string AzureStorageQueueConnectionString = "AzureStorageQueue";
            public const string CosmosDbConnectionString = "CosmosDB";
        }

        public class CosmosDbNames
        {
            public const string DocsNotification = "DocsNotification";
        }

        public class CosmosDbCollections
        {
            public const string ChangesPerDay = "ChangesPerDay";
            public const string TitleWorkerStorage = "TitleWorkerStorage";
            public const string UserProfile = "UserProfile";
        }

        public class Queues
        {
            public const string GitHubTitleWorkerQueueName = "github-title-worker-queue";
        }
    }
}