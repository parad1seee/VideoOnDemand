using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace VideoOnDemand.Domain.Entities.Payment
{
    public class StripeSubscription : IEntity
    {
        #region Properties

        [Key]
        public int Id { get; set; }

        [Required]
        public string SubscriptionId { get; set; }

        [DataType("DateTime")]
        public DateTime CreatedAt { get; set; }

        [DataType("DateTime")]
        public DateTime? EndedAt { get; set; }

        [DataType("DateTime")]
        public DateTime? TrialEnd { get; set; }

        public StripeSubscriptionStatus Status { get; set; }

        public int UserId { get; set; }

        #endregion

        #region Navigation Properties

        [InverseProperty("StripeSubscriptions")]
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        #endregion
    }
}
