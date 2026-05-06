using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class RagPromptRequest : SimplePromptRequest
    {
        public List<string> QueryCollections { get; set; } = new List<string>();
        public int CollectionRetrievals { get; set; } = 1;
        public Dictionary<string, object> EmbeddingFilters { get; set; } = new Dictionary<string, object>();
        public double? MinDistance { get; set; } = null;
    }
}
