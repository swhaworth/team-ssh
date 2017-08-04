using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Extensions;

namespace TeamSSHLibrary.Tunnelling
{
    public class TcpServerTunnelEnd : BaseTunnelEnd
    {
        #region Ctors

        public TcpServerTunnelEnd(ILogger logger, string name, IPAddress ipAddress, int port, CancellationToken cancel) : base(logger, name, cancel)
        {
            this.IPAddress = ipAddress;
            this.Port = port;
        }

        #endregion

        #region Properties

        public Func<TcpClient, BaseTunnelEnd> CreateOtherEnd { get; set; }
        public IPAddress IPAddress { get; }
        public int Port { get; }

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
            var listener = new TcpListener(this.IPAddress, this.Port);
            listener.Start();
            var connectTask = listener.AcceptTcpClientAsync();
            while (!this.Cancel.IsCancellationRequested)
            {
                if (connectTask.IsCompleted)
                {
                    var client = connectTask.Result;
                    var thisEnd = new TcpClientTunnelEnd(this.Logger, this.Name, client, this.Cancel);
                    var otherEnd = this.CreateOtherEnd(client);
                    if (otherEnd == null)
                    {
                        client.Dispose();
                    }
                    else
                    {
                        thisEnd.Connect(otherEnd);
                        thisEnd.Start();
                        otherEnd.Start();
                    }
                    connectTask = listener.AcceptTcpClientAsync();
                }
                this.Cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(0.1));
            }
        }

        #endregion
    }
}
