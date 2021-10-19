using VideoOnDemand.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels.Chat
{
    public class UserStatusResponseModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public UserConnectionStatus Status { get; set; }
    }
}
