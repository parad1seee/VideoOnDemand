using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class CheckResetPasswordTokenRequestModel : EmailRequestModel
    {
        [Required(ErrorMessage = "Token is empty")]
        public string Token { get; set; }
    }
}
