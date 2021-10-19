using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class BraintreeSubscriptionRequestModel
    {
        [JsonProperty("paymentMethodToken")]
        [Required(ErrorMessage = "Payment Method Token field is empty")]
        public string PaymentMethodToken { get; set; }

        [JsonProperty("planId")]
        [Required(ErrorMessage = "Plan Id field is empty")]
        public string PlanId { get; set; }
    }
}