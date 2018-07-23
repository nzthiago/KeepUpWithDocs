using AzureDocsUpdatesFnApp.Model;
using Octokit;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using AzureDocsUpdates.Shared.Model;
using System.Security.Cryptography;

namespace AzureDocsUpdatesFnApp.Services
{
    public class GitDataImportService
    {
        private const string GitHubToken = "e808d21cc44212bc1931ff29fac58f04c7b846ee";
        private const string GitHubOwner = "MicrosoftDocs";
        private const string GitHubRepName = "azure-docs";
        private const string subfolder = "articles/";

        private static readonly MD5 _md5 = MD5.Create();

        private IGitHubClient _gitHubClient = new GitHubClient(new ProductHeaderValue("AzureDocsUpdates"))
        {
            Credentials = new Credentials(GitHubToken)
        };
                
        public async Task RunAsync(
            DateTime importStartDate,
            DateTime importEndDate,
            IAsyncCollector<DayInfo> changesPerDayCollection, 
            IAsyncCollector<ProcessDayInfoForTitlesJobWorkerData> outputTitleWorkerStorage, 
            IAsyncCollector<ProcessDayInfoForTitlesJob> outputTitleQueue,
            TraceWriter log)
        {

            // TODO: improve
            // https://developer.github.com/v3/repos/commits/#list-commits-on-a-repository
            // https://developer.github.com/v3/#timezones
            var commits = await _gitHubClient.Repository.Commit.GetAll(GitHubOwner, GitHubRepName,
                                    new CommitRequest { Since = importStartDate, Until = importEndDate }
                                );
            
            log.Info($"Processing {commits.Count} commits");

            var commitsPerDays = commits.GroupBy(_ => _.Commit.Committer.Date.UtcDateTime.Date);
            log.Info($"Processing {commitsPerDays.Count()} days");

            foreach (var commitsPerDay in commitsPerDays)
            {
                var dayInfo = new DayInfo
                {
                    Id = commitsPerDay.Key,
                    Date = commitsPerDay.Key,
                    DocChanges = new List<DocChange>(),
                    DeepLinksGenerated = true
                };


                foreach (var commitOverview in commitsPerDay)
                {
                    var commitDetail = await _gitHubClient.Repository.Commit.Get(GitHubOwner, GitHubRepName, commitOverview.Sha);

                    foreach (var file in commitDetail.Files)
                    {
                        AddOrModifyFileChange(commitDetail.Sha, commitDetail.Parents.FirstOrDefault()?.Sha, file, dayInfo.DocChanges, commitOverview.Commit.Committer.Date.UtcDateTime, log);
                    }
                }

                if (dayInfo.DocChanges.Count > 0)
                {
                    await changesPerDayCollection.AddAsync(dayInfo);

                    var filesToProcess = GetAllFiles(dayInfo);
                    if (filesToProcess.Count > 0)
                    {
                        await outputTitleWorkerStorage.AddAsync(new ProcessDayInfoForTitlesJobWorkerData
                        {
                            DayInfoId = dayInfo.Date,
                            FilesToProcess = filesToProcess,
                        });

                        await outputTitleQueue.AddAsync(new ProcessDayInfoForTitlesJob()
                        {
                            DayInfoId = dayInfo.Date
                        });
                    }

                    log.Info($"Processed {dayInfo.DocChanges.Count} DocChanges for {dayInfo.Date}");
                }
                else
                {
                    log.Info($"No DocChanges for {dayInfo.Date}");
                }
            }

            await changesPerDayCollection.FlushAsync();
        }

        private string GetLongestCommonPrefix(IEnumerable<string> pathes)
        {
            if (pathes == null || pathes.Count() == 0)
                return String.Empty;

            const char SEPARATOR = '/';

            string[] prefixParts =
                pathes.Select(dir => dir.Split(SEPARATOR))
                .Aggregate(
                    (first, second) => first.Zip(second, (a, b) =>
                                            new { First = a, Second = b })
                                        .TakeWhile(pair => pair.First.Equals(pair.Second))
                                        .Select(pair => pair.First)
                                        .ToArray()
                );

            return string.Join(SEPARATOR.ToString(), prefixParts);
        }

        private List<string> GetAllFiles(DayInfo dayInfo)
        {
            return dayInfo.DocChanges
                .Where(_ => _.Commits[0].Status != ChangeStatus.Removed)
                .Select(_ => _.Url)
                .Distinct()
                .ToList();
        }

        private bool AddOrModifyFileChange(string commitSha, string parentSha, GitHubCommitFile file, List<DocChange> changes, DateTime utcDateTime, TraceWriter log)
        {
            var diffAnchorHash = BitConverter.ToString(_md5.ComputeHash(Encoding.UTF8.GetBytes(file.Filename)))
                .Replace("-", string.Empty).ToLower();

            if (!file.Filename.StartsWith(subfolder)
                            || !file.Filename.EndsWith(".md")
                            || file.Filename.EndsWith("/TOC.md", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var filename = file.Filename.Replace(subfolder, "").Replace("/", "\\");

            var product = Path.GetDirectoryName(filename);
            if (string.IsNullOrWhiteSpace(product)
                || product.Split('\\').Length == 0)
            {
                return false;
            }
            product = product.Split('\\')[0];
            filename = Path.GetFileName(filename);

            var url = file.ContentsUrl
                .Substring(0, file.ContentsUrl.IndexOf("?ref="))
                .Replace("https://api.github.com/repos/", "");

            var existingDocChange = changes
                .Where(_ => _.Url == url)
                .SingleOrDefault();

            ChangeStatus changeStatus;
            if (!Enum.TryParse(value: file.Status, ignoreCase: true, result: out changeStatus))
            {
                log.Warning($"Could not parse status {file.Status}.");
            }

            if (existingDocChange == null)
            {
                changes.Add(new DocChange
                {
                    Product = product,
                    File = Path.GetFileName(filename),
                    Url = url,
                    Commits = new List<DocChangeCommit>
                    {
                        new DocChangeCommit()
                        {
                            Sha = commitSha,
                            ParentSha = parentSha,
                            Status = changeStatus,
                            DeepLink = diffAnchorHash,
                            ChangedAt = utcDateTime
                        }
                    }
                });
            }
            else
            {
                existingDocChange.Commits.Add(
                    new DocChangeCommit()
                    {
                        Sha = commitSha,
                        ParentSha = parentSha,
                        Status = changeStatus,
                        DeepLink = diffAnchorHash,
                        ChangedAt = utcDateTime
                    }
                );
            }

            return true;
        }
    }
}
