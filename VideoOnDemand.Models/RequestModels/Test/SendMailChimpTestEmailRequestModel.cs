using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Test
{
    public class SendMailChimpTestEmailRequestModel
    {
        [Required]
        public string Subject { get; set; }

        [Required]
        public string FromEmail { get; set; }

        [Required]
        public string FromName { get; set; }

        [Required]
        public List<string> ToEmails { get; set; }

        [Required]
        public string TemplateName { get; set; }
    }
}
