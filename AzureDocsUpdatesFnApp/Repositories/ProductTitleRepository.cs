using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AzureDocsUpdatesFnApp.Repositories
{
    public static class ProductTitleRepository
    {
        private static object _lock = new Object();

        private static Dictionary<string, string> _folderProductTitleMapping = new Dictionary<string, string>();

        public static IReadOnlyDictionary<string, string> GetMapping(string functionAppDirectory)
        {
            EnsureMappingIsLoaded(functionAppDirectory);

            return _folderProductTitleMapping;
        }

        public static string MapFolderToProductTitle(string productFolderName, string functionAppDirectory)
        {
            EnsureMappingIsLoaded(functionAppDirectory);

            string productTitle;
            if (!_folderProductTitleMapping.TryGetValue(productFolderName, out productTitle))
            {
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                productTitle = textInfo.ToTitleCase(productFolderName.Replace('-', ' '));
                // TODO: log missing mapping
            }
            return productTitle;
        }

        private static void EnsureMappingIsLoaded(string functionAppDirectory)
        {
            if (_folderProductTitleMapping == null || _folderProductTitleMapping.Count == 0)
            {
                lock (_lock)
                {
                    if (_folderProductTitleMapping == null || _folderProductTitleMapping.Count == 0)
                    {
                        var mappingFile = Path.GetFullPath(Path.Combine(functionAppDirectory, "Model/FolderProductMapping.json"));
                        var json = File.ReadAllText(mappingFile);
                        _folderProductTitleMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    }
                }
            }
        }
    }
}
