using Microsoft.SemanticKernel;

namespace api.SemanticKernel.Helpers
{
    /// <summary>
    /// Helper class for configuring and building an Ollama-based Semantic Kernel.
    /// It initializes the kernel with the Ollama Chat Completion service.
    /// </summary>
    public class OllamaKernelHelper
    {
        private readonly Kernel _kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaKernelHelper"/> class.
        /// Configures the kernel with Ollama Chat Completion service using configuration settings.
        /// </summary>
        /// <param name="configuration">The configuration containing Ollama endpoint and model details.</param>
        public OllamaKernelHelper(IConfiguration configuration)
        {
            // Load Ollama endpoint and model from configuration, with fallback defaults.
            var ollamaEndpoint = configuration["OLLAMA_ENDPOINT"] ?? "http://localhost:11434";
            var modelId = configuration["OLLAMA_MODEL"] ?? "deepseek-r1";

            // Create the kernel builder.
            var kernelBuilder = Kernel.CreateBuilder();
            var ollamaUri = new Uri(ollamaEndpoint);

            // Add Ollama Chat Completion service to the kernel.
#pragma warning disable SKEXP0070
            kernelBuilder.AddOllamaChatCompletion(modelId, ollamaUri);
#pragma warning restore SKEXP0070

            // Build the kernel.
            _kernel = kernelBuilder.Build();
        }

        /// <summary>
        /// Removes any text between <think> and </think> (including the tags) from the provided content.
        /// </summary>
        /// <param name="content">The content from which <think> tags will be removed.</param>
        /// <returns>The content with <think> tags and their content removed.</returns>
        public static string RemoveThinkContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            const string startTag = "<think>";
            const string endTag = "</think>";

            int startIndex = content.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return content;

            int endIndex = content.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex >= 0)
            {
                // Extract parts before and after the <think> tag.
                string beforeThink = content.Substring(0, startIndex);
                string afterThink = content.Substring(endIndex + endTag.Length);
                return beforeThink + afterThink;
            }
            else
            {
                // If no closing </think> tag is found, remove content from <think> to the end.
                return content.Substring(0, startIndex);
            }
        }

        /// <summary>
        /// Splits the input text into plain text and "thinking" content (the content within <think> tags).
        /// </summary>
        /// <param name="input">The input content to be split.</param>
        /// <returns>A tuple containing the plain text and the "thinking" content.</returns>
        public static (string plainText, string thinkText) SplitContentFromThinking(string input)
        {
            const string startTag = "<think>";
            const string endTag = "</think>";

            int startIndex = input.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
            {
                // No <think> tags found, return the input as plain text and an empty "think" section.
                return (input, string.Empty);
            }

            int endIndex = input.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex >= 0)
            {
                // Extract plain text and thinking content.
                string beforeThink = input.Substring(0, startIndex);
                string thinkSection = input.Substring(startIndex, endIndex - startIndex + endTag.Length);
                string afterThink = input.Substring(endIndex + endTag.Length);
                return (beforeThink + afterThink, thinkSection);
            }
            else
            {
                // If no closing </think> tag is found, return everything before <think> as plain text,
                // and the remaining content as thinking content.
                string beforeThink = input.Substring(0, startIndex);
                string thinkSection = input.Substring(startIndex);
                return (beforeThink, thinkSection);
            }
        }

        /// <summary>
        /// Gets the configured instance of the Semantic Kernel.
        /// </summary>
        /// <returns>The configured <see cref="Kernel"/> instance.</returns>
        public Kernel GetKernel()
        {
            return _kernel;
        }
    }
}
