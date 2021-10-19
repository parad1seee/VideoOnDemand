using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VideoOnDemand.Models.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestType
    {
        GET,
        POST
    }
}
