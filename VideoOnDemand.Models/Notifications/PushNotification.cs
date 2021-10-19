using Newtonsoft.Json;

namespace VideoOnDemand.Models.Notifications
{
    public class PushNotification
    {
        public PushNotification(string title, string body, PushNotificationData data = null)
        {
            Title = title;
            Body = body;
            Data = data;
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("data")]
        public PushNotificationData Data { get; set; }
    }
}