using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSSHLibrary.Tunnelling;

namespace TeamSSHClient
{
    public static class Program
    {
        #region Fields

        private static readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        #endregion

        #region Public Methods

        public static int Main(string[] args)
        {
            var arguments = new ArgumentHandler(args);
            var configuration = ConfigurationFile.Load(arguments);
            var loggerFactory = (ILoggerFactory)new LoggerFactory();
            if (configuration.AddConsoleLogger)
            {
                loggerFactory = loggerFactory.AddConsole();
            }
            if (configuration.AddDebugLogger)
            {
                loggerFactory = loggerFactory.AddDebug();
            }
            var logger = loggerFactory.CreateLogger(configuration.LoggerCategoryName);
            Console.CancelKeyPress += Program.Console_CancelKeyPress;
            try
            {
                var serverEnds = new List<BaseTunnelEnd>();
                var executionMode = arguments.ExecutionMode;
                logger.LogInformation($"Starting with mode {executionMode}.");
                switch (executionMode)
                {
                    case ClientMode.Local:
                        serverEnds.Add(new TcpServerTunnelEnd(logger, "Server", IPAddress.Any, configuration.ServerPort, _cancel.Token)
                        {
                            CreateOtherEnd = (c) => new TcpClientTunnelEnd(logger, "SSH", configuration.LocalUri, configuration.LocalPort, _cancel.Token)
                        });
                        break;
                    case ClientMode.Client:
                        serverEnds.AddRange(configuration.Clients.Select((c) => new TcpServerTunnelEnd(logger, "Server", IPAddress.Any, c.LocalPort, _cancel.Token)
                        {
                            CreateOtherEnd = (_) => new WebSocketClientTunnelEnd(logger, "WS", new Uri(c.ServerUri), c.ConnectionId, _cancel.Token)
                        }));
                        break;
                    case ClientMode.Server:
                        serverEnds.AddRange(configuration.Servers.Select((s) => new WebSocketServerTunnelEnd(logger, "WSServ", new Uri(s.ServerUri), s.ConnectionId, _cancel.Token)
                        {
                            CreateOtherEnd = (_) => new TcpClientTunnelEnd(logger, "SSH", s.LocalUri, s.LocalPort, _cancel.Token)
                        }));
                        break;
                    case ClientMode.Add:
                        configuration.AddClient(new ConfigurationItem
                        {
                            ConnectionId = arguments.GetIntArgument("--id", 1), LocalPort = arguments.GetIntArgument("--lport", 10022),
                            ServerUri = arguments.GetStringArgument("--suri")
                        });
                        return 0;
                    case ClientMode.Register:
                        configuration.AddServer(new ConfigurationItem
                        {
                            ConnectionId = arguments.GetIntArgument("--id", 1), LocalPort = arguments.GetIntArgument("--lport", 22),
                            LocalUri = arguments.GetStringArgument("--luri", IPAddress.Loopback.ToString()), ServerUri = arguments.GetStringArgument("--suri")
                        });
                        return 0;
                    case ClientMode.Remove:
                        configuration.RemoveClient(arguments.GetIntArgument("--id", 1));
                        return 0;
                    case ClientMode.Unregister:
                        configuration.RemoveServer(arguments.GetIntArgument("--id", 1));
                        return 0;
                    case ClientMode.Help:
                        return 0;
                }
                if (!serverEnds.Any())
                {
                    logger.LogError("Could not create tunnel end based on arguments.");
                    return -1;
                }
                Task.WaitAll(serverEnds.Select((e) => e.Start()).ToArray());
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

        #endregion
    }
}