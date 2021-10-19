using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.RequestModels
{
    public class PaginationBaseRequestModel
    {
        [JsonProperty("limit")]
        public int Limit { get; set; } = 10;

        [JsonProperty("offset")]
        public int Offset { get; set; } = 0;
    }
}
