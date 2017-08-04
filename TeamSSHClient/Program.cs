using System;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Tunnelling;

namespace TeamSSHClient
{
    public static class Program
    {
        #region Fields

        private static readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        #endregion

        #region Properties

        public static string[] Arguments { get; private set; }

        #endregion

        #region Public Methods

        public static int Main(string[] args)
        {
            Program.Arguments = args;
            var loggerFactory = new LoggerFactory().AddConsole().AddDebug();
            var logger = loggerFactory.CreateLogger("TeamSSHClient");
            Console.CancelKeyPress += Program.Console_CancelKeyPress;
            try
            {
                var serverEnd = default(BaseTunnelEnd);
                var clientMode = Program.GetMode(ClientMode.Client);
                logger.LogInformation($"Starting with mode {clientMode}.");
                switch (clientMode)
                {
                    case ClientMode.Local:
                        serverEnd = new TcpServerTunnelEnd(logger, "Server", IPAddress.Any, 10022, _cancel.Token)
                        {
                            CreateOtherEnd = (c) => new TcpClientTunnelEnd(logger, "SSH", "127.0.0.1", 22, _cancel.Token)
                        };
                        break;
                    case ClientMode.Client:
                        serverEnd = new TcpServerTunnelEnd(logger, "Server", IPAddress.Any, 10022, _cancel.Token)
                        {
                            CreateOtherEnd = (c) => new WebSocketClientTunnelEnd(logger, "WS", new Uri("ws://authenticatorwebservice.azurewebsites.net/teamssh"), _cancel.Token)
                        };
                        break;
                    case ClientMode.Server:
                        serverEnd = new WebSocketServerTunnelEnd(logger, "WSServ", new Uri("ws://authenticatorwebservice.azurewebsites.net/teamssh"), _cancel.Token)
                        {
                            CreateOtherEnd = (s) => new TcpClientTunnelEnd(logger, "SSH", "127.0.0.1", 22, _cancel.Token)
                        };
                        break;
                }
                if (serverEnd == null)
                {
                    logger.LogError("Could not create tunnel end based on arguments.");
                    return -1;
                }
                serverEnd.Start().Wait();
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(0, ex, "Exception");
                return -255;
            }
        }

        #endregion

        #region Private Methods

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _cancel.Cancel();
        }

        private static ClientMode GetMode(ClientMode defaultMode)
        {
            foreach (var arg in Program.Arguments)
            {
                if (Enum.TryParse<ClientMode>(arg, true, out var mode))
                {
                    return mode;
                }
            }
            return defaultMode;
        }

        #endregion
    }
}