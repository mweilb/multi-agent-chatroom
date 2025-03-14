﻿using Microsoft.Extensions.VectorData;

namespace api.src.SemanticKernel.VectorStore
{
    internal class TextParagraph
    {
        /// <summary>A unique key for the text paragraph.</summary>
        [VectorStoreRecordKey]
        public required string Key { get; init; }

        /// <summary>A uri that points at the original location of the document containing the text.</summary>
        [VectorStoreRecordData]
        public required string DocumentUri { get; init; }

        /// <summary>The id of the paragraph from the document containing the text.</summary>
        [VectorStoreRecordData]
        public required string ParagraphId { get; init; }

        /// <summary>The text of the paragraph.</summary>
        [VectorStoreRecordData]
        public required string Text { get; init; }

        /// <summary>The embedding generated from the Text.</summary>
        [VectorStoreRecordVector(1536)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}