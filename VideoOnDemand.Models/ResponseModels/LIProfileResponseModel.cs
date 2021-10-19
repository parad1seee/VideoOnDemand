using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoOnDemand.Models.ResponseModels
{
    public class LocalizedModel
    {
        [JsonProperty("localized")]
        public Dictionary<string, string> Localized { get; set; }
    }

    public class LIProfileResponseModel
    {
        public string Id { get; set; }

        [JsonProperty("firstName")]
        public LocalizedModel LocalizedFirstName { private get; set; }

        [JsonProperty("lastName")]
        public LocalizedModel LocalizedLastName { private get; set; }

        public string FirstName
        {
            get
            {
                return LocalizedFirstName.Localized?.First().Value;
            }
        }

        public string LastName
        {
            get
            {
                return LocalizedLastName.Localized?.First().Value;
            }
        }

        [JsonProperty("emailAddress")]
        public string Email { get; set; }
    }

    public class LinkedInEmailHanlde
    {
        [JsonProperty("emailAddress")]
        public string Email { get; set; }
    }

    public class LinkedInEmailElement
    {
        [JsonProperty("handle")]
        public string Handle { get; set; }

        [JsonProperty("handle~")]
        public LinkedInEmailHanlde EmailHanlde { get; set; }
    }

    public class LinkedInEmailResponseModel
    {
        [JsonProperty("elements")]
        public List<LinkedInEmailElement> Elements { get; set; }
    }
}
