using AgentOps.WebSockets;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.Json;

namespace api.src.Websockets
{
    public class WebSocketListener
    {
        private readonly Dictionary<(string idUser, string TransactionId), List<WebSocketBaseMessage>> messages = new ();
        public WebSocketListener(WebSocketHandler handler)
        {
            // Subscribe to the OnMessageReceived event
            handler.OnMessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(string message)
        {
            Console.WriteLine($"Message From Wss: {message}");
            try
            {
                // Deserialize the JSON into a WebSocketBaseMessage.
                var incomingMessage = JsonSerializer.Deserialize<WebSocketBaseMessage>(message);
                if (incomingMessage != null)
                {
                    var identifier = (incomingMessage.UserId, incomingMessage.TransactionId);
                    if (messages.ContainsKey((incomingMessage.UserId, incomingMessage.TransactionId)))
                        {
                        var userMessagesList = messages[identifier];
                        userMessagesList.Add(incomingMessage);
                        messages.Add((incomingMessage.UserId, incomingMessage.TransactionId), userMessagesList);
                        Console.WriteLine("Values added to dictionary");
                    }
                    else
                    {
                       messages[identifier] = new List<WebSocketBaseMessage> {incomingMessage};
                    }
                }
                //foreach (var _message in messages.Keys)
                //{
                //    Console.WriteLine($"Key: {_message}, Value: \n");
                //    foreach (var key in messages[_message])
                //    {
                //        Console.WriteLine($"{key}, ");
                //    }
                //}
                //The part where you send it to the semantic kernel
            }
            catch (Exception ex) { Console.WriteLine($" Error deserializing the message: {ex.Message}"); }
        }
    }
}
