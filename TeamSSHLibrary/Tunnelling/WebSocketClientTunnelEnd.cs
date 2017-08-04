using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Extensions;

namespace TeamSSHLibrary.Tunnelling
{
    public class WebSocketClientTunnelEnd : BaseTunnelEnd
    {
        #region Ctors

        public WebSocketClientTunnelEnd(ILogger logger, string name, WebSocket socket, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.Socket = socket;
        }

        public WebSocketClientTunnelEnd(ILogger logger, string name, Uri uri, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.Uri = uri;
        }

        #endregion

        #region Properties

        public WebSocket Socket { get; private set; }
        public Uri Uri { get; }

        #endregion        

        #region Public Methods

        public override Task Start()
        {
            this.Logger?.LogInformation(this.LogAll(this.Name));
            return Task.Run(() => this.InternalStart());
        }

        #endregion

        #region Private Methods

        private void InternalStart()
        {
            var readTaskEvent = default(AutoResetEvent);
            var writeTaskEvent = default(AutoResetEvent);
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(this.Cancel, _close.Token).Token;
            try
            {
                if (this.Socket == null)
                {
                    this.Socket = new ClientWebSocket();
                    if (!((ClientWebSocket)this.Socket).Connect(this.Uri, cancel, this.Logger, this.LogPrefix(this.Name)))
                    {
                        return;
                    }
                    if (!this.Socket.SendJson(new { Id = 1, Type = "Client" }, cancel, this.Logger, this.LogPrefix(this.Name)))
                    {
                        return;
                    }
                }
                else
                {
                    this.Logger?.LogInformation(this.LogPrefix(this.Name) + $"Already connected {this.Socket.CloseStatus} {this.Socket.State} {this.Socket.SubProtocol}");
                }
                var readTask = default(Task<WebSocketReceiveResult>);
                var readBuffer = new byte[8192];
                var currentMessage = new MemoryStream();
                var endOfCurrentMessage = false;
                var writeTask = default(Task);
                var writeBuffer = default(byte[]);
                readTaskEvent = new AutoResetEvent(false);
                writeTaskEvent = new AutoResetEvent(false);
                while (!cancel.IsCancellationRequested)
                {
                    if (readTask == null)
                    {
                        readTask = this.Socket.ReceiveAsync(new ArraySegment<byte>(readBuffer), cancel).ContinueWithEvent(readTaskEvent);
                    }
                    if (readTask.IsCompleted)
                    {
                        if (readTask.IsFaulted)
                        {
                            this.Logger?.LogError(0, readTask.Exception, this.LogPrefix(this.Name) + "Exception receiving from WebSocket");
                            return;
                        }
                        currentMessage.Write(readBuffer, 0, readTask.Result.Count);
                        endOfCurrentMessage = readTask.Result.EndOfMessage;
                        readTask = null;
                    }
                    if (endOfCurrentMessage)
                    {
                        this.Outgoing.AddRange(currentMessage.ToArray());
                        currentMessage.SetLength(0);
                        endOfCurrentMessage = false;
                    }
                    if (writeTask == null)
                    {
                        var writeData = this.Incoming.Take(1024);
                        if (writeData.Any())
                        {
                            writeBuffer = writeData.ToArray();
                            writeTask = this.Socket.SendAsync(new ArraySegment<byte>(writeBuffer), WebSocketMessageType.Binary, true, cancel).ContinueWithEvent(writeTaskEvent);
                        }
                    }
                    if ((writeTask != null) && writeTask.IsCompleted)
                    {
                        writeTask = null;
                    }
                    var waitResult = WaitHandle.WaitAny(new WaitHandle[] { cancel.WaitHandle, readTaskEvent, writeTaskEvent, this.Incoming.Added }, TimeSpan.FromSeconds(0.1));
                    if (waitResult == 0)
                    {
                        if (_close.IsCancellationRequested)
                        {
                            this.Logger?.LogInformation(this.LogPrefix(this.Name) + "Got close event.");
                        }
                        else
                        {
                            this.Logger?.LogInformation(this.LogPrefix(this.Name) + "Got cancel event.");
                        }
                        return;
                    }
                }
            }
            finally
            {
                readTaskEvent?.Dispose();
                writeTaskEvent?.Dispose();
            }
        }

        #endregion
    }
}
