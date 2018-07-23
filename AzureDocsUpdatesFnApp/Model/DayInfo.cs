using AzureDocsUpdates.Shared.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AzureDocsUpdatesFnApp.Model
{
    public class DayInfo
    {
        [JsonProperty(PropertyName = "id")]
        [JsonConverter(typeof(IsoShortDateTimeConverter))]
        public DateTime Id { get; set; }

        // Only workaround for order-by ("Primary key order-by is not supported")
        // Will change in the future: https://stackoverflow.com/a/48738591
        [JsonConverter(typeof(IsoShortDateTimeConverter))]
        public DateTime Date { get; set; }

        public bool DeepLinksGenerated { get; set; }

        public bool TitlesGenerated { get; set; }

        /// <summary>
        /// The changes per doc (file). 
        /// </summary>
        public List<DocChange> DocChanges { get; set; }
    }
}
