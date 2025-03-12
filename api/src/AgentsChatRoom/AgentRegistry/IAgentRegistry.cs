using Microsoft.SemanticKernel;

namespace api.AgentsChatRoom.AgentRegistry
{
    /// <summary>
    /// Defines methods for storing, adding, and retrieving agent definitions.
    /// Implementations can be extended with additional operations such as update or removal.
    /// </summary>
    public interface IAgentRegistry
    {


        /// <summary>
        /// Configures the agent definitions using the provided kernel instance.
        /// Implementations should load or initialize agent definitions as needed.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance used to configure agents.</param>
        void ConfigureAgents(Kernel kernel);

        /// <summary>
        /// Retrieves all registered agents as a read-only collection.
        /// </summary>
        /// <returns>A read-only collection of <see cref="IAgentProfile"/> instances.</returns>
        IReadOnlyCollection<IAgentProfile> GetAllAgents();
    }
}

 