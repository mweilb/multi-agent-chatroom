using AgentOps.WebSockets;
using OllamaSharp.Models.Chat;
using api.src.Agents; 
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.Pinecone; 

namespace api.src.Websockets
{
    public class WebSocketListener
    {
        private readonly Dictionary<(string idUser, string TransactionId), List<WebSocketBaseMessage>> messages = new();
        private readonly SKAgents _skAgent; 

        public WebSocketListener(WebSocketHandler handler, SKAgents skAgent)
        {
            _skAgent = skAgent ?? throw new ArgumentNullException(nameof(skAgent));
            handler.OnMessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(string message)
        {
            Console.WriteLine($"Message From WebSocket: {message}");
            try
            {
                // ✅ Deserialize JSON safely
                var incomingMessage = JsonSerializer.Deserialize<WebSocketBaseMessage>(message);
                if (incomingMessage == null)
                {
                    Console.WriteLine("Received empty or malformed message.");
                    return;
                }

                var identifier = (incomingMessage.UserId, incomingMessage.TransactionId);

                // ✅ Store messages in dictionary safely
                if (!messages.ContainsKey(identifier))
                {
                    messages[identifier] = new List<WebSocketBaseMessage>();
                }
                messages[identifier].Add(incomingMessage);
                Console.WriteLine("Values added to dictionary");

                // ✅ Ensure message has content
                if (string.IsNullOrWhiteSpace(incomingMessage.Content))
                {
                    Console.WriteLine("Message content is empty, skipping processing.");
                    return;
                }

                // ✅ Send to SKAgents for processing
                var processedResponse = await _skAgent.ProcessMessageAsync(incomingMessage.Content);

                // ✅ Store processed data in Pinecone
                var pineconeRecord = new PineconeVectorStoreRecord(
                    id: $"{incomingMessage.UserId}-{incomingMessage.TransactionId}",
                    vector: processedResponse.Vector, 
                    metadata: new Dictionary<string, object>
                    {
                        { "userId", incomingMessage.UserId },
                        { "transactionId", incomingMessage.TransactionId },
                        { "content", processedResponse.Content }
                    }
                );

                await _skAgent.StoreInPineconeAsync(pineconeRecord);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
            }
        }
    }
}
