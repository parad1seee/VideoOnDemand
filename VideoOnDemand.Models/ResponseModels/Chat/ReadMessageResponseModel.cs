using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels.Chat
{
    public class ReadMessageResponseModel
    {
        [JsonRequired]
        public int ChatId { get; set; }

        [JsonRequired]
        public List<int> Messages { get; set; }
    }
}
