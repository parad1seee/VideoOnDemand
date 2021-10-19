using VideoOnDemand.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Socials
{
    public class FacebookWithEmailRequestModel
    {
        [Required(ErrorMessage = "Token is empty")]
        public string Token { get; set; }

        [DataType(DataType.EmailAddress, ErrorMessage = "Email address is not in valid format")]
        [StringLength(129, ErrorMessage = "Email address is not in valid format", MinimumLength = 3)]
        [CustomRegularExpression(ModelRegularExpression.REG_EMAIL, ErrorMessage = "Email address is not in valid format")]
        [CustomRegularExpression(ModelRegularExpression.REG_EMAIL_DOMAINS, ErrorMessage = "Email address is not in valid format")]
        public string Email { get; set; }
    }
}
