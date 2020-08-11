using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace client
{
    public class WebSocketService
    {
        private const int BufferSize = 2048;
        private readonly ClientWebSocket m_Websocket;
        private readonly Uri m_WebsocketUri;

        public IConfiguration Configuration { get; }

        public WebSocketService(IConfiguration configuration)
        {
            var url = configuration["WebSocketUrl"];
            Configuration = configuration;
            m_Websocket = new ClientWebSocket();
            m_WebsocketUri = new Uri(url);
        }

        /// <summary>
        /// Connect to the websocket and begin yielding messages
        /// received from the connection.
        /// </summary>
        public async IAsyncEnumerable<string> ConnectAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var failedConnectMessage = string.Empty;

            try
            {
                await m_Websocket.ConnectAsync(m_WebsocketUri, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to connect to websocket: '{exception.Message}'");
                failedConnectMessage = $"Failed to connect to websocket '{exception.Message}'";
            }

            if (!string.IsNullOrEmpty(failedConnectMessage))
            {
                yield return failedConnectMessage;
                yield break;
            }

            var buffer = new ArraySegment<byte>(new byte[BufferSize]);

            while (!cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                using var memoryStream = new MemoryStream();
                do
                {
                    result = await m_Websocket.ReceiveAsync(buffer, cancellationToken);
                    memoryStream.Write(
                        buffer.Array,
                        buffer.Offset,
                        result.Count);

                } while (!result.EndOfMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                yield return Encoding.UTF8.GetString(memoryStream.ToArray());

                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }

        public Task CloseConnectionAsync()
            => m_Websocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Close",
                CancellationToken.None);
    }
}