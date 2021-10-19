using Newtonsoft.Json;
using VideoOnDemand.Models.RequestModels.Socials;

namespace VideoOnDemand.Models.ResponseModels
{
    public class FBProfileResponseModel
    {
        public string Id { get; set; }

        public string Email { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("data")]
        public FacebookImageReponseModel ImageData { get; set; }
    }
}
