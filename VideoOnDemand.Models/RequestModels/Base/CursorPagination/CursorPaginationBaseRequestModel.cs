using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Base.CursorPagination
{
    public class CursorPaginationBaseRequestModel
    {
        [JsonProperty("limit")]
        public int Limit { get; set; } = 10;

        [JsonProperty("lastId")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} is invalid")]
        public int? LastId { get; set; }
    }
}
