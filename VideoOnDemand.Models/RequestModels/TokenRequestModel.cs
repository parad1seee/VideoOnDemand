using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class TokenRequestModel
    {
        [JsonProperty("token")]
        [Required(ErrorMessage = "Token field is empty")]
        public string Token { get; set; }
    }
}
