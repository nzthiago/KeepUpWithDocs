using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Repositories;
using Microsoft.Azure.Documents;
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

        //[Fact]
        //public void VerifyGetUserProfileById()
        //{
        //    var repository = new UserProfileRepository(cosmosDbConnectionString);
        //    var profile = repository.GetUserProfileById("f5bdc22b-5ecb-4ad4-abc3-412d80b30a36");

        //    Assert.NotNull(profile);
        //}

        [Fact]
        public void VerifyGetUserProfileByEmailAddress()
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
                    Categories = new string[] { "app-service", "cosmos-db" },
                    Frequency = 1
                }
            };

            repository.CreateUserProfile(profile);


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
                    Categories = new string[] { "app-service", "cosmos-db"},
                    Frequency = 1
                }
            };

            var repository = new UserProfileRepository(cosmosDbConnectionString);
            Document createdProfile = await repository.CreateUserProfile(profile);

            Assert.NotNull(createdProfile);
        }

        //TODO: Need to fix up this test!

        //[Fact]
        //public async Task VerifyUpdateUserProfile()
        //{
        //    var repository = new UserProfileRepository(cosmosDbConnectionString);
        //    UserProfile profile = repository.GetUserProfileById("f5bdc22b-5ecb-4ad4-abc3-412d80b30a36");

        //    profile.NotificationProfile.MobilePhoneNumber = "555-555-1234";
        //    await repository.UpdateUserProfile(profile);

        //    UserProfile updatedProfile = repository.GetUserProfileById("f5bdc22b-5ecb-4ad4-abc3-412d80b30a36");

        //    Assert.Equal("555-555-1234", updatedProfile.NotificationProfile.MobilePhoneNumber);
        //}

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
    }
}
