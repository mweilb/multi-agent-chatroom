using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AgentOps.WebSockets;
using api.AgentsChatRoom.AgentRegistry;
using api.AgentsChatRoom.WebSockets;
using api.SemanticKernel.Modifications;
using Microsoft.SemanticKernel;

namespace api.AgentsChatRoom.Rooms
{
    /// <summary>
    /// Central manager for agent handlers. Responsible for assembling and registering agent chat rooms.
    /// This class wires together agent registries, chat room instances, and logging, then registers the commands for WebSocket handling.
    /// </summary>
    public class MultiAgentChatRooms
    {
        // List of registered agent handlers.
        private readonly List<IMultiAgentChatRoom> rooms = new();

        /// <summary>
        /// Adds a new agent chat room room.
        /// This method:
        ///   1) Validates the provided agent registry and room.
        ///   2) Retrieves termination and selection strategies from the room.
        ///   3) Creates a default AgentStreamingChatRoom with these strategies.
        ///   4) Creates a logger (using LoggerFactory with Console output).
        ///   5) Initializes the room with the registry, chat room, and logger.
        ///   6) Configures agents via the room (calls registry.ConfigureAgents and chatRoom.InitGroupChat internally).
        ///   7) Stores the room for later registration.
        /// </summary>
        /// <param name="registry">An instance of IAgentRegistry to configure agent definitions.</param>
        /// <param name="room">The multi-agent chat room room to add.</param>
        /// <param name="kernel">The Semantic Kernel instance for agent configuration.</param>
        /// <returns>True if the room was successfully added; otherwise, false.</returns>
        public bool AddAgentChatRoom(IAgentRegistry registry, IMultiAgentChatRoom room, Kernel kernel)
        {
            // 1. Validate the provided registry and room.
            if (registry == null || room == null)
            {
                return false;
            }

            // 2. Retrieve termination and selection strategies from the room.
            var terminationStrategy = room.GetTerminationStrategy();
            var selectionStrategy = room.GetSelectionStrategy();
            if (terminationStrategy == null || selectionStrategy == null)
            {
                return false;
            }

            // 3. Create a default AgentStreamingChatRoom with the provided strategies.
            var chatRoom = new AgentStreamingChatRoom(
                terminationStrategy: terminationStrategy,
                selectionStrategy: selectionStrategy,
                kernel: kernel
            );

            // 4. Create a logger using LoggerFactory (this example logs to the console).
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<IMultiAgentChatRoom>();

            // 5. Initialize the room with the registry, chat room, and logger.
            room.Initialize(registry, chatRoom, logger);

            // 6. Configure agents via the room.
            room.ConfigureAgents(kernel);

            // 7. Store the room for later registration.
            rooms.Add(room);

            return true;
        }

        /// <summary>
        /// Registers all agent chat room handlers with the provided WebSocketHandler.
        /// Each room's command name is mapped to its HandleCommandAsync callback.
        /// Also registers a special "rooms" command to retrieve a list of available rooms.
        /// </summary>
        /// <param name="webSocketHandler">The WebSocketHandler used for registering commands.</param>
        /// <param name="kernel">The Semantic Kernel instance (passed if needed by commands).</param>
        public void RegisterChatRooms(WebSocketHandler webSocketHandler, Kernel kernel)
        {
            // Register the "rooms" command using the helper function.
            webSocketHandler.RegisterCommand("rooms", HandleRoomsCommandAsync);

            // Register each room's command.
            foreach (var room in rooms)
            {
                webSocketHandler.RegisterCommand(room.CommandName, room.HandleCommandAsync);
            }
        }

        /// <summary>
        /// Handles the "rooms" command by returning a list of available chat rooms.
        /// The response is serialized to JSON and sent back over the WebSocket.
        /// </summary>
        /// <param name="message">The incoming WebSocket message.</param>
        /// <param name="webSocket">The WebSocket connection for sending the response.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task HandleRoomsCommandAsync(WebSocketBaseMessage message, WebSocket webSocket)
        {
            if (message.SubAction == "get")
            {
                // Create a response message for the "rooms" command.
                var response = new WebSocketGetRoomsMessage
                {
                    UserId = "system",
                    TransactionId = message.TransactionId,
                    Action = "rooms",
                    SubAction = "room list",
                    Content = "List of available rooms"
                };

                // Populate the response with available rooms from the registered handlers.
                foreach (var room in rooms)
                {
 

                    response.Rooms.Add(new WebSocketGetRooms
                    {
                        
                        Name = room.CommandName,
                        Emoji = room.Emoji,
                        Agents = [.. room.GetAllAgents().Select(agent => new WebSocketAgentProfile
                                    {
                                        Name = agent.Name,
                                        Emoji = agent.Emoji
                                    })
                                ]
                    });
                }

                // Serialize the response to JSON.
                var responseJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                // Send the JSON response back to the client.
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseJson),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }
}
