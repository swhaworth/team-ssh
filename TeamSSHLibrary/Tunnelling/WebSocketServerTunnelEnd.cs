using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Extensions;

namespace TeamSSHLibrary.Tunnelling
{
    public class WebSocketServerTunnelEnd : BaseTunnelEnd
    {
        #region Ctors

        public WebSocketServerTunnelEnd(ILogger logger, string name, Uri uri, int id, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.Id = id;
            this.Uri = uri;
        }

        #endregion

        #region Properties

        public Func<WebSocket, BaseTunnelEnd> CreateOtherEnd { get; set; }
        public int Id { get; }
        public Uri Uri { get; }

        #endregion        

        #region Public Methods

        public override Task Start()
        {
            this.Logger.LogInformation(this.LogAll(this.Name));
            return Task.Run(() => this.InternalStart());
        }

        #endregion

        #region Private Methods

        private void InternalStart()
        {
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(this.Cancel, _close.Token).Token;
            while (!cancel.IsCancellationRequested)
            {
                var socket = new ClientWebSocket();
                try
                {
                    if (!socket.Connect(this.Uri, cancel, this.Logger, this.LogPrefix(this.Name)))
                    {
                        cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                        continue;
                    }
                    if (!socket.SendJson(new { Id = this.Id, Type = "Server" }, cancel, this.Logger, this.LogPrefix(this.Name)))
                    {
                        cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                        continue;
                    }
                    var connectionObject = socket.ReceiveJson(cancel, this.Logger, this.LogPrefix(this.Name));
                    if (connectionObject == null)
                    {
                        cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                        continue;
                    }
                    var thisEnd = new WebSocketClientTunnelEnd(this.Logger, this.Name, socket, this.Cancel);
                    var otherEnd = this.CreateOtherEnd(socket);
                    if (otherEnd != null)
                    {
                        thisEnd.Connect(otherEnd);
                        thisEnd.Start();
                        otherEnd.Start();
                        socket = null;
                    }
                }
                finally
                {
                    socket?.Dispose();
                }
            }
        }

        #endregion
    }
}
