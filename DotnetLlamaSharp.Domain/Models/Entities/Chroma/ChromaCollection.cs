using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaCollection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
