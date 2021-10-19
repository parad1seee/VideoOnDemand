using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Test
{
    public class SendTestSMSRequestModel
    {
        [Required]
        public string Phone { get; set; }

        [Required]
        [MaxLength(30)]
        public string Text { get; set; }
    }
}
