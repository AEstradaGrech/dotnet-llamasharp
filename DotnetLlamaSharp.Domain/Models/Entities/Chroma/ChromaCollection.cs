using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaCollection
    {
        public string Name { get; set; }
        public Dictionary<string, string> MetadataTags { get; set; }
    }
}
