using AgentOps.WebSockets;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Embeddings;

namespace api.AgentsChatRoom.WebSockets
{
    /// <summary>
    /// Listens for WebSocket messages, stores them in memory, and sends them to a Semantic Kernel agent for embedding.
    /// </summary>
    public class WebSocketMessageListener
    {
        private readonly ConcurrentDictionary<string, string> _messageStore = new();
        private readonly ConcurrentDictionary<string, float[]> _embeddingStore = new();
        private readonly Kernel _kernel;
        private readonly ITextEmbeddingGeneration _embeddingService;

        /// <summary>
        /// Initializes the WebSocketMessageListener with Semantic Kernel.
        /// </summary>
        public WebSocketMessageListener(Kernel kernel)
        {
            _kernel = kernel;
            _embeddingService = _kernel.GetService<ITextEmbeddingGeneration>();
        }

        /// <summary>
        /// Listens for messages from the WebSocket, stores them, and sends them to a Semantic Kernel agent for embedding.
        /// </summary>
        public async Task ListenForMessages(WebSocket webSocket, string connectionId)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received WebSocket message from {connectionId}: {messageText}");

                    // Store the raw message
                    _messageStore[connectionId] = messageText;

                    // Generate embedding using Semantic Kernel agent
                    float[] embedding = await GenerateEmbeddingAsync(messageText);

                    // Store the embedding
                    _embeddingStore[connectionId] = embedding;

                    Console.WriteLine($"Generated embedding for {connectionId}: [{string.Join(", ", embedding[..5])}...]");
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine($"WebSocket {connectionId} closed.");
                }
            }
        }

        /// <summary>
        /// Generates an embedding for the given message using Semantic Kernel.
        /// </summary>
        private async Task<float[]> GenerateEmbeddingAsync(string message)
        {
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(message);
                return embedding.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embedding: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// Retrieves the last received message for a given WebSocket connection.
        /// </summary>
        public string? GetMessage(string connectionId)
        {
            return _messageStore.TryGetValue(connectionId, out var message) ? message : null;
        }

        /// <summary>
        /// Retrieves the last generated embedding for a given WebSocket connection.
        /// </summary>
        public float[]? GetEmbedding(string connectionId)
        {
            return _embeddingStore.TryGetValue(connectionId, out var embedding) ? embedding : null;
        }
    }
}
