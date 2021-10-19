using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels
{
    public class CheckResetPasswordTokenResponseModel
    {
        [JsonRequired]
        public bool IsValid { get; set; }
    }
}