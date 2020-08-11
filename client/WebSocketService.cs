using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client
{
    public class WebSocketService
    {
        private const int BufferSize = 2048;
        private readonly ClientWebSocket m_Websocket;
        private readonly Uri m_WebsocketUri;

        public WebSocketService()
        {
            m_Websocket = new ClientWebSocket();
#if DEBUG
            var webSocketUrl = "wss://webapi:443";
#else
            var webSocketUrl = Environment.GetEnvironmentVariable("WEBSOCKET-URL");
#endif
            m_WebsocketUri = new Uri(webSocketUrl);
        }

        /// <summary>
        /// Connect to the websocket and begin yielding messages
        /// received from the connection.
        /// </summary>
        public async IAsyncEnumerable<string> ConnectAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await m_Websocket.ConnectAsync(m_WebsocketUri, cancellationToken);

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