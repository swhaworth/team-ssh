using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamSSHLibrary.Helpers;

namespace TeamSSHLibrary.Extensions
{
    public static class WebSocketExt
    {
        #region Public Methods

        public static bool Connect(this ClientWebSocket socket, Uri uri, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            using (var connectTaskEvent = new AutoResetEvent(false))
            {
                var connectTask = socket.ConnectAsync(uri, cancel).ContinueWithEvent(connectTaskEvent);
                while (!cancel.IsCancellationRequested)
                {
                    if(connectTask.IsFaulted)
                    {
                        logger?.LogError(0, connectTask.Exception, logPrefix + "Exception Connecting WebSocket");
                        return false;
                    }
                    else if (connectTask.IsCompleted)
                    {
                        logger?.LogInformation(logPrefix + $"Connected websocket {socket.CloseStatusDescription} {socket.State} {socket.SubProtocol}.");
                        return (socket.State == WebSocketState.Open);
                    }
                    var waitResult = WaitHandle.WaitAny(new WaitHandle[] { cancel.WaitHandle, connectTaskEvent }, TimeSpan.FromSeconds(0.1));
                    if (waitResult == 0)
                    {
                        logger?.LogInformation(logPrefix + "Got cancel event.");
                        return false;
                    }
                }
            }
            return false;
        }

        public static JObject ReceiveJson(this WebSocket socket, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            var value = socket.ReceiveString(cancel, logger, logPrefix);
            if (value == null)
            {
                return null;
            }
            return JObject.Parse(value);
        }

        public static string ReceiveString(this WebSocket socket, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            var value = new StringBuilder();
            while (!cancel.IsCancellationRequested)
            {
                var part = socket.ReceiveStringPart(cancel, logger, logPrefix);
                if (part.Value == null)
                {
                    return null;
                }
                value.Append(part.Value);
                if (part.EndOfMessage)
                {
                    break;
                }
            }
            return value.ToString();
        }

        public static (string Value, bool EndOfMessage) ReceiveStringPart(this WebSocket socket, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            using (var receiveTaskEvent = new AutoResetEvent(false))
            {
                var segment = new ArraySegment<byte>(new byte[8192]);
                var receiveTask = socket.ReceiveAsync(segment, cancel).ContinueWithEvent(receiveTaskEvent);
                while (!cancel.IsCancellationRequested)
                {
                    if (receiveTask.IsCompleted)
                    {
                        logger?.LogInformation(logPrefix + $"Received {receiveTask.Result.Count} bytes.");
                        return (Encoding.UTF8.GetString(segment.Array, 0, receiveTask.Result.Count), receiveTask.Result.EndOfMessage);
                    }
                    var waitResult = WaitHandle.WaitAny(new WaitHandle[] { cancel.WaitHandle, receiveTaskEvent }, TimeSpan.FromSeconds(0.1));
                    if (waitResult == 0)
                    {
                        logger?.LogInformation(logPrefix + "Got cancel event.");
                        return (null, true);
                    }
                }
            }
            return (null, true);
        }

        public static bool SendJson(this WebSocket socket, object jsonObject, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            return socket.SendString(JsonConvert.SerializeObject(jsonObject), cancel, logger, logPrefix);
        }

        public static bool SendString(this WebSocket socket, string value, CancellationToken cancel, ILogger logger = null, string logPrefix = "")
        {
            using (var sendTaskEvent = new AutoResetEvent(false))
            {
                var segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(value));
                var sendTask = socket.SendAsync(segment, WebSocketMessageType.Text, true, cancel).ContinueWith((t) => ExceptionHelpers.WrapObjectDisposedException(() => sendTaskEvent.Set()));
                while (!cancel.IsCancellationRequested)
                {
                    if (sendTask.IsCompleted)
                    {
                        logger?.LogInformation(logPrefix + $"Sent {segment.Count} text bytes.");
                        return true;
                    }
                    var waitResult = WaitHandle.WaitAny(new WaitHandle[] { cancel.WaitHandle, sendTaskEvent }, TimeSpan.FromSeconds(0.1));
                    if (waitResult == 0)
                    {
                        logger?.LogInformation(logPrefix + "Got cancel event.");
                        return false;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
