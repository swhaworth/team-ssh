using System;
using System.Globalization;
using System.Linq;
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
                var clientMode = Program.GetMode(ClientMode.Client, 1);
                logger.LogInformation($"Starting with mode {clientMode}.");
                switch (clientMode.Mode)
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
                            CreateOtherEnd = (c) => new WebSocketClientTunnelEnd(logger, "WS", new Uri("ws://authenticatorwebservice.azurewebsites.net/teamssh"), clientMode.Id, _cancel.Token)
                        };
                        break;
                    case ClientMode.Server:
                        serverEnd = new WebSocketServerTunnelEnd(logger, "WSServ", new Uri("ws://authenticatorwebservice.azurewebsites.net/teamssh"), clientMode.Id, _cancel.Token)
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

        private static (ClientMode Mode, int Id) GetMode(ClientMode defaultMode, int defaultId)
        {
            for (var c = 0; c < Program.Arguments.Length; ++c)
            {
                if (Enum.TryParse<ClientMode>(Program.Arguments[c], true, out var mode))
                {
                    var nextArgument = Program.Arguments.ElementAtOrDefault(c + 1);
                    if (!string.IsNullOrEmpty(nextArgument) && int.TryParse(nextArgument, NumberStyles.Integer, CultureInfo.CurrentCulture, out var id))
                    {
                        return (mode, id);
                    }
                    return (mode, defaultId);
                }
            }
            return (defaultMode, defaultId);
        }

        #endregion
    }
}