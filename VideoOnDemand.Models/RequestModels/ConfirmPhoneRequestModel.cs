using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class ConfirmPhoneRequestModel : PhoneNumberRequestModel
    {
        [Required(ErrorMessage = "SMS code is empty")]
        [RegularExpression(ModelRegularExpression.REG_SMS_CODE, ErrorMessage = "SMS code contains invalid characters")]
        [StringLength(4, ErrorMessage = "SMS code is not valid. Add correct code or re-send it", MinimumLength = 4)]
        public string Code { get; set; }
    }
}
