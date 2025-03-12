using System.Net.WebSockets;
using Microsoft.SemanticKernel;
using AgentOps.WebSockets;
using Microsoft.SemanticKernel.ChatCompletion;
using api.SemanticKernel.Modifications;
using api.AgentsChatRoom.WebSockets;
using api.AgentsChatRoom.AgentRegistry;
 
 
#pragma warning disable SKEXP0110

namespace api.AgentsChatRoom.Rooms
{
    /// <summary>
    /// An abstract multi-agent chat room handler that leverages an agent registry to load agents
    /// and an agent chat room to manage conversation state and streaming responses.
    /// This class encapsulates the logic to initialize agents, configure them with the Semantic Kernel,
    /// and handle WebSocket communications including streaming responses and error handling.
    /// </summary>
    public abstract class MultiAgentChatRoom : IMultiAgentChatRoom
    {
        // Private fields to hold dependencies for the agent registry, chat room, and logging.
        private IAgentRegistry? agentRegistry;
        private AgentStreamingChatRoom? chatRoom;
        private ILogger<IMultiAgentChatRoom>? logger;

        /// <summary>
        /// Gets the command name associated with this handler (e.g., "groupchat").
        /// Derived classes must specify a unique command name.
        /// </summary>
        public abstract string CommandName { get; }

        /// <summary>
        /// Gets the emoji representation for this handler.
        /// Derived classes can use an emoji to visually identify the chat room type.
        /// </summary>
        public abstract string Emoji { get; }

        /// <summary>
        /// Initializes the handler with its required dependencies:
        /// the agent registry to load agents, the chat room to manage conversation state,
        /// and a logger for tracking events and errors.
        /// </summary>
        /// <param name="agentRegistry">The registry containing agent definitions.</param>
        /// <param name="chatRoom">The chat room instance that manages conversation state and streaming responses.</param>
        /// <param name="logger">Logger instance for diagnostic purposes.</param>
        public void Initialize(
            IAgentRegistry agentRegistry,
            AgentStreamingChatRoom chatRoom,
            ILogger<IMultiAgentChatRoom> logger)
        {
            this.agentRegistry = agentRegistry;
            this.chatRoom = chatRoom;
            this.logger = logger;
        }

        /// <summary>
        /// Configures agents by initializing the agent registry with the provided Semantic Kernel,
        /// then sets up the chat room with the loaded agents.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance used for agent configuration.</param>
        public void ConfigureAgents(Kernel kernel)
        {
            // Log the start of the configuration process.
            logger?.LogInformation("Initializing agent registry for command: {CommandName}", CommandName);

            if (agentRegistry != null)
            {
                // Configure the agents using the Semantic Kernel.
                agentRegistry.ConfigureAgents(kernel);

                // Retrieve the chat agent instances from the registry and initialize the chat room.
                var chatAgentList = agentRegistry.GetAllAgents()
                                                 .Select(agent => agent.ChatAgent)
                                                 .ToList()
                                                 .AsReadOnly();
                chatRoom?.InitGroupChat(chatAgentList);
            }

            // Log that the agent chat room has been successfully initialized.
            logger?.LogInformation("Agent chat room initialized for command: {CommandName}", CommandName);
        }

        /// <summary>
        /// Main entry point for handling an incoming WebSocket command.
        /// Processes the message by adding it to the chat history and streaming agent responses back via a WebSocket sender.
        /// </summary>
        /// <param name="message">The incoming WebSocket message containing user content and metadata.</param>
        /// <param name="webSocket">The WebSocket connection used for sending responses back to the client.</param>
        /// <returns>A task representing the asynchronous handling operation.</returns>
        public async Task HandleCommandAsync(WebSocketBaseMessage message, WebSocket webSocket)
        {
            // Wrap the WebSocket connection with a sender helper to simplify sending messages.
            var sender = new WebSocketSender(webSocket);

            // Create a cancellation token to manage operation lifetime.
            using var cts = new CancellationTokenSource();
            CancellationToken cancellationToken = cts.Token;

            // Ensure that the chat room is properly initialized.
            if (chatRoom == null)
            {
                logger?.LogError("ChatRoom not initialized for {CommandName}", CommandName);
                await SendErrorAsync(sender, message.UserId, CommandName, $"ChatRoom not initialized {CommandName}", cancellationToken);
                return;
            }

            try
            {
                // Add the user's incoming message to the conversation history.
                chatRoom.AddChatMessage(new ChatMessageContent(AuthorRole.User, message.Content));

                // Begin streaming agent replies back to the client.
                await StreamAgentRepliesAsync(message, sender, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log any exceptions and send an error response to the client.
                logger?.LogError(ex, "Error occurred handling command {CommandName}", CommandName);
                await SendErrorAsync(sender, message.UserId, CommandName, $"Initialization or logic error: {ex.Message}", cancellationToken);
            }
        }

        /// <summary>
        /// Resets the chat room state by clearing the conversation history.
        /// </summary>
        /// <returns>A task that represents the asynchronous reset operation.</returns>
        public async Task Reset()
        {
            if (chatRoom != null)
            {
                await chatRoom.ResetAsync();
            }
            logger?.LogInformation("Chat history cleared for command: {CommandName}", CommandName);
        }

        /// <summary>
        /// Streams agent responses to the client, handling new agent response initiation and updating the message with chunked output.
        /// </summary>
        /// <param name="message">The original WebSocket message from the client.</param>
        /// <param name="sender">The WebSocket sender used to transmit responses back to the client.</param>
        /// <param name="cancellationToken">Token to observe cancellation requests during the streaming process.</param>
        /// <returns>A task representing the asynchronous streaming operation.</returns>
        protected virtual async Task StreamAgentRepliesAsync(
            WebSocketBaseMessage message,
            IWebSocketSender sender,
            CancellationToken cancellationToken)
        {
            if (chatRoom == null)
            {
                logger?.LogError("ChatRoom not initialized for {CommandName}", CommandName);
                await SendErrorAsync(sender, message.UserId, CommandName, $"ChatRoom not initialized {CommandName}", cancellationToken);
                return;
            }

            try
            {
                // Create an initial WebSocket reply message that will be updated with streaming content.
                WebSocketReplyChatRoomMessage currentMessage = CreateNewMessage(
                    message.UserId,
                    message.TransactionId,
                    CommandName
                );

                // Asynchronously stream content from the chat room.
                await foreach (var chunk in chatRoom.InvokeStreamingAsync(cancellationToken))
                {
                    AgentStreamingContent? agentChunk = (AgentStreamingContent)chunk;

                    if (agentChunk != null)
                    {
                        // If a new agent's response is starting, create a new message.
                        if (agentChunk.IsNewAgent)
                        {
                            currentMessage = CreateNewMessage(
                                message.UserId,
                                Guid.NewGuid().ToString(),
                                CommandName
                            );
                        }

                        // Update the message with the new content chunk.
                        UpdateMessage(currentMessage, agentChunk);

                        // Send the updated message back to the client.
                        await sender.SendAsync(currentMessage, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // If an error occurs during streaming, send an error message and log the error.
                await SendErrorAsync(sender, message.UserId, CommandName, $"Error: {ex.Message}", cancellationToken);
                logger?.LogError(ex, "Error during streaming for command: {CommandName}", CommandName);
            }
        }

        /// <summary>
        /// Sends an error message to the client over the WebSocket.
        /// This helper method creates a standardized error message and sends it using the provided sender.
        /// </summary>
        /// <param name="sender">The WebSocket sender used to transmit messages.</param>
        /// <param name="userId">The user ID to which the error message is sent.</param>
        /// <param name="command">The command context for the error.</param>
        /// <param name="error">A detailed error message.</param>
        /// <param name="cancellationToken">Token to observe cancellation requests.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        protected virtual async Task SendErrorAsync(
            IWebSocketSender sender,
            string userId,
            string command,
            string error,
            CancellationToken cancellationToken)
        {
            var errorResponse = CreateError(userId, CommandName, error);
            await sender.SendAsync(errorResponse, cancellationToken);
        }

        /// <summary>
        /// Creates a new WebSocket reply message for use in streaming agent responses.
        /// This method initializes the message with default values.
        /// </summary>
        /// <param name="userId">The ID of the user sending the message.</param>
        /// <param name="transactionId">A unique identifier for the transaction.</param>
        /// <param name="command">The command context associated with the message.</param>
        /// <returns>A new instance of <see cref="WebSocketReplyChatRoomMessage"/>.</returns>
        public static WebSocketReplyChatRoomMessage CreateNewMessage(
            string userId,
            string transactionId,
            string command
        ) => new()
        {
            UserId = userId,
            TransactionId = transactionId,
            Action = command,
            SubAction = "chunk",
            Content = string.Empty,
            AgentName = "Unknown",
        };

        /// <summary>
        /// Creates an error message formatted for WebSocket transmission.
        /// </summary>
        /// <param name="userId">The target user ID for the error.</param>
        /// <param name="command">The command associated with the error.</param>
        /// <param name="explanation">A detailed explanation of the error.</param>
        /// <returns>A new instance of <see cref="WebSocketReplyChatRoomMessage"/> representing the error.</returns>
        public static WebSocketReplyChatRoomMessage CreateError(
            string userId,
            string command,
            string explanation
        ) => new()
        {
            UserId = userId,
            TransactionId = Guid.NewGuid().ToString(),
            Action = command,
            SubAction = "error",
            Content = explanation,
            AgentName = string.Empty,
        };

        /// <summary>
        /// Updates a WebSocket reply message with new streaming content.
        /// It sets the agent name, updates hints, and maintains the "chunk" subaction.
        /// </summary>
        /// <param name="currentMessage">The current message to be updated.</param>
        /// <param name="content">The latest streaming content from an agent.</param>
        internal void UpdateMessage(WebSocketReplyChatRoomMessage currentMessage, AgentStreamingContent content)
        {
            // Set the agent name if available; otherwise, default to "Deciding..."
            currentMessage.AgentName = content.AgentName ?? "Deciding...";
            
            //find the agents in list and if not found, default to null
            var agent = GetAllAgents().FirstOrDefault(a => a.Name == content.AgentName);
            currentMessage.Emoji = agent?.Emoji ?? "🤔";

            // Update the hints provided by the agent.
            currentMessage.Hints = new(content.Hints);
            // Ensure the subaction remains "chunk" for streaming.
            currentMessage.SubAction = "chunk";
        }

        /// <summary>
        /// Retrieves all agent profiles from the agent registry.
        /// If the registry is uninitialized, returns an empty collection.
        /// </summary>
        /// <returns>An enumerable of <see cref="IAgentProfile"/>.</returns>
        public IEnumerable<IAgentProfile> GetAllAgents()
        {
            return agentRegistry?.GetAllAgents() ?? Enumerable.Empty<IAgentProfile>();
        }

        // ----------------------------------------------------
        // Derived classes supply their custom strategies if needed.
        // ----------------------------------------------------

        /// <summary>
        /// Derived classes can supply a custom termination strategy for streaming responses.
        /// Examples might include returning the first agent's response, using an aggregator, etc.
        /// </summary>
        /// <returns>An instance of <see cref="TerminationStreamingStrategy"/> or null.</returns>
        public abstract TerminationStreamingStrategy? GetTerminationStrategy();

        /// <summary>
        /// Derived classes can supply a custom selection strategy for choosing an agent's response.
        /// This could involve selecting the best agent, choosing randomly, or applying specialized logic.
        /// </summary>
        /// <returns>An instance of <see cref="SelectionStreamingStrategy"/> or null.</returns>
        public abstract SelectionStreamingStrategy? GetSelectionStrategy();
    }
}

 
#pragma warning restore SKEXP0110
