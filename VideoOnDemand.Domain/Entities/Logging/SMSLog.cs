using VideoOnDemand.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Domain.Entities.Logging
{
    public class SMSLog : IEntity
    {
        #region Properties

        public int Id { get; set; }

        [MaxLength(15)]
        public string Sender { get; set; }

        [MaxLength(15)]
        public string Recipient { get; set; }

        [MaxLength(200)]
        public string Text { get; set; }

        public SendingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        #endregion
    }

}
