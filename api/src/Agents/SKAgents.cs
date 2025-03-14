using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;

namespace api.src.Agents
{
    public class SKAgents
    {
        private readonly Kernel _kernel;
        private readonly IVectorStore _vectorStore;

        public SKAgents(Kernel kernel, IVectorStore vectorStore)
        {
            _kernel = kernel;
            _vectorStore = vectorStore;
        }

        public async Task<MemoryRecord> ProcessMessageAsync(string message)
        {
            Console.WriteLine($"Processing message: {message}");

            // ✅ Using the actual WebSocket message instead of a hardcoded prompt
            var response = await _kernel.InvokeAsync<string>(message);

            return new MemoryRecord(
                id: Guid.NewGuid().ToString(),
                vector: new List<float> { 0.1f, 0.2f, 0.3f, 0.4f }.ToArray(), // Example vector
                metadata: new Dictionary<string, object>
                {
                    { "content", response }
                }
            );
        }

        public async Task StoreInPineconeAsync(MemoryRecord record)
        {
            await _vectorStore.UpsertAsync(new List<MemoryRecord> { record });
        }
    }
}
