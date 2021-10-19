using Newtonsoft.Json;
using VideoOnDemand.Models.Enums;

namespace VideoOnDemand.Models.RequestModels.Base.CursorPagination
{
    public class CursorPaginationRequestModel<T> : CursorPaginationBaseRequestModel where T : struct
    {
        [JsonProperty("search")]
        public string Search { get; set; }

        [JsonProperty("order")]
        public OrderingRequestModel<T, SortingDirection> Order { get; set; }
    }
}
