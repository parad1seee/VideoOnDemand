using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoOnDemand.Domain.Entities.Logging
{
    public class EmailRecipient
    {
        #region Properties

        public int Id { get; set; }

        public int LogId { get; set; }

        [MaxLength(129)]
        public string Email { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey("LogId")]
        [InverseProperty("EmailRecepients")]
        public EmailLog EmailLog { get; set; }

        #endregion
    }
}
