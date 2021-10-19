using Newtonsoft.Json;
using VideoOnDemand.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class ResetPasswordWithPhoneRequestModel : PhoneNumberRequestModel
    {
        [JsonProperty("code")]
        [Required(ErrorMessage = "SMS code is empty")]
        [RegularExpression(ModelRegularExpression.REG_SMS_CODE, ErrorMessage = "SMS code contains invalid characters")]
        [StringLength(4, ErrorMessage = "SMS code is not valid. Add correct code or re-send it", MinimumLength = 4)]
        public string Code { get; set; }

        [Required(ErrorMessage = "Password field is empty")]
        [CustomRegularExpression(ModelRegularExpression.REG_ONE_LATER_DIGIT, ErrorMessage = "Password should contain at least one letter and one digit")]
        [CustomRegularExpression(ModelRegularExpression.REG_NOT_CONTAIN_SPACES_ONLY, ErrorMessage = "Password can’t contain spaces only")]
        [StringLength(50, ErrorMessage = "Password should be from 6 to 50 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password field is empty")]
        [Compare("Password", ErrorMessage = "Confirm Password isn’t the same as Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
