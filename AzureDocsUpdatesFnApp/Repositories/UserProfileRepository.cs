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

        public UserProfileRepository()
        {
            _cosmosDbClient = DocumentDbAccount.Parse(Environment.GetEnvironmentVariable("CosmosDB", EnvironmentVariableTarget.Process));
        }
            
        public UserProfileRepository(string connectionString)
        {
            _cosmosDbClient = DocumentDbAccount.Parse(connectionString);
        }

        public async Task<UserProfile> GetUserProfileById(string userId)
        {
            UserProfile userProfile = null;

            var collection = new DocumentCollection { Id = "UserProfile" };
            collection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("AzureDocUpdates"), collection)).Resource;

            var profileQuery = from p in _cosmosDbClient.CreateDocumentQuery<UserProfile>(collection.SelfLink)
                          where p.Id == userId
                          select p;

            userProfile = profileQuery.ToArray().FirstOrDefault();

            return userProfile;
        }

        public async Task<UserProfile> GetUserProfileByEmailAddress(string emailAddress)
        {
            UserProfile userProfile = null;

            var collection = new DocumentCollection { Id = "UserProfile" };
            collection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("AzureDocUpdates"), collection)).Resource;

            var profileQuery = from p in _cosmosDbClient.CreateDocumentQuery<UserProfile>(collection.SelfLink)
                               where p.ContactProfile.EmailAddress == emailAddress
                               select p;

            userProfile = profileQuery.ToArray().FirstOrDefault();

            return userProfile;
        }

        public async Task UpdateUserProfile(UserProfile userProfile)
        {
            var collection = new DocumentCollection { Id = "UserProfile" };
            collection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("AzureDocUpdates"), collection)).Resource;

            var response = await _cosmosDbClient.UpsertDocumentAsync(collection.SelfLink, userProfile);

            Document upsertedDocument = response.Resource;
        }

        public async Task CreateUserProfile(UserProfile userProfile)
        {
            var collection = new DocumentCollection { Id = "UserProfile" };
            collection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("AzureDocUpdates"), collection)).Resource;

            Document profileCreated = await _cosmosDbClient.CreateDocumentAsync(collection.SelfLink, userProfile);
        }

        public async Task<IList<UserProfile>> GetUsersByCategory(string category)
        {
            var collection = new DocumentCollection { Id = "UserProfile" };
            collection = (await _cosmosDbClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("AzureDocUpdates"), collection)).Resource;

            var categoryQuery = _cosmosDbClient.CreateDocumentQuery<UserProfile>(collection.SelfLink, $"SELECT * FROM c where ARRAY_CONTAINS(c.notificationProfile.categories, '{category}')");

            List<UserProfile> profiles = categoryQuery.ToList<UserProfile>();

            return profiles;
        }
    }
}
