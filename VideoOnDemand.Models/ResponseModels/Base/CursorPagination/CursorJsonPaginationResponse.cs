using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels.Base.CursorPagination
{
    public class CursorJsonPaginationResponse<T> : JsonResponse<T> where T : class
    {
        public CursorJsonPaginationResponse(T newdata, int? lastId) 
            : base(newdata)
        {

            Pagination = new CursorPaginationModel()
            {
                LastId = lastId
            };

        }
        [JsonRequired]
        [JsonProperty("pagination")]
        public CursorPaginationModel Pagination { get; set; }
    }

    public class CursorPaginationModel
    {
        [JsonProperty("lastId")]
        public int? LastId { get; set; }
    }
}
