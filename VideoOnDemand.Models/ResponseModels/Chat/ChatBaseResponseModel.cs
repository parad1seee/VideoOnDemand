using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels.Chat
{
    public class ChatBaseResponseModel
    {
        [JsonRequired]
        public int ChatId { get; set; }

        [JsonRequired]
        public List<UserResponseModel> ChatUsers { get; set; }
    }
}
