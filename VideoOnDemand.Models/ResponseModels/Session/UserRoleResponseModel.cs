using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels.Session
{
    public class UserRoleResponseModel : UserResponseModel
    {
        [JsonProperty("role")]
        public string Role { get; set; }
    }
}
