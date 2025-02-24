using System.Collections.Concurrent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

namespace api.AgentsChatRoom.AgentRegistry
{
    /// <summary>
    /// Default agent registry that holds a thread-safe collection of Agent objects.
    /// This registry can be extended to load and configure agent definitions.
    /// </summary>
    public class AgentRegistry : IAgentRegistry
    {
         

        // Thread-safe dictionary to store agents by their names.
        private readonly ConcurrentDictionary<string, AgentProfile> agentsDictionary = new();

        /// <summary>
        /// Configures agents for the given kernel. Override this method in derived classes
        /// to add or reinitialize agent definitions.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance to configure agents for.</param>
        public virtual void ConfigureAgents(Kernel kernel)
        {
            // Example usage: add or configure multiple agents here.
            // For example:
            // AddAgent(kernel, "assistant", "You are a helpful AI assistant.", "😀");
        }

        /// <summary>
        /// Returns all registered ChatCompletionAgent instances as a read-only collection.
        /// </summary>
        /// <returns>A read-only collection of ChatCompletionAgent objects.</returns>
        public IReadOnlyCollection<IAgentProfile> GetAllAgents()
        {
            // Extract the ChatCompletionAgent from each stored Agent.
            return agentsDictionary.Values.Select(agent => agent).ToList().AsReadOnly();
        }


        /// <summary>
        /// Adds an agent to the registry with the specified name, instructions, and emoji.
        /// Derived classes can use this method to add new agents.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance to associate with the agent.</param>
        /// <param name="agentName">The name identifier for the agent.</param>
        /// <param name="agentInstructions">The instructions or prompt associated with the agent.</param>
        /// <param name="agentEmoji">The emoji representing the agent.</param>
        protected void AddAgent(Kernel kernel, string agentName, string agentInstructions, string agentEmoji)
        {
            // Create a new ChatCompletionAgent instance with the provided properties.
            var newChatAgent = new ChatCompletionAgent
            {
                Name = agentName,
                Instructions = agentInstructions,
                Kernel = kernel
            };

            // Create the internal Agent instance to hold both the ChatCompletionAgent and the emoji.
            var agent = new AgentProfile(newChatAgent,agentName, agentEmoji);

            // Add or update the agent in the dictionary.
            agentsDictionary[agentName] = agent;
        }

        /// <summary>
        /// Retrieves an agent from the registry by its name.
        /// </summary>
        /// <param name="agentName">The name of the agent to retrieve.</param>
        /// <returns>The IAgent if found; otherwise, null.</returns>
        public IAgentProfile? GetAgent(string agentName)
        {
            if (agentsDictionary.TryGetValue(agentName, out AgentProfile? storedAgent))
            {
                return storedAgent;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the emoji associated with the specified agent name.
        /// </summary>
        /// <param name="agentName">The name of the agent.</param>
        /// <returns>The emoji if found; otherwise, an empty string.</returns>
        public string GetAgentEmoji(string agentName)
        {
            if (agentsDictionary.TryGetValue(agentName, out AgentProfile? storedAgent))
            {
                return storedAgent.Emoji;
            }
            return string.Empty;
        }
 
    }
}

#pragma warning restore SKEXP0110
