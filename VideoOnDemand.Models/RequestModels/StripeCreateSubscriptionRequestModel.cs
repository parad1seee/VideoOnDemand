using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class StripeCreateSubscriptionRequestModel
    {
        [JsonProperty("planId")]
        [Required(ErrorMessage = "Plan Id field is empty")]
        public string PlanId { get; set; }
    }
}
