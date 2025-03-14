using AgentOps.WebSockets;
using api.Agents.Yaml;
using api.AgentsChatRoom.Rooms;
using api.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;

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


// Create the kernel builder for initializing services.
var kernelBuilder = Kernel.CreateBuilder();

// Initialize KernelHandler
//KernelHelper.SetupAzure(kernelBuilder, configuration);
KernelHelper.SetupOllama(kernelBuilder, configuration);

KernelHelper.SetupQdrant(kernelBuilder, configuration);
//KernelHelper.SetupPinecone(kernelBuilder, configuration);
//KernelHelper.SetupAzureSearch(kernelBuilder, configuration);
// Build the kernel.
var kernel = kernelBuilder.Build();

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

    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

    // Register the agent chat room with the handler manager
    agentHandlerManager.AddAgentChatRoom(registry, agentHandler, kernel, loggerFactory);
}
//can also do code version..
//agentHandlerManager.AddAgentChatRoom(new ExampleAgentRegistry(), new ExampleAgentHandler(), kernel);

// You can add more handlers here if needed.
agentHandlerManager.RegisterChatRooms(webSocketHandler, kernel);

app.UseWebSockets();

// Map the "/ws" endpoint for WebSocket connections.
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("WebSocket connection established");
        await webSocketHandler.HandleRequestAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
 
 
app.Run();

 