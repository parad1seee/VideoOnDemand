using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Socials
{
    public class GoogleWithPhoneRequestModel
    {
        [Required(ErrorMessage = "Token is empty")]
        public string Token { get; set; }

        [RegularExpression(ModelRegularExpression.REG_PHONE, ErrorMessage = "Phone number is not in valid format")]
        [StringLength(14, ErrorMessage = "Phone number is not in valid format", MinimumLength = 13)]
        public string PhoneNumber { get; set; }
    }
}
