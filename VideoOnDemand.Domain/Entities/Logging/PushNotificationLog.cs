using VideoOnDemand.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VideoOnDemand.Domain.Entities.Logging
{
    public class PushNotificationLog : IEntity
    {
        #region Properties

        public int Id { get; set; }

        [MaxLength(200)]
        public string DeviceToken { get; set; }

        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Body { get; set; }

        [MaxLength(8000)]
        public string DataJSON { get; set; }

        public SendingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        #endregion
    }
}
