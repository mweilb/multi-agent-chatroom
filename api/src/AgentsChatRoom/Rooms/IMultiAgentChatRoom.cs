using AgentOps.WebSockets;
using api.AgentsChatRoom.AgentRegistry;
using api.SemanticKernel.Modifications;
using Microsoft.SemanticKernel;
using System.Net.WebSockets;

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

namespace api.AgentsChatRoom.Rooms
{
    /// <summary>
    /// Defines the contract for a multi-agent chat room handler.
    /// Implementations are responsible for initializing agents, handling command invocations,
    /// and providing custom termination and selection strategies.
    /// </summary>
    public interface IMultiAgentChatRoom
    {   
        /// <summary>
        /// Gets the command name associated with this chat room.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Gets the emoji representation for this chat room.
        /// </summary>
        string Emoji { get; }

        /// <summary>
        /// Initializes the agent(s) for the chat room handler.
        /// </summary>
        /// <param name="agentRegistry">The agent registry containing agent definitions.</param>
        /// <param name="chatRoom">The chat room instance to be configured.</param>
        /// <param name="logger">Logger for tracking initialization and runtime events.</param>
        void Initialize(IAgentRegistry agentRegistry, AgentStreamingChatRoom chatRoom, ILogger<IMultiAgentChatRoom> logger);

        /// <summary>
        /// Configures agents using the provided Semantic Kernel.
        /// </summary>
        /// <param name="kernel">The kernel instance used to configure agents.</param>
        void ConfigureAgents(Kernel kernel);

        /// <summary>
        /// Handles an incoming command invocation from a WebSocket message.
        /// </summary>
        /// <param name="message">The incoming WebSocket message.</param>
        /// <param name="webSocket">The WebSocket connection for sending responses.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task HandleCommandAsync(WebSocketBaseMessage message, WebSocket webSocket);

        /// <summary>
        /// Gets the termination strategy used to determine when to stop agent responses.
        /// </summary>
        /// <returns>The termination strategy instance, or null if not defined.</returns>
        TerminationStreamingStrategy? GetTerminationStrategy();

        /// <summary>
        /// Gets the selection strategy used to choose the next agent to respond.
        /// </summary>
        /// <returns>The selection strategy instance, or null if not defined.</returns>
        SelectionStreamingStrategy? GetSelectionStrategy();

        
        /// <summary>
        /// Gets all agents in the chat room.
        /// </summary>
        /// <returns>The collection of agents in the chat room.</returns>
        IEnumerable<IAgentProfile> GetAllAgents();
    }
}

#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0110
