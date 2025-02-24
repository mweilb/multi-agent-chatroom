using AgentOps.WebSockets;
using api.SemanticKernel.Helpers;
using api.AgentsChatRoom.Rooms;
using api.Agents.Yaml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS to allow the React app (assumes it runs on http://localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    
});

var app = builder.Build();

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Environment variables take precedence

IConfiguration configuration = configBuilder.Build();

// Initialize KernelHandler
//var kernelHandler = new AzureKernelHelper(configuration);
var kernelHandler = new OllamaKernelHelper(configuration);
var kernel = kernelHandler.GetKernel();

// Enable CORS
app.UseCors("AllowFrontend");

// Create WebSocketHandler
var webSocketHandler = new WebSocketHandler();
var agentHandlerManager = new MultiAgentChatRooms();

// Determine the base directory and the Agents directory
string baseDirectory = AppContext.BaseDirectory;
string agentsDirectory = Path.Combine(baseDirectory, "Agents");

// Get all YAML files (.yml and .yaml) in the Agents directory
var yamlFiles = Directory.GetFiles(agentsDirectory, "*.yml")
                         .Union(Directory.GetFiles(agentsDirectory, "*.yaml"));

foreach (var yamlFilePath in yamlFiles)
{
    // Create the YAML registry for the current file
    var registry = new YamlAgentRegistry(yamlFilePath);
    registry.ConfigureAgents(kernel);

    // Create the agent handler using the YAML file and the kernel
    var agentHandler = new YamlAgentHandler(yamlFilePath, kernel);

    // Register the agent chat room with the handler manager
    agentHandlerManager.AddAgentChatRoom(registry, agentHandler, kernel);
}
//can also do code version..
//agentHandlerManager.AddAgentChatRoom(new ExampleAgentRegistry(), new ExampleAgentHandler(), kernel);

// You can add more handlers here if needed.
agentHandlerManager.RegisterChatRooms(webSocketHandler, kernel);

// Configure WebSocket endpoints
webSocketHandler.ConfigureWebSocketEndpoints(app);

app.Run();

 