namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class SmartRagSettings
    {
        public bool WithQueryAugmentation => QueryAugments > 0; // generate N variants of the input to cover more similarity points;
        public bool WithRagExpansion => RagExpansions > 0; // generate a brief response without rag and use the result along the user query to create the rag
        public bool WithLangSearch { get; set; } // use LangSearch if the query is not related to any rag collection
        public bool WithFewShotExpansion { get; set; } // if true the each generated result will be appended to the system message as an expected output examplse
        public int MaxExamples { get; set; } // will limit the number of appended examples if Augments | Expansions > 2
        public int QueryAugments { get; set; }
        public int RagExpansions { get; set; }
    }
}
