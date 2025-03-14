using api.src.SemanticKernel.VectorStore.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace api.src.SemanticKernel.VectorStore
{
    public class VectorStoreHelper
    {

        public static async Task SavePdfToVectorStore(string pdfPath, Kernel kernel, string collectionName, int maxChunkLength)
        {
            // Use the chunker to split the PDF into smaller pieces.
            var chunks = PdfReader.GetParagraphs(pdfPath, maxChunkLength);

            var dataUploader = kernel.Services.GetRequiredService<DataUploader>();

            await dataUploader.GenerateEmbeddingsAndUpload(collectionName,chunks);

        }
        public static async Task SaveWordToVectorStore(string url, string path, Kernel kernel, string collectionName)
        {
            // Load the data.
            var textParagraphs = DocumentReader.ReadParagraphs(
                new FileStream(
                    path,
                    FileMode.Open),
                url);

            var dataUploader = kernel.Services.GetRequiredService<DataUploader>();
            await dataUploader.GenerateEmbeddingsAndUpload(collectionName, textParagraphs);
        }
    }
}
