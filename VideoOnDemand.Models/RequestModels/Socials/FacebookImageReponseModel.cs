using Newtonsoft.Json;

namespace VideoOnDemand.Models.RequestModels.Socials
{
    public class FacebookImageReponseModel
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("is_silhouette")]
        public bool Is_silhouette { get; set; }

        public FacebookImageReponseModel() { }

        public FacebookImageReponseModel(string url)
        {
            this.Url = url;
        }
    }
}
