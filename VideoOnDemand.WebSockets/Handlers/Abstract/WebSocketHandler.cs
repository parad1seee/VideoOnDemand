using Microsoft.Extensions.Logging;
using VideoOnDemand.WebSockets.Interfaces;
using VideoOnDemand.WebSockets.Managers;
using VideoOnDemand.WebSockets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.WebSockets.Abstract
{
    /// <summary>
    /// Handle connections and messages sending/receiving
    /// </summary>
    public abstract class WebSocketHandler
    {
        protected WebSocketConnectionManager<IWSItem> WebSocketConnectionManager { get; set; }
        private ILogger<WebSocketHandler> _log = null;

        public WebSocketHandler(WebSocketConnectionManager<IWSItem> manager, ILogger<WebSocketHandler> log)
        {
            WebSocketConnectionManager = manager;
            _log = log;
        }

        /// <summary>
        /// If new socket connected to logs
        /// </summary>
        /// <param name="userId">User id</param>
        /// <param name="socket">Connected socket</param>
        /// <returns>Task</returns>
        public virtual async Task<IWSItem> Connect(WebSocket socket, IWSItem data)
        {
            return await WebSocketConnectionManager.AddSocket(socket, data);
        }

        /// <summary>
        /// If socket disconnected
        /// </summary>
        /// <param name="socket">Disconnected socket</param>
        /// <returns>Task</returns>
        public virtual async Task Disconnect(WebSocket socket)
        {
            int? tokenId = WebSocketConnectionManager.GetTokenId(socket);
            if (tokenId != null)
            {
                try
                {
                    _log.LogInformation("WebSocketHandler.OnDisconnected -> WebSocket with id {" + tokenId + "} disconected");
                    await WebSocketConnectionManager.RemoveSocketAsync(socket, WebSocketCloseStatus.NormalClosure, "Connection closed");
                }
                catch (Exception ex)
                {
                    _log.LogError("WebSocketHandler.OnDisconnected -> Exception occured during connection closing. Connection id: " + tokenId + ". Exception message: " + ex.Message);
                    WebSocketConnectionManager.RemoveSocket(socket);
                }
            }
        }

        /// <summary>
        /// send message into specific socket
        /// </summary>
        /// <param name="socket">Receiver</param>
        /// <param name="message">Data to send</param>
        /// <returns>Task</returns>
        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
            {
                WebSocketConnectionManager.RemoveSocket(socket);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(array: bytes,
                                                          offset: 0,
                                                          count: bytes.Length),
                                   WebSocketMessageType.Text,
                                   true,
                                   CancellationToken.None);
        }

        /// <summary>
        /// send message into specific socket
        /// </summary>
        /// <param name="socket">Receiver</param>
        /// <param name="message">Data to send</param>
        /// <returns>Task</returns>
        public async Task SendMessageAsync(WebSocket socket, byte[] bytes)
        {
            if (socket.State != WebSocketState.Open)
            {
                WebSocketConnectionManager.RemoveSocket(socket);
                return;
            }

            await socket.SendAsync(new ArraySegment<byte>(array: bytes,
                                                          offset: 0,
                                                          count: bytes.Length),
                                   WebSocketMessageType.Binary,
                                   true,
                                   CancellationToken.None);
        }

        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="socket">Receiver</param>
        /// <param name="result">Receiving result</param>
        /// <param name="buffer">Buffer with message</param>
        /// <param name="chatId">Specific chat id</param>
        /// <returns>Task</returns>
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);

        public int? GetTokenId(WebSocket socket)
        {
            return WebSocketConnectionManager.GetTokenId(socket);
        }

        public async Task RemoveWebSocketConnection(WebSocket webSocket)
        {
            try
            {
                _log.LogInformation("WebSocketHandler.RemoveWebSocketConnection -> Close connection for WebSocket");
                await WebSocketConnectionManager.RemoveSocketAsync(webSocket, WebSocketCloseStatus.NormalClosure, "Invalid connection");
            }
            catch (Exception ex)
            {
                _log.LogError("WebSocketHandler.RemoveWebSocketConnection -> Exception occured during connection closing. Exception message: " + ex.Message);
                WebSocketConnectionManager.RemoveSocket(webSocket);
            }
        }
    }
}