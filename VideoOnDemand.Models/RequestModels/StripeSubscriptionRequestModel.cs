using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Models.RequestModels
{
    public class StripeSubscriptionRequestModel
    {
        [JsonProperty("subscriptionId")]
        [Required(ErrorMessage = "Subscription Id field is empty")]
        public string SubscriptionId { get; set; }
    }
}
