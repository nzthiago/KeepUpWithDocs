using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Repositories;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AzureDocsUpdatesFnApp.Tests
{
    // TODO: These integration tests need some cleanup!!

    public class UserProfileRepositoryIntegrationTest
    {
        private static IConfiguration Configuration { get; set; }
        private static string cosmosDbConnectionString;
        
        public UserProfileRepositoryIntegrationTest()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            cosmosDbConnectionString = Configuration["CosmosDb"];
        }

        [Fact]
        public void VerifyGetUserProfileById()
        {
            var repository = new UserProfileRepository(cosmosDbConnectionString);
            var profile = repository.GetUserProfileById("e40b1c7f-70bc-4bab-8c5a-fa87824d58a1");

            Assert.NotNull(profile);
        }


        [Fact]
        public async Task VerifyGetUserProfileByEmailAddress()
        {
            var repository = new UserProfileRepository(cosmosDbConnectionString);

            UserProfile profile = new UserProfile
            {
                ContactProfile = new ContactProfile
                {
                    EmailAddress = "mcollier@contoso.com",
                    FirstName = "Michael",
                    LastName = "Collier",
                    MobilePhoneNumber = "555-555-1234"
                },
                NotificationProfile = new NotificationProfile
                {
                    EmailAddress = "mcollier@contoso.com",
                    MobilePhoneNumber = "555-555-1234",
                    WebhookUrl = "https://api.contoso.com/",
                    Categories = new string[] { "app-service", "cosmos-db" }
                }
            };

            await repository.CreateUserProfile(profile);


            var profileRetrieved = repository.GetUserProfileByEmailAddress("mcollier@contoso.com");

            Assert.NotNull(profileRetrieved);
            Assert.Equal("mcollier@contoso.com", profileRetrieved.ContactProfile.EmailAddress);
        }

        [Fact]
        public async Task VerifyCreateUserProfile()
        {
            UserProfile profile = new UserProfile
            {
                ContactProfile = new ContactProfile
                {
                    EmailAddress = "mcollier@contoso.com",
                    FirstName = "Michael",
                    LastName = "Collier",
                    MobilePhoneNumber = "555-555-1234"
                },
                NotificationProfile = new NotificationProfile
                {
                    EmailAddress = "mcollier@contoso.com",
                    MobilePhoneNumber = "555-555-1234",
                    WebhookUrl = "https://api.contoso.com/",
                    Categories = new string[] { "app-service", "cosmos-db"}
                }
            };

            var repository = new UserProfileRepository(cosmosDbConnectionString);
            await repository.CreateUserProfile(profile);
        }

        [Fact]
        public async Task VerifyUpdateUserProfile()
        {
            var repository = new UserProfileRepository(cosmosDbConnectionString);
            UserProfile profile = repository.GetUserProfileById("e40b1c7f-70bc-4bab-8c5a-fa87824d58a1");

            profile.NotificationProfile.MobilePhoneNumber = "555-555-1234";
            await repository.UpdateUserProfile(profile);

            UserProfile updatedProfile = repository.GetUserProfileById("e40b1c7f-70bc-4bab-8c5a-fa87824d58a1");

            Assert.Equal("555-555-1234", updatedProfile.NotificationProfile.MobilePhoneNumber);
        }

        [Fact]
        public void VerifyGetUsersByCategory()
        {
            var repository = new UserProfileRepository(cosmosDbConnectionString);
            IList<UserProfile> profiles = repository.GetUsersByCategory("app-service");

            IList<UserProfile> noProfiles = repository.GetUsersByCategory("jhghjg");

            Assert.NotNull(profiles);
            Assert.NotEmpty(profiles);

            Assert.Empty(noProfiles);
        }

        [Fact]
        public void VerifyGetAllUserProfiles()
        {
            var repository = new UserProfileRepository(cosmosDbConnectionString);
            var profiles = repository.GetAllUserProfiles("1");

            Assert.NotNull(profiles);
            Assert.NotEmpty(profiles);
        }
    }
}
