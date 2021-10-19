using Newtonsoft.Json;
using System.Collections.Generic;

namespace VideoOnDemand.Models.Notifications
{
    public class PushNotificationData
    {
        public PushNotificationData(int userId, string action, string threadId = null)
        {
            UserId = userId;
            Action = action;
            ThreadId = threadId;
        }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("thread-id")]
        public string ThreadId { get; set; }

        public Dictionary<string, string> GetDictionary()
        {
            return new Dictionary<string, string>
            {
                {"userId", UserId.ToString() },
                {"action", Action },
                {"thread-id", ThreadId }
            };
        }

        public Dictionary<string, string> GetAndroidDictionary(string title, string body, int badge)
        {
            return new Dictionary<string, string>
            {
                {"userId", UserId.ToString() },
                {"action", Action },
                {"title", title },
                {"body", body },
                {"badge", badge.ToString() },
                {"thread-id", ThreadId }
            };
        }

        public Dictionary<string, object> GetData()
        {
            return new Dictionary<string, object>
            {
                {"userId", UserId },
                {"action", Action },
            };
        }
    }
}