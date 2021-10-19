using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class StripePaymentRequestModel
    {
        [JsonProperty("cardToken")]
        [Required(ErrorMessage = "Card Token field is empry")]
        public string CardToken { get; set; }

        [JsonProperty("amount")]
        [Required(ErrorMessage = "Amount field is empty")]
        [Range(0, long.MaxValue, ErrorMessage = "Amount is invalid")]
        public long Amount { get; set; }

        [JsonProperty("currency")]
        [Required(ErrorMessage = "Currency field is empty")]
        public string Currency { get; set; }
    }
}
