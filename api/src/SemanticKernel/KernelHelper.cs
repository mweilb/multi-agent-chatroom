 
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Pinecone;
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
        }
    }
}
