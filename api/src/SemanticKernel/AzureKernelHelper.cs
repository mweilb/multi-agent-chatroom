using System;
using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace api.SemanticKernel.Helpers
{
    /// <summary>
    /// Helper class for configuring and building an Azure-based Semantic Kernel.
    /// The class manages the initialization of the kernel with Azure OpenAI services and Azure Cognitive Search.
    /// </summary>
    public class AzureKernelHelper
    {
        private readonly Kernel _kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKernelHelper"/> class.
        /// Configures the kernel with Azure OpenAI services and optional Azure Cognitive Search services.
        /// </summary>
        /// <param name="configuration">The configuration containing Azure API keys, endpoints, and deployment names.</param>
        /// <exception cref="InvalidOperationException">Thrown if any required Azure OpenAI environment variables are not set.</exception>
        public AzureKernelHelper(IConfiguration configuration)
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

            // Create the kernel builder for initializing services.
            var kernelBuilder = Kernel.CreateBuilder();

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

            // Build the kernel.
            _kernel = kernelBuilder.Build();
        }

        /// <summary>
        /// Gets the configured instance of the Semantic Kernel.
        /// </summary>
        /// <returns>The configured <see cref="Kernel"/> instance.</returns>
        public Kernel GetKernel()
        {
            return _kernel;
        }
    }
}
