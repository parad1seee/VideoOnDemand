using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels.Session
{
    public class RegisterResponseModel
    {
        [JsonRequired]
        public string Email { get; set; }
    }
}