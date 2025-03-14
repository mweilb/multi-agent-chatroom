using api.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

namespace api.SemanticKernel.Modifications
{
    /// <summary>
    /// Represents a chat room that streams agent responses in a multi-agent conversation.
    /// It utilizes custom termination and selection strategies and leverages a Semantic Kernel
    /// for prompt invocations.
    /// </summary>
    public sealed class AgentStreamingChatRoom(
        TerminationStreamingStrategy terminationStrategy,
        SelectionStreamingStrategy selectionStrategy,
        Kernel kernel) : AgentChat
    {
        // Custom strategies and kernel instance.
        private readonly TerminationStreamingStrategy _terminationStrategy = terminationStrategy;
        private readonly SelectionStreamingStrategy _selectionStrategy = selectionStrategy;
        private readonly Kernel _kernel = kernel;

        /// <summary>
        /// Holds the collection of chat agents. This should be set via <see cref="InitGroupChat"/>.
        /// </summary>
        public IReadOnlyCollection<ChatCompletionAgent>? _agents;

        /// <summary>
        /// Indicates whether the conversation is complete.
        /// </summary>
        public bool isComplete = false;

        /// <summary>
        /// Gets the list of agents as a list of <see cref="Agent"/> objects.
        /// </summary>
        public override IReadOnlyList<Agent> Agents => _agents?.Cast<Agent>().ToList() ?? new List<Agent>();

 
        /// <summary>
        /// Initializes the group chat by setting the available agents.
        /// </summary>
        /// <param name="agents">A collection of configured chat completion agents.</param>
        /// <exception cref="InvalidOperationException">Thrown if no agents are provided.</exception>
        public void InitGroupChat(IReadOnlyCollection<ChatCompletionAgent> agents)
        {
            if (!agents.Any())
            {
                throw new InvalidOperationException("No agents have been loaded. Please configure them first.");
            }

            _agents = agents;
        }

        /// <summary>
        /// Resets the conversation history.
        /// </summary>
        public void Reset()
        {
            History.Clear();
        }

        /// <summary>
        /// Adds a user message to the conversation history.
        /// </summary>
        /// <param name="userMessage">The message content from the user.</param>
        /// <returns>A completed task.</returns>
        public async Task AddUserMessageAsync(string userMessage)
        {
            var entry = new ChatMessageContent(AuthorRole.User, userMessage)
            {
                AuthorName = "User"
            };

            History.Add(entry);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Streams the conversation by invoking agent responses iteratively.
        /// It uses selection and termination strategies to determine the flow of conversation.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An asynchronous stream of <see cref="StreamingChatMessageContent"/> items.</returns>
        public override async IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_agents == null || !_agents.Any())
            {
                throw new InvalidOperationException("No agents have been loaded. Please initialize the agents first.");
            }

            // Cast loaded agents to a list of Agent.
            IReadOnlyList<Agent> agentsList = _agents.Cast<Agent>().ToList();
            bool conversationComplete = false;

            // Loop for a maximum of maxIterations or until termination is signaled.
            int maxIterations = _terminationStrategy.MaximumIterations;
            for (int iteration = 0; !conversationComplete && iteration < maxIterations; iteration++)
            {
                ChatCompletionAgent? selectedAgent = null;
                AgentStreamingContent streamingContent = new();
                bool isNewAgentFlag = true;

                // Execute the selection strategy to pick an agent.
                await foreach (var iterationContent in _selectionStrategy.SelectAgentStreaming(streamingContent, agentsList, History, cancellationToken))
                {
                    selectedAgent = (ChatCompletionAgent?)iterationContent;
                    streamingContent.IsNewAgent = isNewAgentFlag;

                    yield return streamingContent;
                    isNewAgentFlag = false;
                }

                // Ensure an agent was selected.
                if (selectedAgent == null)
                {
                    throw new InvalidOperationException("No agent selected.");
                }

                // Mark the selected agent in the streaming content.
                streamingContent.AgentName = selectedAgent.Name;
                yield return new AgentStreamingContent(streamingContent);

                // Stream the agent's response.
                await foreach (var (prompt, responseContent, reasoning) in StreamAgentResponseAsync(selectedAgent, cancellationToken))
                {
                    var hintData = new Dictionary<string, string>
                    {
                        ["prompt"] = prompt,
                        ["content"] = responseContent,
                        ["reasons"] = reasoning
                    };

                    streamingContent.Hints["agent"] = hintData;

                    yield return new AgentStreamingContent(streamingContent);
                }

                // Check if termination strategy signals the conversation should end.
                await foreach (var shouldTerminate in _terminationStrategy.ShouldAgentTerminateStreaming(streamingContent, selectedAgent, History, cancellationToken))
                {
                    conversationComplete = shouldTerminate;
                    yield return streamingContent;
                }

                if (conversationComplete)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Streams the response from the selected agent.
        /// It builds a prompt from the conversation history and the agent's instructions,
        /// then returns the streaming response content.
        /// </summary>
        /// <param name="agent">The agent whose response is being streamed.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An asynchronous stream of tuples containing the prompt, plain text response, and thinking text.</returns>
        private async IAsyncEnumerable<(string Prompt, string Response, string Thinking)> StreamAgentResponseAsync(ChatCompletionAgent agent, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Build the prompt from conversation history and agent instructions.
            var promptContent = string.Join("\n", History.Select(msg => $"{msg.AuthorName}: {msg.Content}"));
            var prompt = $"{promptContent}\n\nInstructions: {agent.Instructions}";

            // Use a StringBuilder to accumulate streaming chunks.
            StringBuilder overallResponse = new();
            string updatedPlainText = string.Empty;
            string updatedThinkingText = string.Empty;

            // Invoke the prompt asynchronously using the kernel's streaming API.
            var streamingResponse = _kernel.InvokePromptStreamingAsync(prompt, null, null, null, cancellationToken);

            // Yield each chunk as it is received.
            await foreach (var chunk in streamingResponse)
            {
                string currentChunk = chunk.ToString() ?? string.Empty;
                overallResponse.Append(currentChunk);
                (updatedPlainText, updatedThinkingText) = OllamaHelper.SplitContentFromThinking(overallResponse.ToString());

                yield return (prompt, updatedPlainText, updatedThinkingText);
            }

            // After receiving the full response, add it to the conversation history.
            var responseEntry = new ChatMessageContent(AuthorRole.User, updatedPlainText)
            {
                AuthorName = agent.Name  // Set the agent's name as the author.
            };

            History.Add(responseEntry);
        }

        /// <summary>
        /// Not implemented for streaming; use <see cref="InvokeStreamingAsync(CancellationToken)"/> instead.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An asynchronous stream of <see cref="ChatMessageContent"/> items.</returns>
        public override IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0110
