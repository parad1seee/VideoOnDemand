using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels.Chat
{
    public class ChatMessageBaseResponseModel
    {
        [JsonRequired]
        public int Id { get; set; }

        [JsonRequired]
        public int CreatorId { get; set; }

        [JsonRequired]
        public string CreatedAt { get; set; }

        [JsonRequired]
        public int ChatId { get; set; }

        [JsonRequired]
        public bool IsMyMessage { get; set; }

        [JsonRequired]
        public bool IsUnreadForMe { get; set; }

        [JsonRequired]
        public string MessageType { get; set; }

        [JsonRequired]
        public string MessageStatus { get; set; }

        public string Text { get; set; }

        public ImageResponseModel Image { get; set; }
    }
}
