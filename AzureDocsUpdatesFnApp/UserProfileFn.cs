using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using AzureDocsUpdatesFnApp.Repositories;

namespace AzureDocsUpdatesFnApp
{
    public static class UserProfileFn
    {
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

            UserProfileRepository userProfileRepository = new UserProfileRepository();
            var userProfileList = userProfileRepository.GetUserProfilesByFrequency(frequency);

            return new JsonResult(userProfileList);
        }
    }
}
