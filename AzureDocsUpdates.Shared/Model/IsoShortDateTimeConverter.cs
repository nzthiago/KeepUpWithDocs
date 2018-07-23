using Newtonsoft.Json.Converters;

namespace AzureDocsUpdates.Shared.Model
{
    public class IsoShortDateTimeConverter : IsoDateTimeConverter
    {
        public IsoShortDateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-ddT";
        }
    }
}
