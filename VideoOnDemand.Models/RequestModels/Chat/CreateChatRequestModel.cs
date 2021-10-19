using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Models.RequestModels.Chat
{
    public class CreateChatRequestModel
    {
        [Required(ErrorMessage = "{0} is required")]
        public List<int> ChatOpponentsIds { get; set; }
    }
}
