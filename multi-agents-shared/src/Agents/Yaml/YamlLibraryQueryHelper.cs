using api.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;
 

#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

namespace api.Agents.Yaml
{
    /// <summary>
    /// Provides helper methods for querying a library by applying reframing and filtering
    /// to the conversation history.
    /// </summary>
    public static class YamlLibraryQueryHelper
    {
        /// <summary>
        /// Uses the library's filtering and reframing settings to process recent conversation history.
        /// First, it applies a filter to extract a relevant conversation snippet, yielding intermediate results.
        /// Then, it uses the library's reframing text to obtain a final reframed response.
        /// </summary>
        /// <param name="libraryConfig">The library configuration containing reframing and filtering values.</param>
        /// <param name="chatHistory">The conversation history as a list of messages.</param>
        /// <param name="kernel">The kernel used to process prompts.</param>
        /// <param name="cancellationToken">Cancellation token for asynchronous operations.</param>
        /// <returns>
        /// An asynchronous stream of tuples containing:
        ///   - Response: the filtered or final reframed response,
        ///   - FilteredThinking: any thinking text from the filtering step,
        ///   - ReframedThinking: any thinking text from the reframing step.
        /// </returns>
        public static async IAsyncEnumerable<(string Response, string FilteredThinking, string ReframedThinking)> GetReframedQueryAsync(
            LibraryConfig libraryConfig,
            IReadOnlyList<ChatMessageContent> chatHistory,
            Kernel kernel,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // STEP 1: Apply the filter prompt from the library configuration to extract a conversation snippet.
            await foreach (var (ignoredPrompt, filteredJson, filteredThinking) in YamlHistory.GetFilteredHistoryResultAsync(
                preconditionPrompts: null,
                filterInstruction: libraryConfig.Filter ?? "",
                chatHistory: chatHistory,
                kernel: kernel,
                cancellationToken: cancellationToken))
            {
                // Yield intermediate filtering result with the filtered JSON and its thinking text.
                yield return (filteredJson, filteredThinking, string.Empty);
            }

            // STEP 2: Build a new prompt using the library's reframing text and the full conversation history.
            string combinedHistory = string.Join("\n", chatHistory);
            string reframingPrompt = BuildPrompt(libraryConfig.Reframing, combinedHistory);

            // Invoke the reframing prompt using the kernel.
            var promptResult = await kernel.InvokePromptAsync(reframingPrompt, null, null, null, cancellationToken);
            string rawResponse = promptResult?.GetValue<string>() ?? string.Empty;

            // Split the raw response into the reframed result and any additional thinking text.
            (string reframedResult, string reframedThinking) = OllamaHelper.SplitContentFromThinking(rawResponse);

            // Yield the final reframed response.
            yield return (reframedResult, string.Empty, reframedThinking);
        }

        /// <summary>
        /// Constructs the prompt for the kernel by combining the reframing text with the conversation history.
        /// </summary>
        /// <param name="reframingText">The reframing text from the library configuration.</param>
        /// <param name="historyContent">A string representation of the conversation history.</param>
        /// <returns>A formatted prompt string to be sent to the kernel.</returns>
        private static string BuildPrompt(string? reframingText, string historyContent)
        {
            return $"{reframingText ?? ""}\n\nContent:\n{historyContent}\n\nAction: {reframingText ?? ""}";
        }
    }
}

#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0110
