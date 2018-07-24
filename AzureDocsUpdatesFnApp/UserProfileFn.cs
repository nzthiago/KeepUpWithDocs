using System;
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
        [FunctionName("GetAllUserProfilesFn")]
        public static IActionResult GetAllUserProfilesFn(
                        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, 
                        TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            
            UserProfileRepository userProfileRepository = new UserProfileRepository();
            var userProfileList = userProfileRepository.GetUserProfilesByFrequency();

            return new JsonResult(userProfileList);
        }
    }
}
