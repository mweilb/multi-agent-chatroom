using api.SemanticKernel.Modifications;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Runtime.CompilerServices;

#pragma warning disable SKEXP0110

namespace api.Agents.Yaml
{
    /// <summary>
    /// Implements a selection strategy that uses YAML configuration to decide which agent should respond next.
    /// The strategy processes conversation history using preconditions and filtering, then applies a selection prompt.
    /// </summary>
    public class YamlSelectionStrategy(Kernel kernel, string selectionCriteriaPrompt, List<string>? preconditionPrompts, string? filterInstruction)
        : SelectionStreamingStrategy
    {
        // Instance of the kernel used to invoke prompts.
        private readonly Kernel kernelInstance = kernel ?? throw new ArgumentNullException(nameof(kernel));

        // Optional list of precondition prompts for filtering the conversation history.
        private readonly List<string>? preconditionPromptsInternal = preconditionPrompts;

        // Optional filter instruction to apply during history processing.
        private readonly string? filterInstructionInternal = filterInstruction;

        // The description prompt outlining the selection criteria.
        private readonly string selectionCriteriaPromptInternal = selectionCriteriaPrompt ?? throw new ArgumentNullException(nameof(selectionCriteriaPrompt));

        /// <summary>
        /// Streams the agent selection process by first filtering the conversation history and then applying a selection prompt.
        /// Intermediate results are yielded as null until the final agent is selected.
        /// </summary>
        /// <param name="streamingContent">Container for hints and intermediate results.</param>
        /// <param name="agents">List of available agents.</param>
        /// <param name="chatHistory">The conversation history.</param>
        /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous stream that eventually yields the selected agent.</returns>
        public override async IAsyncEnumerable<Agent?> SelectAgentStreaming(
            AgentStreamingContent streamingContent,
            IReadOnlyList<Agent> agents,
            IReadOnlyList<ChatMessageContent> chatHistory,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string filteredHistoryJson = string.Empty;

            // STEP 1: Filter the conversation history based on preconditions and filter instructions.
            await foreach (var (promptText, filteredJson, filterThinking) in YamlHistory.GetFilteredHistoryResultAsync(
                preconditionPrompts: preconditionPromptsInternal,
                filterInstruction: filterInstructionInternal,
                chatHistory: chatHistory,
                kernel: kernelInstance,
                cancellationToken: cancellationToken))
            {
                // Update hints with the intermediate filtering result.
                streamingContent.Hints["select-history"] = new Dictionary<string, string>
                {
                    { "prompt", promptText },
                    { "content", filteredJson },
                    { "reason", filterThinking }
                };

                // Yield null to indicate ongoing processing.
                yield return null;

                // Save the filtered history JSON for the next step.
                filteredHistoryJson = filteredJson;
            }

            // STEP 2: Construct the selection prompt using the filtered history and the selection criteria.
            string selectionPrompt = $@"
                History:
                {filteredHistoryJson}

                Agent Selection Criteria:
                {selectionCriteriaPromptInternal}

                Based on the above, please return a JSON object with two properties:
                - ""rationale"": An explanation of your decision.
                - ""nextAgent"": The name of the agent to respond next.

                Example Output:
                {{
                    ""rationale"": ""The last message was from User, so according to the rules, the next agent should be CopyWriter."",
                    ""nextAgent"": ""CopyWriter""
                }}

                Return only the JSON response without any additional commentary.
            ";

            string decisionJson = string.Empty;

            // STEP 3: Invoke the selection prompt and process the decision result.
            await foreach (var (finalPrompt, decisionResult, decisionThinking) in YamlHelpers.GetJsonDecisionAsync(selectionPrompt, kernelInstance, cancellationToken))
            {
                // Update hints with the intermediate decision result.
                streamingContent.Hints["select-content"] = new Dictionary<string, string>
                {
                    { "prompt", finalPrompt },
                    { "content", decisionResult },
                    { "reason", decisionThinking }
                };

                // Yield null to indicate processing is still underway.
                yield return null;

                // Save the decision JSON for final parsing.
                decisionJson = decisionResult;
            }

            // STEP 4: Parse the final JSON decision to extract the next agent's name and rationale.
            (string nextAgentName, string rationale) = ExtractSelectionData(decisionJson);

            // Select the agent by matching the name; fallback to the first agent if no match is found.
            var selectedAgent = agents.FirstOrDefault(a =>
                string.Equals(a.Name, nextAgentName, StringComparison.OrdinalIgnoreCase)) ?? agents.First();

            // Update final hints with the parsed decision details.
            streamingContent.Hints["select-decision"] = new Dictionary<string, string>
            {
                { "prompt", decisionJson },
                { "content", rationale },
                { "reason", "code" }
            };

            // Yield the final selected agent.
            yield return selectedAgent;
        }

        /// <summary>
        /// Extracts the selected agent's name and decision rationale from the JSON response.
        /// </summary>
        /// <param name="input">The JSON string containing the selection decision.</param>
        /// <returns>A tuple with the next agent's name and the rationale behind the decision.</returns>
        public static (string nextAgent, string rationale) ExtractSelectionData(string input)
        {
            // Clean up the JSON response to ensure valid formatting.
            input = YamlHelpers.CleanJsonResponse(input);

            // Extract the next agent's name.
            string nextAgent = YamlHelpers.ExtractValueByKey(input, "\"nextAgent\":");

            // Extract the rationale for the selection.
            string rationale = YamlHelpers.ExtractValueByKey(input, "\"rationale\":");

            return (nextAgent, rationale);
        }

        /// <summary>
        /// Synchronous agent selection is not implemented for this strategy.
        /// </summary>
        protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> chatHistory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

#pragma warning restore SKEXP0110
