using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Chat;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Chat;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface IChatService
    {
        /// <summary>
        /// Create chat
        /// </summary>
        /// <param name="model">List of chat opponents</param>
        /// <returns></returns>
        Task<ChatBaseResponseModel> CreateChat(CreateChatRequestModel model);

        /// <summary>
        /// Get chat by id 
        /// </summary>
        /// <param name="id">Chat id</param>
        /// <returns></returns>
        Task<ChatBaseResponseModel> GetChat(int id);

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="chatId">Id of Chat</param>
        /// <param name="model">Model with text or image message</param>
        /// <returns></returns>
        Task<ChatMessageResponseModel> SendMessage(int chatId, ChatMessageRequestModel model);

        /// <summary>
        /// Get chat list
        /// </summary>
        /// <param name="model">Chat list pagination model</param>
        /// <returns></returns>
        Task<PaginationResponseModel<ChatResponseModel>> GetChatList(PaginationBaseRequestModel model);

        /// <summary>
        /// Get list of messages
        /// </summary>
        /// <param name="model">Message list pagination model</param>
        /// <returns></returns>
        Task<PaginationResponseModel<ChatMessageBaseResponseModel>> GetMessages(int chatid, ChatMessagesListRequestModel model);

        /// <summary>
        /// Read messages
        /// </summary>
        /// <param name="model">message read model</param>
        /// <returns></returns>
        Task<UnreadMessagesResponseModel> ReadMessages(int chatid, ReadMessagesRequestModel model);

        /// <summary>
        /// Get unread messages count
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        int GetAllUnreadMessagesCount(int userId);

        /// <summary>
        /// Check if user have any connections
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        Task<UserStatusResponseModel> GetUserStatus(int userId);
    }
}
