
using YamlDotNet.Serialization;

namespace api.Agents.Yaml
{
    /// <summary>
    /// Represents the root configuration loaded from a YAML file.
    /// Contains general properties like name, emoji, agents, and strategy settings.
    /// </summary>
    public class YamlConfig
    {
        /// <summary>
        /// The name of the overall configuration or application.
        /// </summary>
        [YamlMember(Alias = "name")]
        public string? Name { get; set; }

        /// <summary>
        /// The default emoji associated with the configuration.
        /// </summary>
        [YamlMember(Alias = "emoji")]
        public string? Emoji { get; set; }
        
        /// <summary>
        /// A collection of agent configurations identified by their names.
        /// </summary>
        [YamlMember(Alias = "agents")]
        public Dictionary<string, AgentConfig>? Agents { get; set; }
        
        /// <summary>
        /// Configuration settings for different strategies (e.g., termination, selection).
        /// </summary>
        [YamlMember(Alias = "strategies")]
        public StrategiesConfig? Strategies { get; set; }
    }

    /// <summary>
    /// Represents the configuration settings for an individual agent.
    /// </summary>
    public class AgentConfig
    {
        /// <summary>
        /// The instructions or prompt for the agent.
        /// </summary>
        [YamlMember(Alias = "instructions")]
        public string? Instructions { get; set; }

        /// <summary>
        /// Optional emoji specific to this agent.
        /// </summary>
        [YamlMember(Alias = "emoji")]
        public string? Emoji { get; set; }
        
        /// <summary>
        /// Optional collection of libraries to be associated with the agent.
        /// </summary>
        [YamlMember(Alias = "libraries")]
        public Dictionary<string, LibraryConfig>? Libraries { get; set; }
    }

    /// <summary>
    /// Represents the configuration for a library that can be attached to an agent.
    /// </summary>
    public class LibraryConfig
    {
        /// <summary>
        /// The reframing text used by the library (formerly known as "prompt").
        /// </summary>
        [YamlMember(Alias = "reframing")]
        public string? Reframing { get; set; }

        /// <summary>
        /// A list of preset conditions applicable to the library.
        /// </summary>
        [YamlMember(Alias = "preset-conditions")]
        public List<string>? PresetConditions { get; set; }
        
        /// <summary>
        /// A filter string to refine the library's functionality.
        /// </summary>
        [YamlMember(Alias = "filter")]
        public string? Filter { get; set; }
    }

    /// <summary>
    /// Contains configuration settings for different strategies, such as termination and selection.
    /// </summary>
    public class StrategiesConfig
    {
        /// <summary>
        /// Configuration for the termination strategy.
        /// </summary>
        [YamlMember(Alias = "termination")]
        public StrategyConfig? Termination { get; set; }
        
        /// <summary>
        /// Configuration for the selection strategy.
        /// </summary>
        [YamlMember(Alias = "selection")]
        public StrategyConfig? Selection { get; set; }
    }

    /// <summary>
    /// Represents a generic strategy configuration used for various purposes.
    /// </summary>
    public class StrategyConfig
    {
        /// <summary>
        /// A description of the strategy.
        /// </summary>
        [YamlMember(Alias = "description")]
        public string? Description { get; set; }
        
        /// <summary>
        /// A list of preset conditions that define the strategy's behavior.
        /// </summary>
        [YamlMember(Alias = "preset-conditions")]
        public List<string>? PresetConditions { get; set; }
        
        /// <summary>
        /// A filter to customize or constrain the strategy.
        /// </summary>
        [YamlMember(Alias = "filter")]
        public string? Filter { get; set; }
    }
}
