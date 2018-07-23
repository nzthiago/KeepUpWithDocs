using System.Collections.Generic;
using System.Globalization;

namespace AzureDocsUpdatesFnApp.Model
{
    public class DocChange
    {
        private string _title;

        public string Product { get; set; }

        public string File { get; set; }

        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_title))
                {
                    var textInfo = new CultureInfo("en-US", false).TextInfo;
                    return textInfo.ToTitleCase(File.Replace('-', ' ').Replace(".md", ""));
                }
                return _title;
            }
            set { _title = value; }
        }

        public string Url { get; set; }

        /// <summary>
        /// The commits associated with this doc (file).
        /// </summary>
        public List<DocChangeCommit> Commits { get; set; }
    }
}
