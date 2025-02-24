 
using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110
namespace api.AgentsChatRoom.AgentRegistry
{
    /// <summary>
    /// A concrete implementation of <see cref="IAgentProfile"/>.
    /// </summary>
    public class AgentProfile(ChatCompletionAgent chatAgent,string name, string emoji) : IAgentProfile
    {
        /// <inheritdoc/>
        public ChatCompletionAgent ChatAgent { get; set; } = chatAgent;

        /// <inheritdoc/>
        public string Emoji { get; set; } = emoji;

        
        /// <inheritdoc/>
        public string Name { get; set; } = name;
    }
}

#pragma warning restore SKEXP0110