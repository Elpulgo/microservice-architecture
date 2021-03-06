using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace client
{
    public class WebSocketService
    {
        private const int BufferSize = 2048;
        private readonly ClientWebSocket m_Websocket;
        public Uri BaseAddress { get; set; }

        public WebSocketService()
        {
            m_Websocket = new ClientWebSocket();
        }

        public async Task<string> ConnectAsync(CancellationToken cancellationToken)
        {
            var failedConnectMessage = string.Empty;

            try
            {
                await m_Websocket.ConnectAsync(BaseAddress, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to connect to websocket: '{exception.Message}'");
                failedConnectMessage = $"Failed to connect to websocket '{exception.Message}'";
            }

            return failedConnectMessage;
        }

        public async IAsyncEnumerable<PostModel> ConsumeAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
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

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                memoryStream.Seek(0, SeekOrigin.Begin);

                yield return JsonSerializer.Deserialize<PostModel>(Encoding.UTF8.GetString(memoryStream.ToArray()));
            }
        }

        public Task CloseConnectionAsync()
            => m_Websocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Close",
                CancellationToken.None);
    }
}