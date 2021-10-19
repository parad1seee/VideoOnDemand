using VideoOnDemand.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoOnDemand.Domain.Entities.Chat
{
    public class ChatUser : IEntity
    {
        #region Properties

        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; }

        [Required]
        public int UserId { get; set; }

        public bool IsActive { get; set; }

        public int? LastReadMessageId { get; set; }

        #endregion

        #region Navigation properties

        [ForeignKey("ChatId")]
        [InverseProperty("Users")]
        public virtual Chat Chat { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Chats")]
        public virtual ApplicationUser User { get; set; }

        #endregion
    }
}
