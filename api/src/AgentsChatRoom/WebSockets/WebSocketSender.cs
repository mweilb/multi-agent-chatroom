using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace api.AgentsChatRoom.WebSockets
{
    /// <summary>
    /// Default implementation of <see cref="IWebSocketSender"/> that sends JSON messages over an actual WebSocket.
    /// </summary>
    public class WebSocketSender : IWebSocketSender
    {
        // The underlying WebSocket used for sending messages.
        private readonly WebSocket webSocket;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketSender"/> class.
        /// </summary>
        /// <param name="webSocket">The WebSocket instance to send messages through.</param>
        public WebSocketSender(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }

        /// <summary>
        /// Asynchronously sends a chat room reply message as a JSON string over the WebSocket.
        /// </summary>
        /// <param name="message">The chat room reply message to be sent.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        public async Task SendAsync(WebSocketReplyChatRoomMessage message, CancellationToken cancellationToken = default)
        {
            // Configure JSON serialization options to format the output and include public fields.
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            };

            // Serialize the message to a JSON string.
            string json = JsonSerializer.Serialize(message, options);
            // Convert the JSON string to UTF8-encoded bytes.
            var bytes = Encoding.UTF8.GetBytes(json);

            // Send the JSON message over the WebSocket.
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: cancellationToken
            );
        }
    }
}
