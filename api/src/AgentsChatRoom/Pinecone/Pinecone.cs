using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace api.AgentsChatRoom.Pinecone
{
    public class PineconeService
    {
        private readonly string _apiKey;
        private readonly string _indexName;
        private readonly string _pineconeUrl;
        private readonly HttpClient _httpClient;

        public PineconeService(IConfiguration configuration)
        {
            _apiKey = configuration["Pinecone:ApiKey"] ?? throw new Exception("Pinecone API Key is missing");
            _indexName = configuration["Pinecone:IndexName"] ?? throw new Exception("Pinecone Index Name is missing");
            var environment = configuration["Pinecone:Environment"] ?? throw new Exception("Pinecone Environment is missing");

            _pineconeUrl = $"https://{_indexName}-{environment}.pinecone.io/vectors/upsert";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
        }

        public async Task StoreVectorAsync(string id, float[] vector, Dictionary<string, object>? metadata = null)
        {
            var requestBody = new
            {
                vectors = new[]
                {
                    new
                    {
                        id = id,
                        values = vector,
                        metadata = metadata ?? new Dictionary<string, object>()
                    }
                }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_pineconeUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error storing vector in Pinecone: {response.StatusCode}");
            }

            Console.WriteLine($"Vector {id} stored successfully in Pinecone.");
        }
    }
}
