using Microsoft.Extensions.Logging;
using VideoOnDemand.Domain;
using VideoOnDemand.WebSockets.Interfaces;
using VideoOnDemand.WebSockets.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.WebSockets.Managers
{
    public class WebSocketConnectionManager<ConnectedData> where ConnectedData : class, IWSItem
    {
        public HashSet<WSItem<ConnectedData>> Sockets { get; set; }
        private ILogger<WebSocketConnectionManager<ConnectedData>> _log = null;

        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager<ConnectedData>> log)
        {
            Sockets = new HashSet<WSItem<ConnectedData>>();
            _log = log;
        }

        public List<WSItem<ConnectedData>> Get(Func<WSItem<ConnectedData>, bool> predicate)
        {
            return Sockets.Where(predicate).ToList();
        }

        public WebSocket GetSocketByTokenId(int tokenId)
        {
            return Sockets.FirstOrDefault(p => p.Data.TokenId == tokenId)?.Socket;
        }

        public int? GetTokenId(WebSocket socket)
        {
            return Sockets.FirstOrDefault(p => p.Socket == socket)?.Data.TokenId;
        }

        public ConnectedData GetSocketDataByTokenId(int tokenId)
        {
            return Sockets.FirstOrDefault(p => p.Data.TokenId == tokenId)?.Data;
        }

        public async Task<ConnectedData> AddSocket(WebSocket socket, ConnectedData data)
        {
            var currentSocket = this.GetSocketByTokenId(data.TokenId);

            if (currentSocket != null)
            {
                try
                {
                    string desc = "WebSocket connection with id" + data.TokenId + " was closed because this user opened new connection";
                    await RemoveSocketAsync(currentSocket, WebSocketCloseStatus.NormalClosure, desc);
                    _log.LogInformation("WebSocketConnectionManager.AddSocket -> " + desc);
                }
                catch (ArgumentException ex)
                {
                    //Ignore error from RemoveSocket
                }
                catch (Exception ex)
                {
                    _log.LogError("WebSocketConnectionManager.AddSocket -> Exception occured during connection closing. Connection id: " + data.TokenId + ". Exception message: " + ex.Message);
                    Sockets.RemoveWhere(w => w.Socket == socket);
                }
            }

            _log.LogInformation("Open connection for WebSocket with id " + data.TokenId);

            var userConnections = this.Get(w => w.Data.UserId == data.UserId);

            if (userConnections.Count > 0)
            {
                userConnections.ForEach(w => w.Data = data);
            }

            Sockets.Add(new WSItem<ConnectedData> { Socket = socket, Data = data });

            return data;
        }

        public async Task<ConnectedData> AddTestSocket(WebSocket socket, ConnectedData data)
        {
            data.TokenId = Sockets.Count > 0 ? Sockets.Min(w => w.Data.TokenId) - 1 : -1;
            data.UserId = Sockets.Count > 0 ? Sockets.Min(w => w.Data.UserId) - 1: -1;

            return await AddSocket(socket, data);
        }

        public async Task RemoveSocketAsync(WebSocket socket, WebSocketCloseStatus status, string description)
        {         
            if (socket != null)
            {
                var tokenId = this.GetTokenId(socket);

                if (tokenId == null)
                    return;

                if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.CloseSent && socket.State != WebSocketState.CloseReceived)
                {
                    try
                    {
                        await socket.CloseAsync(closeStatus: status,
                                                statusDescription: description,
                                                cancellationToken: CancellationToken.None);
                    }
                    catch (WebSocketException ex)
                    {
                        _log.LogError("WebSocketConnectionManager.RemoveSocket -> Exception occured during connection closing. Connection id: " + tokenId + ". Exception message: " + ex.Message + ". WebSocket state - " + Enum.GetName(typeof(WebSocketState), socket.State));

                        Sockets.Where(w => w.Socket == socket);

                        if (this.GetSocketDataByTokenId((int)tokenId) == null)
                        {
                            _log.LogInformation("WebSocketConnectionManager.RemoveSocket -> All connections with id {" + tokenId + "} have been closed.");
                        }

                        throw new ArgumentException("WebSocketConnectionManager.RemoveSocket -> Exception occured during connection closing. Connection id: " + tokenId + ". Exception message: " + ex.Message);
                    }
                }

                Sockets.RemoveWhere(w => w.Socket == socket);

                if (this.GetSocketDataByTokenId((int)tokenId) == null)
                {
                    _log.LogInformation("WebSocketConnectionManager.RemoveSocket -> All connections with id {" + tokenId + "} have been closed.");
                }
            }
        }

        // remove socket
        public void RemoveSocket(WebSocket socket)
        {
            Sockets.RemoveWhere(w => w.Socket == socket);
        }
    }
}
