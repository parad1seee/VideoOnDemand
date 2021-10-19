using Newtonsoft.Json;
using VideoOnDemand.Models.ResponseModels.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels
{
    public class ChatResponseModel : ChatBaseResponseModel
    {
        public ChatMessageBaseResponseModel LastItem { get; set; }
    }
}
