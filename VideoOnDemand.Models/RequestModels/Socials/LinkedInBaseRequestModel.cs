using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Socials
{
    public class LinkedInBaseRequestModel
    {
        [Required(ErrorMessage = "Access token is required")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Redirect uri is required")]
        public string RedirectUri { get; set; }
    }
}
