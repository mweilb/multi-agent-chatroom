using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;
using api.src.SemanticKernel.VectorStore;

internal static class PdfReader
{
    /// <summary>
    /// Reads a PDF document, splits its text into chunks of up to maxChunkLength characters,
    /// and yields each chunk as a TextParagraph.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="maxChunkLength">Maximum number of characters per chunk.</param>
    /// <returns>An enumerable of TextParagraph objects.</returns>
    public static IEnumerable<TextParagraph> GetParagraphs(string pdfPath, int maxChunkLength)
    {
        // Extract the full text from the PDF.
        var fullText = new StringBuilder();
        using (var document = PdfDocument.Open(pdfPath))
        {
            foreach (var page in document.GetPages())
            {
                fullText.AppendLine(page.Text);
            }
        }

        // Split the text into paragraphs using newlines.
        string[] paragraphs = fullText.ToString()
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Combine paragraphs into chunks of up to maxChunkLength characters.
        StringBuilder currentChunk = new StringBuilder();
        int chunkCounter = 0;
        foreach (var paragraph in paragraphs)
        {
            // Check if adding the current paragraph would exceed the maximum chunk length.
            if (currentChunk.Length + paragraph.Length > maxChunkLength)
            {
                if (currentChunk.Length > 0)
                {
                    var chunkText = currentChunk.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(chunkText))
                    {
                        Console.WriteLine("Found PDF chunk:");
                        Console.WriteLine(chunkText);
                        Console.WriteLine();
                        yield return new TextParagraph
                        {
                            Key = Guid.NewGuid().ToString(),
                            DocumentUri = pdfPath,
                            ParagraphId = $"chunk_{chunkCounter++}",
                            Text = chunkText
                        };
                    }
                    currentChunk.Clear();
                }
            }

            // Append a newline if the current chunk already has content to maintain separation.
            if (currentChunk.Length > 0)
            {
                currentChunk.AppendLine();
            }
            currentChunk.Append(paragraph);
        }

        // Yield any remaining text as the final chunk.
        if (currentChunk.Length > 0)
        {
            var chunkText = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                Console.WriteLine("Found PDF chunk:");
                Console.WriteLine(chunkText);
                Console.WriteLine();
                yield return new TextParagraph
                {
                    Key = Guid.NewGuid().ToString(),
                    DocumentUri = pdfPath,
                    ParagraphId = $"chunk_{chunkCounter++}",
                    Text = chunkText
                };
            }
        }
    }
}
