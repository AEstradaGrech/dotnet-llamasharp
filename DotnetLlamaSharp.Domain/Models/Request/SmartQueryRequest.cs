namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SmartQueryRequest : SimpleCommandRequest
    {
        public bool WithChatCollections { get; set; } // include the catalogue of chat collections in the available collections catalogue;
        public int QueryAugments { get; set; } // generate N variants of the input to cover more similarity points;
        public int RagExpansions { get; set; } // generate a brief response without rag and use the result along the user query to create the rag
        public bool WithFewShotExpansion { get; set; }
        public int MaxFewShotExamples { get; set; }
        public bool WithLangSearch { get; set; } // use LangSearch if the query is not related to any rag collection

        public int CollectionRetrievals { get; set; }
        public int MaxCollectionChoices { get; set; }
        public float IntentConfidenceThreshold { get; set; }
    }
}
