using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels
{
    public class IdResponseModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
