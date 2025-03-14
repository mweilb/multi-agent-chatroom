
using YamlDotNet.Serialization;
using Microsoft.SemanticKernel;
using api.AgentsChatRoom.AgentRegistry;

namespace api.Agents.Yaml
{
    /// <summary>
    /// Loads agent configurations from a YAML file and registers them with the Semantic Kernel.
    /// </summary>
    public class YamlAgentRegistry : AgentRegistry
    {
        // Holds the deserialized YAML configuration.
        private readonly YamlConfig yamlConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlAgentRegistry"/> class using the provided YAML file path.
        /// </summary>
        /// <param name="yamlFilePath">Path to the YAML configuration file.</param>
        /// <exception cref="InvalidOperationException">Thrown if the YAML configuration cannot be parsed.</exception>
        public YamlAgentRegistry(string yamlFilePath)
        {
            using var reader = new StreamReader(yamlFilePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            // Deserialize the YAML file into a configuration object.
            yamlConfig = deserializer.Deserialize<YamlConfig>(reader);

            // Ensure the configuration was loaded successfully.
            if (yamlConfig == null)
            {
                throw new InvalidOperationException($"Failed to parse YAML configuration from file: {yamlFilePath}");
            }
        }

        /// <summary>
        /// Configures agents in the Semantic Kernel using the YAML configuration.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance to configure agents for.</param>
        /// <exception cref="InvalidOperationException">Thrown if no agents are defined in the configuration.</exception>
        public override void ConfigureAgents(Kernel kernel)
        {
            if (yamlConfig.Agents == null)
            {
                throw new InvalidOperationException("No agents found in the configuration.");
            }

            // Iterate over each agent entry defined in the YAML configuration.
            foreach (var agentEntry in yamlConfig.Agents)
            {
                string agentName = agentEntry.Key;

                // Skip if the agent configuration or its instructions are missing.
                if (agentEntry.Value == null || agentEntry.Value.Instructions == null)
                {
                    continue;
                }

                // Register the agent with the kernel using its name and instructions.
                AddAgent(kernel, agentName, agentEntry.Value.Instructions, agentEntry.Value.Emoji?? "🤖");

                // If the agent has libraries defined, register them.
                if (agentEntry.Value.Libraries != null)
                {
                    var agent = GetAgent(agentName); // Assumes GetAgent() retrieves the agent instance.
                    foreach (var libraryEntry in agentEntry.Value.Libraries)
                    {
                        string libraryName = libraryEntry.Key;
                        LibraryConfig libraryConfig = libraryEntry.Value;

                        // Output the library registration details for demonstration.
                        Console.WriteLine($"Registering library '{libraryName}' for agent '{agentName}':");
                        Console.WriteLine($"  Reframing: {libraryConfig.Reframing}");
                        Console.WriteLine($"  Filter: {libraryConfig.Filter}");

                        // In a real implementation, attach the library to the agent's vector lookup system.
                        // e.g., agent.AttachLibrary(libraryName, libraryConfig.Reframing, libraryConfig.Filter);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the strategy configuration from the YAML file.
        /// </summary>
        /// <returns>The strategies configuration if available; otherwise, null.</returns>
        public StrategiesConfig? GetStrategiesConfig() => yamlConfig.Strategies;

        /// <summary>
        /// Retrieves the agent's name from the YAML configuration.
        /// </summary>
        /// <returns>The agent name if available; otherwise, null.</returns>
        public string? GetName() => yamlConfig.Name;

        /// <summary>
        /// Retrieves the agent's emoji from the YAML configuration.
        /// </summary>
        /// <returns>The agent emoji if available; otherwise, null.</returns>
        public string? GetEmoji() => yamlConfig.Emoji;

        /// <summary>
        /// Retrieves the configuration for a specific agent by name.
        /// </summary>
        /// <param name="agentName">The name of the agent.</param>
        /// <returns>The agent's configuration if found; otherwise, null.</returns>
        public AgentConfig? GetConfigForAgent(string agentName)
        {
            if (yamlConfig.Agents != null && yamlConfig.Agents.TryGetValue(agentName, out AgentConfig? agentConfig))
            {
                return agentConfig;
            }
            return null;
        }
    }
}
