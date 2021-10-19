using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Chat;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.ResponseModels.Chat;
using VideoOnDemand.WebSockets.Abstract;
using VideoOnDemand.WebSockets.Constants;
using VideoOnDemand.WebSockets.Interfaces;
using VideoOnDemand.WebSockets.Managers;
using VideoOnDemand.WebSockets.Models;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace VideoOnDemand.WebSockets.Handlers
{
    public class WebSocketMessageHandler : WebSocketHandler
    {
        private IConfiguration _configuration;
        private IUnitOfWork _unitOfWork;
        private ILogger<WebSocketMessageHandler> _log = null;

        public WebSocketMessageHandler(WebSocketConnectionManager<IWSItem> webSocketConnectionManager, IUnitOfWork unitOfWork, ILogger<WebSocketMessageHandler> log, IConfiguration configuration)
            : base(webSocketConnectionManager, log)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _log = log;
        }

        public async Task SendMessageToUserAsync(int userId, WebSocketEventResponseModel message)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);

            var socketsData = WebSocketConnectionManager.Get(w => w.Data.UserId == userId);

            if (socketsData.Count > 0)
            {
                foreach (var socketData in socketsData)
                {
                    if (socketData.Socket.State != WebSocketState.Open)
                    {
                        _log.LogInformation($"WebSocket => socket {socketData.Data.TokenId} is " + Enum.GetName(typeof(WebSocketState), socketData.Socket.State));
                        return;
                    }

                    await SendMessageAsync(socketData.Socket, serializedMessage);
                }
            }
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = WebSocketConnectionManager.GetTokenId(socket);

            if (socketId != null)
            {
                var socketData = WebSocketConnectionManager.GetSocketDataByTokenId(socketId.Value);

                var json = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                var requestMessage = JsonConvert.DeserializeObject<WebSocketEventResponseModel>(json);

                WebSocketEventResponseModel webSocketResponseMessage = null;
                string responseMessage = null;

                IGroupsItem<int> groupItem = null;

                switch (requestMessage?.EventType?.ToLower())
                {
                    case WebSocketEventType.Ping:

                        webSocketResponseMessage = new WebSocketEventResponseModel
                        {
                            EventType = WebSocketEventType.Pong
                        };

                        responseMessage = JsonConvert.SerializeObject(webSocketResponseMessage);

                        await SendMessageAsync(socket, responseMessage);
                        break;
                    case WebSocketEventType.Typing:

                        webSocketResponseMessage = new WebSocketEventResponseModel
                        {
                            EventType = WebSocketEventType.Typing
                        };

                        responseMessage = JsonConvert.SerializeObject(webSocketResponseMessage);

                        groupItem = socketData as IGroupsItem<int>;

                        if (groupItem != null)
                        {
                            var opponents = WebSocketConnectionManager.Get(w => w.Data.UserId != socketData.UserId && (w.Data as IGroupsItem<int>) != null && (w.Data as IGroupsItem<int>).GroupIds.Any(x => groupItem.GroupIds.Contains(x)));

                            foreach (var opponent in opponents)
                            {
                                await SendMessageAsync(opponent.Socket, responseMessage);
                            }
                        }

                        break;
                    case WebSocketEventType.Message:

                        groupItem = socketData as IGroupsItem<int>;

                        if (groupItem != null)
                        {
                            var opponents = WebSocketConnectionManager.Get(w => w.Data.UserId != socketData.UserId && (w.Data as IGroupsItem<int>) != null && (w.Data as IGroupsItem<int>).GroupIds.Any(x => groupItem.GroupIds.Contains(x)));

                            foreach (var opponent in opponents)
                            {
                                await SendMessageAsync(opponent.Socket, json);
                            }
                        }

                        break;
                }
            }
        }

        public async Task RemoveWebSocketConnectionByTokenId(int tokenId)
        {
            var webSocket = WebSocketConnectionManager.GetSocketByTokenId(tokenId);
            if (webSocket != null)
            {
                try
                {
                    _log.LogInformation("ChatMessageHandler.RemoveWebSocketConnection -> Close connection for WebSocket with id " + tokenId);
                    await WebSocketConnectionManager.RemoveSocketAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Invalid connection");
                }
                catch (Exception ex)
                {
                    _log.LogError("ChatMessageHandler.RemoveWebSocketConnection -> Exception occured during connection closing. Connection id: " + tokenId + ". Exception message: " + ex.Message);
                    WebSocketConnectionManager.RemoveSocket(webSocket);
                }
            }
        }

        public async Task RemoveWebSocketConnectionByUserId(int userId)
        {
            var webSocket = WebSocketConnectionManager.Get(x => x.Data.UserId == userId).FirstOrDefault();

            if (webSocket != null)
            {
                try
                {
                    _log.LogInformation("ChatMessageHandler.RemoveWebSocketConnection -> Close connection for WebSocket with id " + webSocket.Data.TokenId);
                    await WebSocketConnectionManager.RemoveSocketAsync(webSocket.Socket, WebSocketCloseStatus.NormalClosure, "User logout");
                }
                catch (Exception ex)
                {
                    _log.LogError("ChatMessageHandler.RemoveWebSocketConnection -> Exception occured during connection closing. Connection id: " + webSocket.Data.TokenId + ". Exception message: " + ex.Message);
                    WebSocketConnectionManager.RemoveSocket(webSocket.Socket);
                }

                await SendStatus(userId, UserConnectionStatus.Offline);
            }
        }

        public UserConnectionStatus CheckConnectionStatus(int UserId)
        {

            return WebSocketConnectionManager.Get(x => x.Data.UserId == UserId).Any() ? UserConnectionStatus.Online : UserConnectionStatus.Offline;
        }

        public override async Task<IWSItem> Connect(WebSocket socket, IWSItem data)
        {
            var result = await WebSocketConnectionManager.AddSocket(socket, data);

            // Send status after user connection
            await SendStatus(data.UserId, UserConnectionStatus.Online);

            return result;
        }

        public async Task<IWSItem> ConnectTest(WebSocket socket, IWSItem data)
        {
            var result = await WebSocketConnectionManager.AddTestSocket(socket, data);

            await SendMessageAsync(socket, JsonConvert.SerializeObject(new WebSocketEventResponseModel
            {
                EventType = WebSocketEventType.OnlineStatusChanged,
                Data = new UserStatusResponseModel
                {
                    UserId = data.UserId,
                    Status = UserConnectionStatus.Online
                }
            }));

            return result;
        }

        public override async Task Disconnect(WebSocket socket)
        {
            // Get socket
            var socketData = WebSocketConnectionManager.Get(x => x.Socket == socket)
                .FirstOrDefault();

            if (socketData != null)
            {
                try
                {
                    _log.LogInformation("WebSocketHandler.OnDisconnected -> WebSocket with id {" + socketData.Data.TokenId + "} disconected");
                    await WebSocketConnectionManager.RemoveSocketAsync(socket, WebSocketCloseStatus.NormalClosure, "Connection closed");
                }
                catch (Exception ex)
                {
                    _log.LogError("WebSocketHandler.OnDisconnected -> Exception occured during connection closing. Connection id: " + socketData.Data.TokenId + ". Exception message: " + ex.Message);
                    WebSocketConnectionManager.RemoveSocket(socket);
                }

                // Send status after user disconnection
                await SendStatus(socketData.Data.UserId, UserConnectionStatus.Offline);
            }
        }

        private async Task SendStatus(int userId, UserConnectionStatus status)
        {
            // Get opponents from this user chats to 
            var receiversList = _unitOfWork.Repository<Chat>().Get(x => x.Users.Any(y => y.UserId == userId))
                .TagWith(nameof(SendStatus) + "_GetReceiversList")
                .SelectMany(x => x.Users
                    .Where(y => y.UserId != userId && y.IsActive)
                .Select(y => y.UserId))
                .Distinct()
                .ToList();

            var socketsData = WebSocketConnectionManager
                .Get(x => x.Socket.State == WebSocketState.Open && x.Data.UserId != userId && receiversList.Contains(x.Data.UserId))
                .ToList();

            // Create message
            var message = new WebSocketEventResponseModel
            {
                EventType = WebSocketEventType.OnlineStatusChanged,
                Data = new UserStatusResponseModel
                {
                    UserId = userId,
                    Status = status
                }
            };

            var serializedMessage = JsonConvert.SerializeObject(message);

            foreach (var socketData in socketsData)
            {
                await SendMessageAsync(socketData.Socket, serializedMessage);
            }
        }

        public void AddToGroup(int userId, int groupId)
        {
            var connection = WebSocketConnectionManager.Get(w => w.Data.UserId == userId).FirstOrDefault();

            if (connection != null)
            {
                var groupsItem = connection.Data as IGroupsItem<int>;

                if (groupsItem != null && !groupsItem.GroupIds.Any(w => w == groupId))
                    ((IGroupsItem<int>)connection.Data).GroupIds.Append(groupId);
            }
        }

        public void RemoveFromGroup(int userId, int groupId)
        {
            var connection = WebSocketConnectionManager.Get(w => w.Data.UserId == userId).FirstOrDefault();

            if (connection != null)
            {
                var groupsItem = connection.Data as IGroupsItem<int>;

                if (groupsItem != null && groupsItem.GroupIds.Any(w => w == groupId))
                    ((IGroupsItem<int>)connection.Data).GroupIds = ((IGroupsItem<int>)connection.Data).GroupIds.Where(w => w != groupId).ToArray();
            }
        }
    }
}
