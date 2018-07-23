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
using System.Linq;
using System.Threading.Tasks;

namespace AzureDocsUpdatesFnApp
{
    public static class ChangeFeeedFn
    {
        private static DocChangesRepository _docChangesRepository = new DocChangesRepository();

        private static DateTime _cacheUpdated;
        private static Dictionary<string, IEnumerable<DayInfo>> _cache = new Dictionary<string, IEnumerable<DayInfo>>();

        [FunctionName("ChangeFeeedFn")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "api/ChangeFeeed")] HttpRequest req,
            ExecutionContext context,
            TraceWriter log)
        {
            int requestedPage = 1;
            if (req.Query.ContainsKey("page")
                && (!int.TryParse(req.Query["page"], out requestedPage)
                    || requestedPage > _docChangesRepository.MaxPages))
            {
                return new BadRequestObjectResult("Please pass the page on the query string or in the request body");
            }

            var productFilter = new List<string>();
            if (req.Query.ContainsKey("products"))
            {
                productFilter = req.Query["products"].First().Split(',').ToList();
            }

            string dateFilter = null;
            if (req.Query.ContainsKey("date"))
            {
                dateFilter = req.Query["date"].First();
            }

            // TODO: lock
            var cacheKey = $"ChangesPerDay_Page{requestedPage}_Date{dateFilter}";
            if (_cacheUpdated <= DateTime.UtcNow.AddHours(-2) || !_cache.ContainsKey(cacheKey))
            {
                _cache[cacheKey] = await _docChangesRepository.GetChangesPerDayAsync(requestedPage, dateFilter);
                _cacheUpdated = DateTime.UtcNow;
            }
            var results = _cache[cacheKey];

            // TODO: remove 'diff-' at diff anchors processing
            results.SelectMany(_ => _.DocChanges)
                   .SelectMany(_ => _.Commits)
                   .Where(_ => !string.IsNullOrWhiteSpace(_.DeepLink))
                   .ToList()
                   .ForEach(_ => _.DeepLink = _.DeepLink.Replace("diff-", ""));

            var inMemoryQuery = (from c in results
                                 group c by c.Id into g
                                 select new
                                 {
                                     ChangedAtDate = g.Key,
                                     Products = g.SelectMany(_ => _.DocChanges)
                                                 .Where(_ => productFilter.Count == 0 || productFilter.Contains(_.Product))
                                                 .GroupBy(_ => _.Product)
                                                 .Select(_ => new
                                                 {
                                                     Product = ProductTitleRepository.MapFolderToProductTitle(_.Key, context.FunctionAppDirectory),
                                                     Added = _.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Added)),
                                                     Modified = _.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Modified)),
                                                     Deleted = _.Sum(i => i.Commits.Count(commit => commit.Status == ChangeStatus.Removed)),
                                                     Changes = _.OrderByDescending(c => c.Commits, new CommitComparer())
                                                 })
                                                 .OrderBy(_ => _.Product)
                                 }).OrderByDescending(_ => _.ChangedAtDate);

            var result = new CacheHeaderAction(TimeSpan.FromHours(2), new JsonResult(inMemoryQuery.ToList()));
            return result;
        }
    }

    internal class CommitComparer : IComparer<List<DocChangeCommit>>
    {
        public int Compare(List<DocChangeCommit> x, List<DocChangeCommit> y)
        {
            var xSum = x.Sum(_ => GetOrderingValue(_.Status));
            var ySum = y.Sum(_ => GetOrderingValue(_.Status));

            if (xSum < ySum)
            {
                return -1;
            }
            if (xSum > ySum)
            {
                return 1;
            }
            return 0;
        }

        private int GetOrderingValue(ChangeStatus status)
        {
            switch (status)
            {
                case ChangeStatus.Added:
                    return 100;
                case ChangeStatus.Removed:
                    return 10;
                case ChangeStatus.Other:
                case ChangeStatus.Modified:
                case ChangeStatus.Renamed:
                case ChangeStatus.Copied:
                default:
                    return 0;
            }
        }
    }
}
