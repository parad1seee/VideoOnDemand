using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoOnDemand.Domain.Entities.Logging
{
    public class EmailLog : IEntity
    {
        #region Properties

        public int Id { get; set; }

        [MaxLength(129)]
        public string Sender { get; set; }

        [MaxLength(8000)]
        public string EmailBody { get; set; }

        public SendingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        #endregion

        #region Navigation Properties

        [InverseProperty("EmailLog")]
        public virtual ICollection<EmailRecipient> EmailRecepients { get; set; }

        #endregion

        #region Ctors

        public EmailLog()
        {
            EmailRecepients = EmailRecepients.Empty();
        }

        #endregion
    }
}
