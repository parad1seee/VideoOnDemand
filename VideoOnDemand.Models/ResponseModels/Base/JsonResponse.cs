using Newtonsoft.Json;

namespace VideoOnDemand.Models.ResponseModels
{
    public class JsonResponse<T>
    {
        public JsonResponse(T newdata)
        {
            Data = newdata;
        }

        [JsonRequired]
        [JsonProperty("_v")]
        public string Version { get; set; } = "1.0";

        //[JsonRequired]
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
