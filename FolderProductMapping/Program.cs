using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FolderProductMapping
{
    class Program
    {
        private const string GitHubToken = "e808d21cc44212bc1931ff29fac58f04c7b846ee";
        private const string GitHubOwner = "MicrosoftDocs";
        private const string GitHubRepName = "azure-docs";

        private static IGitHubClient _gitHubClient = new GitHubClient(new ProductHeaderValue("AzureDocsUpdates"))
        {
            Credentials = new Credentials(GitHubToken)
        };

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var mapping = new Dictionary<string, string>();
            var textInfo = new CultureInfo("en-US", false).TextInfo;

            var articlesDir = await _gitHubClient.Repository.Content.GetAllContents(GitHubOwner, GitHubRepName, "articles");
            foreach (var productDir in articlesDir.Where(_ => _.Type.Value == ContentType.Dir))
            {
                var productName = productDir.Path.Split('/')[1];
                var productTitle = textInfo
                    .ToTitleCase(productName.Replace('-', ' '))
                    .Replace("iot", "IoT", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("sql", "SQL", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("dns", "DNS", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("vpn", "VPN", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("bi", "BI", StringComparison.InvariantCultureIgnoreCase);

                try
                {
                    var indexYml = await _gitHubClient.Repository.Content.GetAllContents(GitHubOwner, GitHubRepName, productDir.Path + "/index.yml");
                    var ymlContent = indexYml[0].Content;

                    foreach (var line in ymlContent.Split('\n'))
                    {
                        if (line.StartsWith("title: "))
                        {
                            productTitle = line.Replace("title: ", "").Replace("Documentation", "").Trim();
                        }
                    }
                }
                catch (NotFoundException)
                {
                }
                finally
                {
                    mapping.Add(productName, productTitle);
                }
            }

            var json = JsonConvert.SerializeObject(mapping, Formatting.Indented);
            File.WriteAllText("../AzureDocsUpdatesFnApp/Model/FolderProductMapping.json", json);
        }
    }
}
