using AzureDocsUpdatesFnApp.Model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp.Repositories
{
    public class UserProfileRepository
    {
        private readonly DocumentClient _cosmosDbClient = null; 
        private static readonly string DatabaseName = "DocsNotification";
        private static readonly string CollectionName = "UserProfile";


        public UserProfileRepository()
        {
            _cosmosDbClient = DocumentDbAccount.Parse(Environment.GetEnvironmentVariable("CosmosDB", EnvironmentVariableTarget.Process));
        }
            
        public UserProfileRepository(string connectionString)
        {
            _cosmosDbClient = DocumentDbAccount.Parse(connectionString);
        }

        public UserProfile GetUserProfileById(string userId)
        {
            UserProfile userProfile = null;

            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            var profileQuery = from p in _cosmosDbClient.CreateDocumentQuery<UserProfile>(collectionLink)
                          where p.Id == userId
                          select p;

            userProfile = profileQuery.ToArray().FirstOrDefault();

            return userProfile;
        }

        public IList<UserProfile> GetUserProfilesByFrequency()
        {
            List<UserProfile> userProfiles = new List<UserProfile>();
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            
            var profileQuery = from p in _cosmosDbClient.CreateDocumentQuery<UserProfile>(collectionLink)
                               where p.NotificationProfile.Frequency == 1 |
                               ( p.NotificationProfile.Frequency == 7 
                                 && DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                               select p;

            userProfiles = profileQuery.ToList();
           
            return userProfiles;
        }

        public UserProfile GetUserProfileByEmailAddress(string emailAddress)
        {
            UserProfile userProfile = null;
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            var profileQuery = from p in _cosmosDbClient.CreateDocumentQuery<UserProfile>(collectionLink)
                               where p.ContactProfile.EmailAddress == emailAddress
                               select p;

            userProfile = profileQuery.ToArray().FirstOrDefault();

            return userProfile;
        }

        public async Task UpdateUserProfile(UserProfile userProfile)
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            var response = await _cosmosDbClient.UpsertDocumentAsync(collectionLink, userProfile);

            Document upsertedDocument = response.Resource;
        }

        public async Task<Document> CreateUserProfile(UserProfile userProfile)
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            Document profileCreated = await _cosmosDbClient.CreateDocumentAsync(collectionLink, userProfile);

            return profileCreated;
        }

        public IList<UserProfile> GetUsersByCategory(string category)
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            var categoryQuery = _cosmosDbClient.CreateDocumentQuery<UserProfile>(collectionLink, $"SELECT * FROM c where ARRAY_CONTAINS(c.notificationProfile.categories, '{category}')");

            List<UserProfile> profiles = categoryQuery.ToList<UserProfile>();

            return profiles;
        }
    }
}
