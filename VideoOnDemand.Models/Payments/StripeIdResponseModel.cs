using Newtonsoft.Json;

namespace VideoOnDemand.Models.Payments
{
    public class StripeIdResponseModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
