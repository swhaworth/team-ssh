using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Extensions;
using TeamSSHLibrary.Tunnelling;

namespace TeamSSHWebService.Middleware
{
    public class SshConnectionMiddleware
    {
        #region Types

        private sealed class SocketInfo
        {
            public SocketInfo(WebSocket socket, int id)
            {
                this.Connected = false;
                this.Id = id;
                this.Socket = socket;
            }

            public bool Connected { get; set; }
            public int Id { get; }
            public WebSocket Socket { get; }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly IList<SocketInfo> _sockets = new List<SocketInfo>();
        private readonly object _socketsLock = new object();

        #endregion

        #region Ctors

        public SshConnectionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(SshConnectionMiddleware));
            _next = next;
        }

        #endregion

        #region Public Methods

        public Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return _next.Invoke(context);
            }
            return Task.Run(() => this.InvokeStart(context));
        }

        #endregion

        #region Private Methods

        private void InvokeStart(HttpContext context)
        {
            var cancel = context.RequestAborted;
            using (var socket = context.WebSockets.AcceptWebSocketAsync().Result)
            {
                _logger.LogInformation(this.LogPrefix() + "Got WS Connection");
                var connectionObject = socket.ReceiveJson(cancel);
                var type = connectionObject["Type"].ToString();
                var id = connectionObject["Id"].ToObject<int>();
                _logger.LogInformation(this.LogPrefix() + $"Got info Type={type}, Id={id}");
                if (StringComparer.OrdinalIgnoreCase.Equals(type, "server"))
                {
                    this.InvokeServer(socket, id, cancel);
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(type, "client"))
                {
                    this.InvokeClient(socket, id, cancel);
                }
            }
        }

        private void InvokeClient(WebSocket socket, int id, CancellationToken cancel)
        {
            var serverSocket = default(SocketInfo);
            lock (_socketsLock)
            {
                serverSocket = _sockets.FirstOrDefault((s) => (s.Id == id) && !s.Connected);
                if (serverSocket != null)
                {
                    serverSocket.Connected = true;
                }
            }
            if (serverSocket == null)
            {
                _logger.LogInformation(this.LogPrefix() + $"Failed to find server for {id}.");
                return;
            }
            _logger.LogInformation(this.LogPrefix() + $"Sending connection start for {id}.");
            serverSocket.Socket.SendJson(new { Id = id }, cancel, _logger, this.LogPrefix());
            _logger.LogInformation(this.LogPrefix() + $"Start tunnelling between websockets for {id}.");
            var clientEnd = new WebSocketClientTunnelEnd(_logger, "WSClient", socket, cancel);
            var serverEnd = new WebSocketClientTunnelEnd(_logger, "WSServer", serverSocket.Socket, cancel);
            clientEnd.Connect(serverEnd);
            Task.WaitAll(clientEnd.Start(), serverEnd.Start());
            lock (_socketsLock)
            {
                serverSocket.Connected = false;
            }
            _logger.LogInformation(this.LogPrefix() + $"Done tunnelling between websockets for {id}");
        }

        private void InvokeServer(WebSocket socket, int id, CancellationToken cancel)
        {
            var socketInfo = new SocketInfo(socket, id);
            lock (_socketsLock)
            {
                _sockets.Add(socketInfo);
            }
            _logger.LogInformation(this.LogPrefix() + $"Starting server for {id}");
            while (!cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(0.1)) && (socket.State == WebSocketState.Open))
            {
            }
            _logger.LogInformation(this.LogPrefix() + $"Done server for {id}");
            lock (_socketsLock)
            {
                _sockets.Remove(socketInfo);
            }
        }

        #endregion
    }
}
