namespace DotnetLlamaSharp.Domain.Models.Request
{
    // Esto es una gilipollez hacerlo generico --> QueryEmbeddings es una operacion propia de cada conector (o quiero buscar en chroma o quiero buscar / añadir una busqueda en Mongo o X
    // V3 -> Con ConnectorFactory si se puede rutear (depende de que coincidan los parametros de QueryEmbeddings x connector
    public class RagCommandRequest : SimpleCommandRequest
    {
        //EConnectorName = Chroma | Mongo | X
        public List<string> QueryCollections { get; set; } = new List<string>();
        public int CollectionRetrievals { get; set; } = 1;
        public Dictionary<string, object> EmbeddingFilters { get; set; } = new Dictionary<string, object>();
        public double? MaxDistance { get; set; } = null;
    }
}
