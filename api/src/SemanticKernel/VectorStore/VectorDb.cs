using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace api.src.SemanticKernel.VectorStore
{
    /// <summary>
    /// A generic class for managing a vector database backed by Azure Cognitive Search.
    /// Provides methods for adding, updating, querying, and deleting documents from the search index.
    /// </summary>
    public class VectorDB<T>
    {
        private readonly SearchIndexClient _indexClient;
        private readonly SearchIndexerClient _indexerClient;
        private readonly SearchClient _searchClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorDB{T}"/> class.
        /// Configures the Azure search client and indexer client, and creates the index if necessary.
        /// </summary>
        /// <param name="configuration">The configuration containing the Azure Search service credentials.</param>
        /// <param name="indexName">The name of the search index to interact with.</param>
        /// <param name="semanticConfigName">The name of the semantic configuration (default is "default-semantic-config").</param>
        /// <exception cref="ArgumentException">Thrown if the required Azure Search credentials are missing.</exception>
        public VectorDB(IConfiguration configuration, string indexName, string semanticConfigName = "default-semantic-config")
        {
            // Read configuration values
            var azureSearchEndpoint = configuration["AZURE_SEARCH_ENDPOINT"];
            var azureSearchKey = configuration["AZURE_SEARCH_KEY"];

            if (string.IsNullOrEmpty(azureSearchEndpoint) || string.IsNullOrEmpty(azureSearchKey))
            {
                throw new ArgumentException("Search service URI or Admin API Key is not configured.");
            }

            // Initialize clients
            _indexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), new AzureKeyCredential(azureSearchKey));
            _indexerClient = new SearchIndexerClient(new Uri(azureSearchEndpoint), new AzureKeyCredential(azureSearchKey));

            // Create index if it doesn't exist
            CreateIndex(indexName, semanticConfigName);

            // Initialize search client
            _searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, new AzureKeyCredential(azureSearchKey));
        }

        /// <summary>
        /// Creates or updates the search index with the given configuration.
        /// </summary>
        /// <param name="indexName">The name of the search index to create or update.</param>
        /// <param name="semanticConfigName">The semantic configuration name to use for the index.</param>
        private void CreateIndex(string indexName, string semanticConfigName)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(T)); // Build fields based on the type T

            var definition = new SearchIndex(indexName)
            {
                Fields = searchFields,
                SemanticSearch = new()
                {
                    Configurations =
                    {
                        new SemanticConfiguration(semanticConfigName, new()
                        {
                            TitleField = new SemanticField("Title"),
                            ContentFields =
                            {
                                new SemanticField("Description"),
                                new SemanticField("User"),
                            },
                            KeywordsFields =
                            {
                                new SemanticField("Category")
                            }
                        })
                    }
                }
            };

            try
            {
                // Check if the index already exists
                var existingIndex = _indexClient.GetIndex(indexName);
                Console.WriteLine($"Index '{indexName}' already exists. Updating the index...");
                _indexClient.CreateOrUpdateIndex(definition); // Update the existing index
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // If the index does not exist (404), create it
                Console.WriteLine($"Index '{indexName}' does not exist. Creating a new index...");
                _indexClient.CreateOrUpdateIndex(definition); // Create the new index
            }
            catch (Exception ex)
            {
                // Handle other unexpected exceptions
                Console.WriteLine($"An error occurred while creating or updating the index: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the index if it exists.
        /// </summary>
        /// <param name="indexName">The name of the index to delete.</param>
        /// <param name="indexClient">The client used to interact with the search index.</param>
        public void DeleteIndexIfExists(string indexName, SearchIndexClient indexClient)
        {
            try
            {
                if (indexClient.GetIndex(indexName) != null)
                {
                    indexClient.DeleteIndex(indexName);
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                // If exception occurred and status is "Not Found", this is expected
                Console.WriteLine("Failed to find index because it does not exist.");
            }
        }

        /// <summary>
        /// Adds or updates a batch of documents in the search index.
        /// </summary>
        /// <param name="documents">The documents to add or update.</param>
        public void AddOrUpdateDocuments(IEnumerable<T> documents)
        {
            try
            {
                // Create a batch of documents to add or update
                var actions = documents.Select(doc => IndexDocumentsAction.MergeOrUpload(doc));
                var batch = IndexDocumentsBatch.Create(actions.ToArray());

                // Perform the indexing operation
                IndexDocumentsResult result = _searchClient.IndexDocuments(batch);

                // Log success for debugging purposes
                Console.WriteLine($"Successfully indexed {documents.Count()} document(s).");
            }
            catch (RequestFailedException ex)
            {
                // Handle cases where indexing fails
                Console.WriteLine($"Failed to index some of the documents: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously deletes documents from the index based on their keys.
        /// </summary>
        /// <param name="keys">The keys of the documents to delete.</param>
        /// <param name="keyFieldName">The name of the key field in the index.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        public async Task DeleteDocumentsAsync(IEnumerable<string> keys, string keyFieldName)
        {
            if (keys == null || !keys.Any())
            {
                Console.WriteLine("No keys provided for deletion.");
                return;
            }

            try
            {
                // Create delete actions for each key
                var actions = keys.Select(key => IndexDocumentsAction.Delete(new Dictionary<string, object> { { keyFieldName, key } }));

                // Create a batch of delete actions
                var batch = IndexDocumentsBatch.Create(actions.ToArray());

                // Perform the batch deletion
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);

                // Check for partial failures
                if (result.Results.Any(r => !r.Succeeded))
                {
                    Console.WriteLine("Some documents failed to delete:");
                    foreach (var failedResult in result.Results.Where(r => !r.Succeeded))
                    {
                        Console.WriteLine($"- Key: {failedResult.Key}, Error: {failedResult.ErrorMessage}");
                    }
                }
                else
                {
                    Console.WriteLine($"{actions.Count()} document(s) deleted from the index successfully.");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Failed to delete some documents: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries documents from the index based on a filter and selected fields.
        /// </summary>
        /// <param name="filter">The filter to apply to the query.</param>
        /// <param name="fields">The fields to select from the index.</param>
        /// <param name="batchSize">The number of documents to retrieve.</param>
        /// <param name="skip">The number of documents to skip (used for pagination).</param>
        /// <returns>A list of documents matching the query.</returns>
        public List<T> QueryDocuments(string filter, List<string> fields, int batchSize, int skip)
        {
            try
            {
                var options = new SearchOptions
                {
                    Filter = filter,
                    Size = batchSize,
                    Skip = skip
                };

                foreach (var field in fields)
                {
                    options.Select.Add(field);
                }

                SearchResults<T> response = _searchClient.Search<T>("*", options);
                return response.GetResults().Select(result => result.Document).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying documents: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Asynchronously searches for documents in the index based on the provided search text.
        /// </summary>
        /// <param name="searchText">The text to search for in the index.</param>
        /// <param name="fields">The fields to search within the index.</param>
        /// <param name="batchSize">The number of documents to retrieve.</param>
        /// <param name="skip">The number of documents to skip (used for pagination).</param>
        /// <returns>A list of documents matching the search query.</returns>
        public async Task<List<T>> SearchDocuments(string searchText, List<string> fields, int batchSize, int skip)
        {
            try
            {
                SearchOptions options = new SearchOptions
                {
                    QueryType = SearchQueryType.Simple, // Use simple query syntax
                    Size = batchSize,
                    Skip = skip
                };

                foreach (var field in fields)
                {
                    options.Select.Add(field);
                }

                // Perform the search
                var response = await _searchClient.SearchAsync<T>(searchText, options);
                return response.Value.GetResults().Select(result => result.Document).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying documents: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Asynchronously searches for documents using semantic search based on the provided query.
        /// </summary>
        /// <param name="searchText">The search query text.</param>
        /// <param name="fields">The fields to be searched.</param>
        /// <param name="batchSize">The number of documents to retrieve.</param>
        /// <param name="skip">The number of documents to skip (used for pagination).</param>
        /// <returns>A list of documents along with their captions and relevance scores.</returns>
        public async Task<List<(T Document, string Caption, double Score)>> SearchSemanticDocuments(
            string searchText,
            List<string> fields,
            int batchSize,
            int skip)
        {
            try
            {
                SearchResults<T> response = await _searchClient.SearchAsync<T>(
                    searchText,
                    new SearchOptions
                    {
                        SemanticSearch = new()
                        {
                            SemanticConfigurationName = "default-semantic-config",
                            QueryCaption = new(QueryCaptionType.Extractive),
                            QueryAnswer = new(QueryAnswerType.Extractive)
                        },
                        QueryType = SearchQueryType.Semantic,
                        Size = batchSize,
                        Skip = skip
                    });

                // Select document, caption, and answer
                var results = response.GetResults().Select(result => (
                    result.Document,
                    Caption: result.SemanticSearch.Captions.FirstOrDefault()?.Text ?? "No caption available", // Provide a default if null
                    Score: result.Score ?? 0.0
                )).ToList();

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying documents: {ex.Message}");
                return new List<(T Document, string Caption, double Score)>();
            }
        }
    }
}
