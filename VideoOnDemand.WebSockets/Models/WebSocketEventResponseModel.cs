using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.WebSockets.Models
{
    public class WebSocketEventResponseModel
    {
        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
