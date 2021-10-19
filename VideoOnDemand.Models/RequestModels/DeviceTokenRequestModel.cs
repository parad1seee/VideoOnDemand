using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Models.RequestModels
{
    public class DeviceTokenRequestModel
    {
        [Required(ErrorMessage = "Device Token field is empty")]
        public string DeviceToken { get; set; }
    }
}
