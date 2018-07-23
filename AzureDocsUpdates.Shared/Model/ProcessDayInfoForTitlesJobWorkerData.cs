using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzureDocsUpdates.Shared.Model
{
    public class ProcessDayInfoForTitlesJobWorkerData
    {
        [JsonProperty(PropertyName = "dayInfoId")]
        [JsonConverter(typeof(IsoShortDateTimeConverter))]
        public DateTime DayInfoId { get; set; }

        public string id { get { return DayInfoId.ToString(new IsoShortDateTimeConverter().DateTimeFormat); } }

        public List<string> FilesToProcess { get; set; }
    }
}