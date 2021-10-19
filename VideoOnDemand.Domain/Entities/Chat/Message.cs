using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoOnDemand.Domain.Entities.Chat
{
    public class Message : IEntity
    {
        #region Properties

        [Key]
        public int Id { get; set; }

        [Required]
        public int CreatorId { get; set; }

        [Required]
        public int ChatId { get; set; }

        public DateTime CreatedAt { get; set; }

        public MessageType MessageType { get; set; }

        public MessageStatus MessageStatus { get; set; }

        [MaxLength(300)]
        public string Text { get; set; }

        public int? ImageId { get; set; }

        public bool IsActive { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey("CreatorId")]
        [InverseProperty("Messages")]
        public virtual ApplicationUser Creator { get; set; }

        [ForeignKey("ChatId")]
        [InverseProperty("Messages")]
        public virtual Chat Chat { get; set; }

        [ForeignKey("ImageId")]
        public virtual Image Image { get; set; }

        #endregion
    }
}
