#pragma warning disable CS1591, CS0618, SKEXP0020, CS0117, CS1503, CS1061

using AgentOps.WebSockets;
using api.SemanticKernel.Helpers;
using api.AgentsChatRoom.Rooms;
using api.Agents.Yaml;
using api.src.Websockets;
using api.src.Agents; // ? Corrected namespace (without SKAgents)
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pinecone;

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

// Initialize Pinecone Client in DI
builder.Services.AddSingleton<PineconeClient>(sp =>
    new PineconeClient("pcsk_3Urp9A_BqEfvf3aUXWM3754EuUxBZEMPQ8t3rtJ2B6VaoEeva8SkK2P3gSZ2VT4Pq8eMNS", new Uri("https://us-east-1.pinecone.io")));

// Initialize Pinecone Vector Store
builder.Services.AddPineconeVectorStore();

// Add Semantic Kernel
builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.TryAddEnumerable(sp.GetServices<ServiceDescriptor>()); // Fixed Read-Only Services Issue
    return kernelBuilder.Build();
});

// ? Register SKAgents as a service (Correct class reference)
builder.Services.AddSingleton<SKAgents>();

var app = builder.Build();

// Load Configuration
var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Environment variables take precedence

IConfiguration configuration = configBuilder.Build();

// Initialize Kernel Handler
var kernelHandler = new OllamaKernelHelper(configuration);
var kernel = kernelHandler.GetKernel();

// Enable CORS
app.UseCors("AllowFrontend");

// Resolve Pinecone Vector Store & SKAgents
var vectorStore = app.Services.GetRequiredService<IVectorStore>();
var skAgent = app.Services.GetRequiredService<SKAgents>(); // ? Now correctly resolving SKAgents class

// Create WebSocket Handler & Multi-Agent Manager
var webSocketHandler = new WebSocketHandler();
var agentHandlerManager = new MultiAgentChatRooms();

// Load Agents from YAML
string baseDirectory = AppContext.BaseDirectory;
string agentsDirectory = Path.Combine(baseDirectory, "Agents");

// Get all YAML files (.yml and .yaml) in the Agents directory
var yamlFiles = Directory.GetFiles(agentsDirectory, "*.yml")
                         .Union(Directory.GetFiles(agentsDirectory, "*.yaml"));

foreach (var yamlFilePath in yamlFiles)
{
    // Create YAML Agent Registry
    var registry = new YamlAgentRegistry(yamlFilePath);
    registry.ConfigureAgents(kernel);

    // Initialize Agent Handler with Kernel
    var agentHandler = new YamlAgentHandler(yamlFilePath, kernel);

    // Register Agent Chat Room
    agentHandlerManager.AddAgentChatRoom(registry, agentHandler, kernel);
}

// ? Add WebSocket Listener with SKAgents
var listener = new WebSocketListener(webSocketHandler, skAgent);

// Register Chat Rooms & Configure WebSocket Endpoints
agentHandlerManager.RegisterChatRooms(webSocketHandler, kernel);
webSocketHandler.ConfigureWebSocketEndpoints(app);

// Run the Application
app.Run();

#pragma warning restore CS1591, CS0618, SKEXP0020, CS0117, CS1503, CS1061
