using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class BraintreePaymentRequestModel
    {
        [JsonProperty("amount")]
        [Required(ErrorMessage = "Amount field is empty")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Amount is invalid")]
        public decimal Amount { get; set; }

        [JsonProperty("nonce")]
        [Required(ErrorMessage = "Nonce field is empty")]
        public string Nonce { get; set; }
    }
}
