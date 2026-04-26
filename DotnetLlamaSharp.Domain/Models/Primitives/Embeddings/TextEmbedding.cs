using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Embeddings
{
    public class TextEmbedding
    {
        public TextEmbedding(string text, ReadOnlyMemory<float> embeddings, int dimensions)
        {
            Text = text;
            Vector = embeddings;
            Dimensions = dimensions;
        }
        public string Text { get; set; }
        public ReadOnlyMemory<float> Vector { get; set; }
        public int Dimensions { get; set; }
    }
}
