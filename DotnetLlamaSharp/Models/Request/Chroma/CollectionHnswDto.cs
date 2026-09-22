namespace DotnetLlamaSharp.Models.Request.Chroma
{
    public class CollectionHnswDto
    {

        public string Space { get; set; }
        public int EfConstruction { get; set; }
        public int MaxNeighbors { get; set; }
        public int EfSearch { get; set; }
        public int NumThreads { get; set; }
    }
}
