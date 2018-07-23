using System;
using Newtonsoft.Json;

namespace AzureDocsUpdates.Shared.Model
{
    public class ProcessDayInfoForTitlesJob
    {
        [JsonProperty(PropertyName = "dayInfoId")]
        [JsonConverter(typeof(IsoShortDateTimeConverter))]
        public DateTime DayInfoId { get; set; }

        public string id { get { return DayInfoId.ToString(new IsoShortDateTimeConverter().DateTimeFormat); } }
    }
}