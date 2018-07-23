using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Repositories;
using AzureDocsUpdatesFnApp.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WilderMinds.RssSyndication;

namespace AzureDocsUpdatesFnApp
{
    public static class RssFeedFn
    {
        private static DocChangesRepository _docChangesRepository = new DocChangesRepository();

        [FunctionName("RssFeedFn")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed")]
                HttpRequest req, 
            ExecutionContext context,
            TraceWriter log)
        {
            // TODO: enable link for product filter
            var productFilter = new List<string>();
            if (req.Query.ContainsKey("products"))
            {
                productFilter = req.Query["products"].First().Split(',').ToList();
            }

            var result = await _docChangesRepository.GetChangesPerDayAsync(1, null);

            var feed = new Feed
            {
                Title = "Azure Documentation Updates",
                Link = new System.Uri("https://azuredocsupdates.azurewebsites.net/"),
                //Copyright = "Copyright",
                // TODO: Description = ""
            };
            feed.Items = GenerateFeed(result, productFilter, context.FunctionAppDirectory).ToList();


            var actionResult = new CacheHeaderAction(TimeSpan.FromHours(4), new ContentResult
            {
                Content = feed.Serialize(),
                ContentType = "text/xml",
                StatusCode = 200
            });
            return actionResult;
        }

        private static IEnumerable<Item> GenerateFeed(IEnumerable<DayInfo> dayChangeInfos, IEnumerable<string> productFilter, string functionAppDirectory)
        {
            foreach (var dayChangeInfo in dayChangeInfos)
            {
                var products = dayChangeInfo.DocChanges.Select(_ => _.Product).Distinct().ToList();
                if (productFilter.Count() == 0 || products.Intersect(productFilter).Count() > 0)
                {
                    var categories = products
                        .Where(_ => productFilter.Count() == 0 || productFilter.Contains(_))
                        .Select(_ => ProductTitleRepository.MapFolderToProductTitle(_, functionAppDirectory))
                        .OrderBy(_ => _)
                        .ToList();

                    yield return new Item
                    {
                        PublishDate = dayChangeInfo.Date,
                        Title = "Azure Docs Updates for " + dayChangeInfo.Date.ToString("dd. MMMM yyyy", new CultureInfo("en-US")),
                        Categories = categories,
                        Link = new Uri("https://azuredocsupdates.azurewebsites.net/?date="
                                        + dayChangeInfo.Date.ToString("yyyy-MM-dd")),
                        Body = GenerateItemBody(dayChangeInfo, productFilter, functionAppDirectory)
                    };
                }
            }
        }

        private static string GenerateItemBody(DayInfo dayChangeInfo, IEnumerable<string> productFilter, string functionAppDirectory)
        {
            var bodyHtml = new StringBuilder();
            bodyHtml.Append("<ul>");

            var docChangesPerProducts = dayChangeInfo.DocChanges
                .Where(_ => productFilter.Count() == 0 || productFilter.Contains(_.Product))
                .GroupBy(_ => _.Product)
                .OrderBy(_ => ProductTitleRepository.MapFolderToProductTitle(_.Key, functionAppDirectory));

            foreach (var docChangesPerProduct in docChangesPerProducts)
            {
                var added = docChangesPerProduct.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Added));
                var modified = docChangesPerProduct.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Modified));
                var deleted = docChangesPerProduct.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Removed));

                bodyHtml.Append("<li>");
                bodyHtml.Append($"{ProductTitleRepository.MapFolderToProductTitle(docChangesPerProduct.Key, functionAppDirectory)}: ");
                bodyHtml.Append(added > 0 ? $"{added} added, " : "");
                bodyHtml.Append(deleted > 0 ? $"{deleted} deleted, " : "");
                bodyHtml.Append(modified > 0 ? $"{modified} modified, " : "");
                bodyHtml.Remove(bodyHtml.Length - 2, 2);
                bodyHtml.Append(" articles");
                bodyHtml.AppendLine("</li>");
            }

            bodyHtml.Append("</ul>");
            return bodyHtml.ToString();
        }
    }
}
