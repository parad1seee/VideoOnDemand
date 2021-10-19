using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels
{
    public class GProfileResponseModel
    {
        [JsonProperty("sub")]
        public string Id { get; set; }

        public string Email { get; set; }

        [JsonProperty("given_name")]
        public string FirstName { get; set; }

        [JsonProperty("family_name")]
        public string LastName { get; set; }

        [JsonProperty("picture")]
        public string Picture { get; set; }
    }
}
