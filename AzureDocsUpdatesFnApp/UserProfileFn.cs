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

        [FunctionName("GetAllUserProfiles")]
        public static IActionResult GetAllUserProfiles(
                        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
                        TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            int frequency = 0;

            // TODO: Clean up / error handling
            string f = req.Query["frequency"];
            if (f != null)
            {
                frequency = int.Parse(f);
            }
            
            var userProfileList = userProfileRepository.GetUserProfilesByFrequency(frequency);

            return new JsonResult(userProfileList);
        }

        [FunctionName("CreateProfileFn")]
        public static async Task<IActionResult> CreateUserProfile([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
                            TraceWriter log)
        {
            log.Info("Attempting to create a new user profile.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var up = JsonConvert.DeserializeObject<UserProfile>(requestBody);

            Document createdProfile = await userProfileRepository.CreateUserProfile(up);

            log.Info($"Created new user profile with document ID '{createdProfile.Id}'.");

            // TODO: Fix this to provide a better location URI
            return new CreatedResult($"{createdProfile.Id}", createdProfile);
        }
    }
}
