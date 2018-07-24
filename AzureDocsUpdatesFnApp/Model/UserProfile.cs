using Newtonsoft.Json;

namespace AzureDocsUpdatesFnApp.Model
{
    public class UserProfile
    {
        [JsonProperty(PropertyName ="id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "contactProfile")]
        public ContactProfile ContactProfile { get; set; }

        [JsonProperty(PropertyName = "notificationProfile")]
        public NotificationProfile NotificationProfile { get; set; }
    }

    public class ContactProfile
    {
        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty(PropertyName = "mobilePhoneNumber")]
        public string MobilePhoneNumber { get; set; }

        [JsonProperty(PropertyName = "firstName")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "lastName")]
        public string LastName { get; set; }
    }

    public class NotificationProfile
    {
        [JsonProperty(PropertyName = "categories")]
        public string[] Categories { get; set; }

        [JsonProperty(PropertyName = "frequency")]
        public int Frequency { get; set; }

        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty(PropertyName = "webhookUrl")]
        public string WebhookUrl { get; set; }

        [JsonProperty(PropertyName = "mobilePhoneNumber")]
        public string MobilePhoneNumber { get; set; }
    }

}