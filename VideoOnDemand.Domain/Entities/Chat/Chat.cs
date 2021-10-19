using VideoOnDemand.Common.Extensions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VideoOnDemand.Domain.Entities.Chat
{
    public class Chat : IEntity
    {
        #region Properties

        [Key]
        public int Id { get; set; }

        public int? LastMessageId { get; set; }

        #endregion

        #region Navigation Properties

        [InverseProperty("Chat")]
        public virtual ICollection<ChatUser> Users { get; set; }

        public virtual ICollection<Message> Messages { get; set; }

        [ForeignKey("LastMessageId")]
        public virtual Message LastMessage { get; set; }

        #endregion

        public Chat()
        {
            Messages = Messages.Empty();
            Users = Users.Empty();
        }

        public int GetBadge(int userId)
        {
            var lastReaded = GetUserLastReadMessageId(userId);

            var messages = Messages.Where(x => x.CreatorId != userId && x.Id > lastReaded);

            return messages.Count();
        }

        public int GetUserLastReadMessageId(int userId)
        {
            // Get user last readed message id
            var lastReaded = Users?.FirstOrDefault(x => x.UserId == userId)?.LastReadMessageId;

            if (lastReaded.HasValue)
                return lastReaded.Value;
            else
                return 0;
        }
    }
}
