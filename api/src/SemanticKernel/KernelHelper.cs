
using api.src.SemanticKernel.VectorStore.Documents;
using Azure;
using Azure.Core;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using static OllamaSharp.OllamaApiClient;

namespace api.SemanticKernel.Helpers
{
    /// <summary>
    /// Helper class for configuring and building an Azure-based Semantic Kernel.
    /// The class manages the initialization of the kernel with Azure OpenAI services and Azure Cognitive Search.
    /// </summary>
    public class KernelHelper  
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KernelHelper"/> class.
        /// Configures the kernel with Azure OpenAI services and optional Azure Cognitive Search services.
        /// </summary>
        /// <param name="configuration">The configuration containing Azure API keys, endpoints, and deployment names.</param>
        /// <exception cref="InvalidOperationException">Thrown if any required Azure OpenAI environment variables are not set.</exception>
        static public void SetupAzure(IKernelBuilder kernelBuilder, IConfiguration configuration)
        {
            // Load API keys, endpoints, and deployment names from configuration.
            var apiKey = configuration["AZURE_OPENAI_API_KEY"];
            var endpoint = configuration["AZURE_OPENAI_ENDPOINT"];
            var deploymentName = configuration["AZURE_OPENAI_DEPLOYMENT"];

            var azureSearchEndpoint = configuration["AZURE_SEARCH_ENDPOINT"];
            var azureSearchKey = configuration["AZURE_SEARCH_KEY"];

            // Validate required configurations.
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException("Azure OpenAI environment variables are not set.");
            }

            // Add Azure OpenAI Chat Completion service to the kernel.
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                apiKey: apiKey,
                endpoint: endpoint
            );

            // If Azure Cognitive Search configuration is provided, add the services.
            if (!(string.IsNullOrEmpty(azureSearchEndpoint) || string.IsNullOrEmpty(azureSearchKey)))
            {
                #pragma warning disable SKEXP0010
                // Add Azure OpenAI Text Embedding Generation service to the kernel.
                kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey
                );
                #pragma warning restore SKEXP0010

                // Add Azure Cognitive Search vector store if provided.
                kernelBuilder.AddAzureAISearchVectorStore(
                    new Uri(azureSearchEndpoint),
                    new Azure.AzureKeyCredential(azureSearchKey)
                );
            }

          
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaHelper"/> class.
        /// Configures the kernel with Ollama Chat Completion service using configuration settings.
        /// </summary>
        /// <param name="configuration">The configuration containing Ollama endpoint and model details.</param>
        static public void SetupOllama(IKernelBuilder kernelBuilder, IConfiguration configuration)
        {
            // Load Ollama endpoint and model from configuration, with fallback defaults.
            var ollamaEndpoint = configuration["OLLAMA_ENDPOINT"] ?? "http://localhost:11434";
            var modelId = configuration["OLLAMA_MODEL"] ?? "deepseek-r1";

            // Create the kernel builder.
            var ollamaUri = new Uri(ollamaEndpoint);

            // Add Ollama Chat Completion service to the kernel.
#pragma warning disable SKEXP0070
            kernelBuilder.AddOllamaChatCompletion(modelId, ollamaUri);
#pragma warning restore SKEXP0070
        }

        static public void SetupPinecone(IKernelBuilder builder, IConfiguration configuration)
        {
            var pineconeApiKey = configuration["PINECONE_API_KEY"] ?? "your-pinecone-api-key";
   
            // Create a Pinecone client and memory store.
            builder.AddPineconeVectorStore(pineconeApiKey);

            // Register the data uploader.
            builder.Services.AddSingleton<DataUploader>();
        }

        /// <summary>
        /// Configures the kernel with Qdrant as the memory store.
        /// </summary>
        /// <param name="builder">The kernel builder.</param>
        /// <param name="configuration">The configuration containing Qdrant settings.</param>
        public static void SetupQdrant(IKernelBuilder builder, IConfiguration configuration)
        {
            // Load Qdrant settings from configuration with sensible defaults.
            var qdrantEndpoint = configuration["QDRANT_ENDPOINT"] ?? "http://localhost:6335";

            // Parse the endpoint to extract host and port.
            var uri = new Uri(qdrantEndpoint);
            string host = uri.Host;
            int port = uri.Port;

            // Add the Qdrant memory store to the kernel.
            // This method expects the Qdrant endpoint as a Uri and the index name as a string.
            builder.AddQdrantVectorStore(host,port);

            // Register the data uploader.
            builder.Services.AddSingleton<DataUploader>();
        }

        public static void SetupAzureSearch(IKernelBuilder builder, IConfiguration configuration)
        {
            // Load Azure Search settings from configuration.
            var azureSearchEndpoint = configuration["AZURE_SEARCH_ENDPOINT"] ?? "https://your-search-service.search.windows.net";
            var azureSearchKey = configuration["AZURE_SEARCH_KEY"] ?? "your-azure-search-key";
            var indexName = configuration["AZURE_SEARCH_INDEX"] ?? "pdf-docs";

            // Create a TokenCredential (using DefaultAzureCredential as an example).
            var tokenCredential = new AzureKeyCredential(azureSearchKey);

            // Add Azure Cognitive Search vector store using the TokenCredential.

            // Configure Azure Cognitive Search vector store.
            builder.AddAzureAISearchVectorStore(new Uri(azureSearchEndpoint), tokenCredential);

            // Register the data uploader.
            builder.Services.AddSingleton<DataUploader>();
        }
    }
}
