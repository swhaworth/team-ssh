using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Extensions;
using TeamSSHLibrary.Helpers;

namespace TeamSSHLibrary.Tunnelling
{
    public class TcpClientTunnelEnd : BaseTunnelEnd
    {
        #region Ctors

        public TcpClientTunnelEnd(ILogger logger, string name, TcpClient client, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.Client = client;
        }

        public TcpClientTunnelEnd(ILogger logger, string name, string hostname, int port, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.HostName = hostname;
            this.Port = port;
        }

        #endregion

        #region Properties

        public TcpClient Client { get; private set; }
        public string HostName { get; }
        public int Port { get; }

        #endregion

        #region Public Methods

        public override Task Start()
        {
            this.Logger?.LogInformation(this.LogAll(this.Name));
            return Task.Run(() => this.InternalStart());
        }

        #endregion

        private void InternalStart()
        {
            var readTaskEvent = default(AutoResetEvent);
            var writeTaskEvent = default(AutoResetEvent);
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(this.Cancel, _close.Token).Token;
            try
            {
                if (this.Client == null)
                {
                    using (var connectTaskEvent = new AutoResetEvent(false))
                    {
                        var connectTask = default(Task);
                        this.Client = new TcpClient();
                        while (!cancel.IsCancellationRequested)
                        {
                            if (connectTask == null)
                            {
                                connectTask = this.Client.ConnectAsync(IPAddress.Parse(this.HostName), this.Port).ContinueWithEvent(connectTaskEvent);
                            }
                            if (connectTask.IsCompleted)
                            {
                                this.Logger?.LogInformation(this.LogPrefix(this.Name) + $"Connected from {this.Client.Client.LocalEndPoint} to {this.Client.Client.RemoteEndPoint}");
                                break;
                            }
                            var waitResult = WaitHandle.WaitAny(new WaitHandle[] { cancel.WaitHandle, connectTaskEvent }, TimeSpan.FromSeconds(0.1));
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
                }
                else
                {
                    this.Logger?.LogInformation(this.LogPrefix(this.Name) + $"Already connected from {this.Client.Client.LocalEndPoint} to {this.Client.Client.RemoteEndPoint}");
                }
                var stream = this.Client.GetStream();
                var readBuffer = new byte[8192];
                var writeBuffer = default(byte[]);
                var readTask = default(Task<int>);
                var writeTask = default(Task);
                readTaskEvent = new AutoResetEvent(false);
                writeTaskEvent = new AutoResetEvent(false);
                while (!cancel.IsCancellationRequested)
                {
                    if (readTask == null)
                    {
                        readTask = stream.ReadAsync(readBuffer, 0, readBuffer.Length).ContinueWithEvent(readTaskEvent);
                    }
                    if (readTask.IsCompleted)
                    {
                        var read = readTask.Result;
                        if (read == 0)
                        {
                            this.Logger?.LogInformation(this.LogPrefix(this.Name) + $"Got 0 length read, quitting.");
                            return;
                        }
                        this.Outgoing.AddRange(readBuffer.Take(read));
                        readTask = null;
                    }
                    if (writeTask == null)
                    {
                        var writeData = this.Incoming.Take(1024);
                        if (writeData.Any())
                        {
                            writeBuffer = writeData.ToArray();
                            writeTask = stream.WriteAsync(writeBuffer, 0, writeBuffer.Length).ContinueWith((t) => ExceptionHelpers.WrapObjectDisposedException(() => writeTaskEvent.Set()));
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
            catch (Exception ex)
            {
                this.Logger?.LogError(0, ex, this.LogPrefix(this.Name) + "Got Exception");
            }
            finally
            {
                this.OtherEnd?.Close();
                this.Client?.Dispose();
                readTaskEvent?.Dispose();
                writeTaskEvent?.Dispose();
            }
        }
    }
}
