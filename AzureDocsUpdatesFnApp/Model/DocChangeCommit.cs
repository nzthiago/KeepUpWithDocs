using System;

namespace AzureDocsUpdatesFnApp.Model
{
    public class DocChangeCommit
    {
        public string Sha { get; set; }

        public string ParentSha { get; set; }

        public ChangeStatus Status { get; set; }

        public string DeepLink { get; set; }

        public DateTime ChangedAt { get; set; }
    }
}
