using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities;
using VideoOnDemand.Domain.Entities.Chat;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.Notifications;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Chat;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Chat;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.WebSockets.Constants;
using VideoOnDemand.WebSockets.Handlers;
using VideoOnDemand.WebSockets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services
{
    public class ChatService : IChatService
    {
        private IUnitOfWork _unitOfWork;
        private WebSocketMessageHandler _webSocketHandler;
        private INotificationService _notificationService;
        private IMapper _mapper;
        
        private int? _userId = null;

        public ChatService(IUnitOfWork unitOfWork, WebSocketMessageHandler webSocketHandler, IMapper mapper, IHttpContextAccessor httpContextAccessor, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _webSocketHandler = webSocketHandler;
            _notificationService = notificationService;
            _mapper = mapper;

            var context = httpContextAccessor.HttpContext;

            if (context?.User != null)
            {
                try
                {
                    _userId = context.User.GetUserId();
                }
                catch
                {
                    _userId = null;
                }
            }
        }

        public async Task<ChatBaseResponseModel> CreateChat(CreateChatRequestModel model)
        {
            // Get opponents id list
            var chatUsersIds = model.ChatOpponentsIds.Distinct().ToList();

            if (!chatUsersIds.Any())
                throw new CustomException(HttpStatusCode.BadRequest, "chatOpponents", "Chat must have at least 2 users");

            var creator = await GetUser(_userId.Value);

            // Get chat users
            var chatUsers = await _unitOfWork.Repository<ApplicationUser>().Get(x => chatUsersIds.Contains(x.Id) && x.IsActive)
                .TagWith(nameof(CreateChat) + "_GetUsersForChat")
                .Include(x => x.Profile)
                .ToListAsync();

            var chat = new Chat();
            chat.Users.Add(new ChatUser { UserId = creator.Id, IsActive = true, LastReadMessageId = 0 });

            foreach (var id in chatUsersIds)
            {
                var item = chatUsers.FirstOrDefault(x => x.Id == id);
                if (item == null)
                    throw new CustomException(HttpStatusCode.BadRequest, "id", $"Can't find user with given id {id}");
                else
                    chat.Users.Add(new ChatUser { UserId = item.Id, IsActive = true, LastReadMessageId = 0 });
            }

            _unitOfWork.Repository<Chat>().Insert(chat);
            _unitOfWork.SaveChanges();

            foreach (var user in chat.Users)
            {
                _webSocketHandler.AddToGroup(user.Id, user.ChatId);
            }

            var result = new ChatBaseResponseModel
            {
                ChatId = chat.Id,
                ChatUsers = _mapper.Map<List<UserResponseModel>>(chat.Users.Select(x => x.User.Profile))
            };

            return result;
        }

        public async Task<ChatBaseResponseModel> GetChat(int id)
        {
            var chat = await _unitOfWork.Repository<Chat>().Get(x => x.Id == id)
                 .TagWith(nameof(GetChat) + "_GetChat")
                 .Include(x => x.Users)
                     .ThenInclude(x => x.User)
                         .ThenInclude(x => x.Profile)
                 .FirstOrDefaultAsync();

            if (chat == null || !chat.Users.Any(x => x.UserId == _userId.Value && x.IsActive))
                throw new CustomException(HttpStatusCode.BadRequest, "ChatId", $"Can't find chat with such id {id}");

            var result = new ChatBaseResponseModel
            {
                ChatId = chat.Id,
                ChatUsers = _mapper.Map<List<UserResponseModel>>(chat.Users.Select(x => x.User.Profile))
            };

            return result;
        }

        public async Task<ChatMessageResponseModel> SendMessage(int chatId, ChatMessageRequestModel model)
        {
            // Check chat message model
            if ((model.ImageId.HasValue && !string.IsNullOrEmpty(model.Text)) || (!model.ImageId.HasValue && string.IsNullOrEmpty(model.Text)))
                throw new CustomException(HttpStatusCode.BadRequest, "Message", "Invalid message");

            var sender = await GetUser(_userId.Value);

            var chat = await _unitOfWork.Repository<Chat>().Get(x => x.Id == chatId)
                .TagWith(nameof(SendMessage) + "_GetChat")
                .Include(x => x.Users)
                    .ThenInclude(x => x.User)
                .Include(x => x.Messages)
                    .ThenInclude(x => x.Image)
                .Include(x => x.LastMessage)
                     .ThenInclude(x => x.Image)
                .FirstOrDefaultAsync();

            if (chat == null || !chat.Users.Any(x => x.UserId == _userId.Value && x.IsActive))
                throw new CustomException(HttpStatusCode.BadRequest, "ChatId", $"Can't find chat with such id {chatId}");

            // Define message type using incoming model
            MessageType? type = null;

            if (!string.IsNullOrEmpty(model.Text))
                type = MessageType.TextMessage;
            else if (model.ImageId.HasValue)
                type = MessageType.ImageMessage;

            if (!type.HasValue)
                throw new CustomException(HttpStatusCode.BadRequest, "Message", "Invalid message type");

            var message = new Message
            {
                CreatorId = sender.Id,
                CreatedAt = DateTime.UtcNow,
                MessageType = type.Value,
                MessageStatus = MessageStatus.Sent,
                IsActive = true
            };

            switch (type.Value)
            {
                case MessageType.ImageMessage:
                    {
                        // Find image
                        var image = _unitOfWork.Repository<Image>().Find(x => x.Id == model.ImageId.Value && x.IsActive);

                        // Add attachment to message
                        if (image != null)
                            message.Image = image;
                        else
                            throw new CustomException(HttpStatusCode.BadRequest, "ImageId", $"Can't find image with given id {model.ImageId.Value}");
                        break;
                    }
                default:
                    message.Text = model.Text.Trim();
                    break;
            }

            chat.Messages.Add(message);
            chat.LastMessage = message;

            _unitOfWork.Repository<Chat>().Update(chat);
            _unitOfWork.SaveChanges();

            var response = _mapper.Map<Message, ChatMessageResponseModel>(message, opt => opt.AfterMap((src, dest) =>
            {
                dest.IsMyMessage = src.CreatorId == _userId.Value;
                dest.IsUnreadForMe = !dest.IsMyMessage && dest.Id > chat.GetUserLastReadMessageId(_userId.Value);
            }));

            // Send socket events and notifications to chat opponents
            foreach (var receiver in chat.Users)
            {
                if (receiver.UserId != _userId.Value)
                {
                    var receiverResponse = _mapper.Map<Message, ChatMessageResponseModel>(message, opt => opt.AfterMap((src, dest) =>
                    {
                        dest.IsMyMessage = src.CreatorId == receiver.UserId;
                        dest.IsUnreadForMe = !dest.IsMyMessage && dest.Id > chat.GetUserLastReadMessageId(receiver.UserId);
                        dest.UnreadMesagesInChat = chat.GetBadge(receiver.UserId);
                        dest.AllUnreadMesages = GetAllUnreadMessagesCount(receiver.UserId);
                    }));

                    await _webSocketHandler.SendMessageToUserAsync(receiver.UserId, new WebSocketEventResponseModel
                    {
                        EventType = WebSocketEventType.NewMessage,
                        Data = receiverResponse
                    });

                    string messageContent = null;
                    switch (message.MessageType)
                    {
                        case MessageType.ImageMessage:
                            messageContent = message.Image.CompactPath;
                            break;
                        case MessageType.TextMessage:
                            messageContent = "Image";
                            break;
                    }

                    await _notificationService.SendPushNotification(receiver.User, PushNotifications.NewMessage(receiver.UserId, sender.Profile.FullName, messageContent, chat.Id));
                }
            }

            return response;
        }

        public async Task<PaginationResponseModel<ChatResponseModel>> GetChatList(PaginationBaseRequestModel model)
        {
            var chats = await _unitOfWork.Repository<Chat>().Get(x => x.Users.Any(y => y.UserId == _userId.Value))
                .TagWith(nameof(GetChatList) + "_GetChats")
                .Include(x => x.Messages)
                .Include(x => x.LastMessage)
                    .ThenInclude(x => x.Image)
                .Include(x => x.Users)
                    .ThenInclude(x => x.User)
                        .ThenInclude(x => x.Profile)
                .ToListAsync();

            var count = chats.Count();

            // In case when there is no last message in chat using for order default value
            var result = chats
                .OrderByDescending(x => x.LastMessage != null ? x.LastMessage.CreatedAt : default)
                .Skip(model.Offset)
                .Take(model.Limit)
                .Select(x => new ChatResponseModel
                {
                    ChatId = x.Id,
                    ChatUsers = _mapper.Map<List<UserResponseModel>>(x.Users.Select(y => y.User.Profile)),
                    LastItem = x.LastMessage == null ? null : _mapper.Map<Message, ChatMessageResponseModel>(x.LastMessage, opt => opt.AfterMap((src, dest) =>
                    {
                        dest.IsMyMessage = src.CreatorId == _userId.Value;
                        dest.IsUnreadForMe = !dest.IsMyMessage && dest.Id > x.GetUserLastReadMessageId(_userId.Value);
                    }))
                })
                .ToList();

            return new PaginationResponseModel<ChatResponseModel>(result, count);
        }

        public async Task<PaginationResponseModel<ChatMessageBaseResponseModel>> GetMessages(int chatId, ChatMessagesListRequestModel model)
        {
            // Get and check chat.
            var chat = await _unitOfWork.Repository<Chat>().Get(x => x.Id == chatId && x.Users.Any(y => y.UserId == _userId.Value && y.IsActive))
                .TagWith(nameof(GetMessages) + "_GetChat")
                .Include(x => x.Users)
                .FirstOrDefaultAsync();

            if (chat == null)
                throw new CustomException(HttpStatusCode.BadRequest, "ChatId", $"Can't find chat with such id {chatId}");

            var userLastReadMessageid = chat.GetUserLastReadMessageId(_userId.Value);

            // Get messages
            var messages = _unitOfWork.Repository<Message>().Get(x => x.ChatId == chat.Id && (!model.StartDate.HasValue || x.CreatedAt > model.StartDate.Value))
                .TagWith(nameof(GetMessages) + "_GetChatMessages")
                .Include(x => x.Image);

            var count = messages.Count();

            var orderedMessages = model.StartDate.HasValue ? messages.Where(x => x.CreatedAt > model.StartDate.Value).OrderBy(x => x.CreatedAt) : messages.OrderByDescending(x => x.CreatedAt);

            var result = orderedMessages
                .Skip(model.Offset)
                .Take(model.Limit)
                .ToList()
                .Select(x => _mapper.Map<Message, ChatMessageBaseResponseModel>(x, opt => opt.AfterMap((src, dest) =>
                {
                    dest.IsMyMessage = src.CreatorId == _userId.Value;
                    dest.IsUnreadForMe = !dest.IsMyMessage && dest.Id > userLastReadMessageid;
                })))
                .ToList();

            return new PaginationResponseModel<ChatMessageBaseResponseModel>(result, count);
        }

        public async Task<UnreadMessagesResponseModel> ReadMessages(int chatId, ReadMessagesRequestModel model)
        {
            var chat = await _unitOfWork.Repository<Chat>().Get(x => x.Id == chatId && x.Users.Any(y => y.UserId == _userId.Value))
                .TagWith(nameof(ReadMessages) + "_GetChat")
                .Include(x => x.Users)
                .Include(x => x.Messages)
                .FirstOrDefaultAsync();

            if (chat == null)
                throw new CustomException(HttpStatusCode.BadRequest, "ChatId", $"Can't find chat with such id {chatId}");

            var messages = new List<Message>();

            if (model.MessageIds.Any() || (model.ReadTillMessageId.HasValue && model.ReadTillMessageId > 0))
            {
                var lastReadMessageId = chat.GetUserLastReadMessageId(_userId.Value);

                // Get all messages, that this user didn`t read
                messages = chat.Messages.Where(x => x.CreatorId != _userId.Value && x.ChatId == chatId && x.Id > lastReadMessageId
                    && (model.MessageIds.Any() ? model.MessageIds.Contains(x.Id) : !model.ReadTillMessageId.HasValue || x.Id <= model.ReadTillMessageId))
                    .ToList();

                if (messages.Any())
                {
                    // Set message status read if it is`t
                    var messagesToUpdate = messages.Where(x => x.MessageStatus != MessageStatus.Read).ToList();
                    messagesToUpdate.ForEach(x =>
                    {
                        x.MessageStatus = MessageStatus.Read;
                    });

                    // Update last read message
                    var newLastReadMessage = messages.Max(x => x.Id);
                    var currentUser = chat.Users.FirstOrDefault(x => x.UserId == _userId.Value);

                    currentUser.LastReadMessageId = newLastReadMessage;

                    _unitOfWork.Repository<Chat>().Update(chat);
                    _unitOfWork.SaveChanges();

                    // Send socket event only if message status has changed
                    if (messagesToUpdate.Any())
                    {
                        var notifyEntities = messagesToUpdate.Select(x => x.CreatorId)
                            .Distinct()
                            .Select(x => new
                            {
                                UserId = x,
                                ChatId = chat.Id,
                                UpdatetMessages = messages.Where(y => y.CreatorId == x).Select(y => y.Id).ToList()
                            })
                            .ToList();

                        foreach (var item in notifyEntities)
                        {
                            //send socket event to message creator
                            await _webSocketHandler.SendMessageToUserAsync(item.UserId, new WebSocketEventResponseModel
                            {
                                EventType = WebSocketEventType.MessageRead,
                                Data = new ReadMessageResponseModel
                                {
                                    ChatId = item.ChatId,
                                    Messages = item.UpdatetMessages
                                }
                            });
                        }
                    }
                }
            }

            var response = new UnreadMessagesResponseModel
            {
                UnreadMesagesInChat = chat.GetBadge(_userId.Value),
                AllUnreadMesages = GetAllUnreadMessagesCount(_userId.Value),
                Messages = messages.Select(x => x.Id).ToList(),
                ReadTillMessageId = model.ReadTillMessageId
            };

            return response;
        }

        public int GetAllUnreadMessagesCount(int userId)
        {
            var count = _unitOfWork.Repository<Chat>().Get(x => x.Users.Any(y => y.UserId == userId && y.IsActive))
                .TagWith(nameof(GetAllUnreadMessagesCount) + "_GetUnreadMessagesCount")
                .Include(x => x.Users)
                .Include(x => x.Messages)
                .ToList()
                .Sum(x => x.GetBadge(userId));

            return count;
        }

        public async Task<UserStatusResponseModel> GetUserStatus(int userId)
        {
            var user = await GetUser(userId);
            var result = new UserStatusResponseModel
            {
                UserId = user.Id,
                Status = _webSocketHandler.CheckConnectionStatus(userId)
            };

            return result;
        }

        private async Task<ApplicationUser> GetUser(int userId)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().Get(x => x.Id == userId)
                .Include(x => x.Profile)
                .FirstOrDefaultAsync();

            if (user == null || user.Profile == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", $"Can't find user with given id {userId}");

            return user;
        }
    }
}
