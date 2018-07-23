using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp.Utils
{
    public class CacheHeaderAction : IActionResult
    {
        private IActionResult _innerActionResult;

        public CacheHeaderAction(TimeSpan duration, IActionResult innerActionResult)
        {
            Duration = duration;
            _innerActionResult = innerActionResult;
        }

        //     Gets or sets the duration in seconds for which the response is cached. This sets
        //     "max-age" in "Cache-control" header.
        public TimeSpan Duration { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            try
            {
                context.HttpContext.Response.Headers["Cache-Control"] = new StringValues($"public, max-age={(int)Duration.TotalSeconds}");
            }
            catch (Exception ex)
            {
                var test = ex.ToString();
                throw;
            }

            await _innerActionResult.ExecuteResultAsync(context);
        }
    }
}
