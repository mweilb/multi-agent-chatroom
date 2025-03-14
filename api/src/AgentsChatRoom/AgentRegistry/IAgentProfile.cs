using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110
 

namespace api.AgentsChatRoom.AgentRegistry
{
    /// <summary>
    /// Represents the contract for an agent in the system.
    /// </summary>
    public interface IAgentProfile
    {
        /// <summary>
        /// The ChatCompletionAgent instance.
        /// </summary>
        ChatCompletionAgent ChatAgent { get; set; }


        /// <summary>
        /// The name of the agent.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The emoji representing the agent.
        /// </summary>
        string Emoji { get; set; }
    }
}
 
#pragma warning restore SKEXP0110