using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class UserProfileFn
    {
        private static UserProfileRepository userProfileRepository = new UserProfileRepository();

        [FunctionName("GetAllUserProfilesFn")]
        public static IActionResult GetAllUserProfilesFn(
                        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/GetAllUserProfiles")]HttpRequest req, 
                        TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            
            var userProfileList = userProfileRepository.GetUserProfilesByFrequency();

            return new JsonResult(userProfileList);
        }

        [FunctionName("UpsertProfileFn")]
        public static async Task<IActionResult> UpsertUserProfile([HttpTrigger(AuthorizationLevel.Function, "put", Route = "api/UpsertUserProfile")]HttpRequest req,
                            TraceWriter log)
        {
            log.Info("Attempting to upsert a user profile.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var up = JsonConvert.DeserializeObject<UserProfile>(requestBody);

            Document profile = await userProfileRepository.UpdateUserProfile(up);

            log.Info($"Upserted user profile with document ID '{profile.Id}'.");

            // TODO: Fix this to provide a better location URI
            return new CreatedResult($"{profile.Id}", profile);
        }

        [FunctionName("GetUserProfileByEmailFn")]
        public static UserProfile GetUserProfileByEmailAddress(
                        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/GetUserProfileByEmailAddress")]HttpRequest req,
                        TraceWriter log)
        {
            UserProfile profile = null;

            log.Info("Attempting to retrieve profile for user by email address.");

            string emailAddress = req.Query["emailAddress"];
            if (!string.IsNullOrEmpty(emailAddress))
            {
                profile = userProfileRepository.GetUserProfileByEmailAddress(emailAddress);
            }

            return profile;
        }
    }
}
