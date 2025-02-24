using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using api.AgentsChatRoom.Rooms;
using System.Runtime.CompilerServices;
using api.SemanticKernel.Modifications;
using api.AgentsChatRoom.AgentRegistry; // For WebSocketMessage, etc.

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

namespace api.Agents
{
    /// <summary>
    /// A specialized agent registry that registers the "ArtDirector" and "CopyWriter" agents 
    /// with predefined instructions.
    /// </summary>
    public class ExampleAgentRegistry : AgentRegistry
    {
        // Instructions for the Art Director agent.
        private const string ArtDirectorInstructions =
            @"You are an art director who has opinions about copywriting born of a love for David Ogilvy.
The goal is to determine if the given copy is acceptable to print.
If so, state that it is approved.
If not, provide insight on how to refine suggested copy without example.";

        // Instructions for the Copy Writer agent.
        private const string CopyWriterInstructions =
            @"You are a copywriter with ten years of experience and are known for brevity and a dry humor.
The goal is to refine and decide on the single best copy as an expert in the field.
Only provide a single proposal per response.
You're laser focused on the goal at hand.
Don't waste time with chit chat.
Consider suggestions when refining an idea.";

        /// <summary>
        /// Configures the agents by adding the ArtDirector and CopyWriter agents to the registry.
        /// </summary>
        /// <param name="kernel">The kernel used to register the agents.</param>
        public override void ConfigureAgents(Kernel kernel)
        {
            // Register the ArtDirector agent with its specific instructions.
            AddAgent(kernel, "ArtDirector", ArtDirectorInstructions,"🎨");
            // Register the CopyWriter agent with its specific instructions.
            AddAgent(kernel, "CopyWriter", CopyWriterInstructions,"✍️");
        }
    }

    /// <summary>
    /// A custom termination strategy that terminates the conversation as soon as the ArtDirector
    /// provides a response indicating approval.
    /// </summary>
    public class ApprovalTerminationStrategy : TerminationStreamingStrategy
    {
        /// <summary>
        /// Evaluates the conversation history and determines if termination should occur based on the ArtDirector's response.
        /// </summary>
        /// <param name="streamingContent">Container to store hints and intermediate results.</param>
        /// <param name="agent">The current agent being evaluated.</param>
        /// <param name="history">The conversation history.</param>
        /// <param name="cancellationToken">Cancellation token for asynchronous operation.</param>
        /// <returns>An asynchronous stream yielding a boolean value that indicates termination.</returns>
        public override async IAsyncEnumerable<bool> ShouldAgentTerminateStreaming(
            AgentStreamingContent streamingContent,
            Agent agent,
            IReadOnlyList<ChatMessageContent> history,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Run the termination logic asynchronously.
            var terminationResult = await Task.Run(() =>
            {
                // Get the last message from the history.
                var lastMessage = history[^1];
                // Determine termination based on the message content and agent name.
                bool shouldTerminate =
                    lastMessage.Content is not null &&
                    lastMessage.Content.StartsWith("approve", StringComparison.OrdinalIgnoreCase) &&
                    agent.Name is not null &&
                    agent.Name.Equals("ArtDirector", StringComparison.OrdinalIgnoreCase);

                // Build a rationale string based on the termination decision.
                string rationale = shouldTerminate
                    ? "Art Director said approved"
                    : "Art Director said not approved";

                // Update hints with the termination decision details.
                streamingContent.Hints["terminate-decision"] = new Dictionary<string, string>
                {
                    { "content", rationale },
                    { "reason", "code" }
                };

                return shouldTerminate;
            }, cancellationToken);

            // Yield the final termination result.
            yield return terminationResult;
        }

        /// <summary>
        /// Synchronous termination decision is not implemented for this strategy.
        /// </summary>
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A custom agent selection strategy that chooses the CopyWriter if the prompt mentions "copy",
    /// otherwise it selects the ArtDirector.
    /// </summary>
    public class CustomAgentSelectionStrategy : SelectionStreamingStrategy
    {
        /// <summary>
        /// Evaluates the conversation history and selects an agent based on the last message content.
        /// </summary>
        /// <param name="streamingContent">Container to store hints and intermediate results.</param>
        /// <param name="agents">List of available agents.</param>
        /// <param name="history">The conversation history.</param>
        /// <param name="cancellationToken">Cancellation token for asynchronous operation.</param>
        /// <returns>An asynchronous stream yielding the selected agent.</returns>
        public override async IAsyncEnumerable<Agent?> SelectAgentStreaming(
            AgentStreamingContent streamingContent,
            IReadOnlyList<Agent> agents,
            IReadOnlyList<ChatMessageContent> history,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Run the selection logic asynchronously.
            var selectionResult = await Task.Run(() =>
            {
                // Retrieve the last message in the conversation.
                var lastMessage = history[^1];
                // Check if the last message content mentions "copy".
                bool promptMentionsCopy =
                    lastMessage.Content is not null &&
                    lastMessage.Content.Contains("copy", StringComparison.OrdinalIgnoreCase);

                Agent? selectedAgent = null;
                string selectionReason = string.Empty;

                // Determine the next agent based on the last message's content and author.
                if (promptMentionsCopy || (lastMessage.AuthorName != "ArtDirector" && lastMessage.AuthorName != "CopyWriter"))
                {
                    // If "copy" is mentioned, select the CopyWriter.
                    selectedAgent = agents.FirstOrDefault(a => string.Equals(a.Name, "CopyWriter", StringComparison.OrdinalIgnoreCase));
                    selectionReason = "User said copy, therefore selecting CopyWriter.";
                }
                else if (lastMessage.AuthorName == "CopyWriter")
                {
                    // If the last message is from CopyWriter, select ArtDirector next.
                    selectedAgent = agents.FirstOrDefault(a => string.Equals(a.Name, "ArtDirector", StringComparison.OrdinalIgnoreCase));
                    selectionReason = "CopyWriter just responded, next is ArtDirector.";
                }
                else
                {
                    // Otherwise, select the CopyWriter.
                    selectedAgent = agents.FirstOrDefault(a => string.Equals(a.Name, "CopyWriter", StringComparison.OrdinalIgnoreCase));
                    selectionReason = "ArtDirector just responded, next is CopyWriter.";
                }

                // Update hints with the selection decision details.
                streamingContent.Hints["selection-content"] = new Dictionary<string, string>
                {
                    { "content", selectionReason },
                    { "reason", "code" }
                };

                return selectedAgent;
            }, cancellationToken);

            // Yield the final selected agent.
            yield return selectionResult;
        }

        /// <summary>
        /// Synchronous agent selection is not implemented for this strategy.
        /// </summary>
        protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A concrete agent handler that uses the specialized ExampleAgentRegistry and custom strategies.
    /// It sets up a command name and assigns custom termination and selection strategies.
    /// In a real application, an instance of this class would be integrated into your WebSocket or command-dispatch pipeline.
    /// </summary>
    public class ExampleAgentHandler : MultiAgentChatRoom
    {
        // Define the command name and associated emoji.
        public override string CommandName => "groupchat";
        public override string Emoji => "🤖";

        // Single instances of the termination and selection strategies.
        private readonly TerminationStreamingStrategy terminationStrategy;
        private readonly SelectionStreamingStrategy selectionStrategy;

        /// <summary>
        /// Initializes a new instance of the ExampleAgentHandler class,
        /// instantiating custom termination and selection strategies.
        /// </summary>
        public ExampleAgentHandler()
        {
            terminationStrategy = new ApprovalTerminationStrategy();
            selectionStrategy = new CustomAgentSelectionStrategy();
        }

        /// <summary>
        /// Returns the custom termination strategy.
        /// </summary>
        /// <returns>The termination strategy instance.</returns>
        public override TerminationStreamingStrategy? GetTerminationStrategy()
        {
            return terminationStrategy;
        }

        /// <summary>
        /// Returns the custom selection strategy.
        /// </summary>
        /// <returns>The selection strategy instance.</returns>
        public override SelectionStreamingStrategy? GetSelectionStrategy()
        {
            return selectionStrategy;
        }
    }
}

#pragma warning restore SKEXP0110
#pragma warning restore SKEXP0001
