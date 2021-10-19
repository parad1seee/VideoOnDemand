using Newtonsoft.Json;

namespace VideoOnDemand.Models.RequestModels
{
    public class BraintreeSubscriptionWebhookRequestModel
    {
        [JsonProperty("bt_signature")]
        public string bt_signature { get; set; }

        //[JsonProperty("bt_payload")]
        public string bt_payload { get; set; }
    }
}
