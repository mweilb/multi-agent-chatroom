using api.AgentsChatRoom.Rooms;
using api.SemanticKernel.Modifications;
using Microsoft.SemanticKernel;

namespace api.Agents.Yaml
{
    /// <summary>
    /// Handles YAML-based agent configuration and integrates it into a multi-agent chat room.
    /// </summary>
    public class YamlAgentHandler : MultiAgentChatRoom
    {
        /// <summary>
        /// The command name used by this agent, typically derived from the YAML configuration.
        /// </summary>
        public override string CommandName { get; }
        
        /// <summary>
        /// The emoji representing this agent, typically derived from the YAML configuration.
        /// </summary>
        public override string Emoji { get; }

        // Strategy for terminating streaming interactions, if configured.
        private readonly TerminationStreamingStrategy? terminationStreamingStrategy;
        
        // Strategy for selecting options during streaming interactions, if configured.
        private readonly SelectionStreamingStrategy? selectionStreamingStrategy;
        
        // Registry that loads and manages agent settings from a YAML file.
        private readonly YamlAgentRegistry agentRegistry;

        /// <summary>
        /// Initializes a new instance of the YamlAgentHandler class using the specified YAML file and kernel.
        /// </summary>
        /// <param name="yamlFilePath">Path to the YAML configuration file.</param>
        /// <param name="kernel">The Semantic Kernel instance to be used for configuring agents.</param>
        public YamlAgentHandler(string yamlFilePath, Kernel kernel)
        {
            // Create a new registry instance from the YAML file.
            agentRegistry = new YamlAgentRegistry(yamlFilePath);
            
            // Configure agents in the registry using the provided kernel.
            agentRegistry.ConfigureAgents(kernel);

            // Set the command name from the YAML configuration or default if not available.
            CommandName = agentRegistry.GetName() ?? "Name Not Available";

            // Set the agent's emoji from the YAML configuration or default to a robot emoji.
            Emoji = agentRegistry.GetEmoji() ?? "🤖";

            // Retrieve the strategy configuration from the YAML file.
            var strategiesConfiguration = agentRegistry.GetStrategiesConfig();

            if (strategiesConfiguration != null)
            {
                // Check if a termination strategy is provided.
                if (strategiesConfiguration.Termination != null)
                {
                    // Instantiate the termination strategy using the provided description, preset conditions, and filter.
                    terminationStreamingStrategy = new YamlTerminationStrategy(
                        kernel,
                        strategiesConfiguration.Termination.Description ?? "",
                        strategiesConfiguration.Termination.PresetConditions,
                        strategiesConfiguration.Termination.Filter);
                }

                // Check if a selection strategy is provided.
                if (strategiesConfiguration.Selection != null)
                {
                    // Instantiate the selection strategy using the provided description, preset conditions, and filter.
                    selectionStreamingStrategy = new YamlSelectionStrategy(
                        kernel,
                        strategiesConfiguration.Selection.Description ?? "",
                        strategiesConfiguration.Selection.PresetConditions,
                        strategiesConfiguration.Selection.Filter);
                }
            }
        }

        /// <summary>
        /// Returns the termination strategy for streaming interactions, if configured.
        /// </summary>
        public override TerminationStreamingStrategy? GetTerminationStrategy() => terminationStreamingStrategy;

        /// <summary>
        /// Returns the selection strategy for streaming interactions, if configured.
        /// </summary>
        public override SelectionStreamingStrategy? GetSelectionStrategy() => selectionStreamingStrategy;
    }
}
