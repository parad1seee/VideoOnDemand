using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels.Chat
{
    public class UnreadMessagesResponseModel
    {
        [JsonRequired]
        public int? UnreadMesagesInChat { get; set; }

        [JsonRequired]
        public int AllUnreadMesages { get; set; }

        public int? ReadTillMessageId { get; set; }

        [JsonRequired]
        public List<int> Messages { get; set; }
    }
}
