using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels
{
    public class UserProfileRequestModel
    {
        [Required(ErrorMessage = "First Name field is empty")]
        [StringLength(30, ErrorMessage = "First Name must be from 1 to 30 symbols", MinimumLength = 1)]
        [RegularExpression(ModelRegularExpression.REG_MUST_NOT_CONTAIN_SPACES, ErrorMessage = "First Name must not contain spaces")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name field is empty")]
        [StringLength(30, ErrorMessage = "Last Name must be from 1 to 30 symbols", MinimumLength = 1)]
        [RegularExpression(ModelRegularExpression.REG_MUST_NOT_CONTAIN_SPACES, ErrorMessage = "Last Name must not contain spaces")]
        public string LastName { get; set; }

        public int? ImageId { get; set; }
    }
}
