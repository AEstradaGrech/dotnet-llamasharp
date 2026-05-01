using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotnetLlamaSharp.Domain.Models.Entities.Chroma
{
    public class ChromaChatCollection : ChromaChunksCollection<ChromaChatChunk> //Chunks son N last chunks de CurrentSessionId
    {
        public ChromaChatCollection(string id, string name, Dictionary<string, object> metadata): base(id, name, metadata) 
        {
            //Name = Agent-User
            //AgentName = Name.Split("-")[0]
            //UserName = Name.Split("-")[1]

            //Metadata = ChatCollectionMetadata
            // CurrentSessionId = Metadata.SessionChunkIds.Split(",").Last(); <- Esto se puede ir actualizando para recuperar sesiones previas
            // Summarization = Metadata.GlobalSumary ??
            /*
                1  Col x N ChatSessions; 1 ChatSession x N ChatChunks
               [Col agrupa todo (LONGMEMO)]
               [Ses agrupa N chatChunks] OPCION 
                                                A --> Cada X chats genero un chatChunk. si no es el primero, lo uso para hacer chromaquery con TODOs los msjs --> capturar contexto general de lo hablado
                                                      Uso el resultado para los proximos X chat y loop
                                                
                                                B --> smart rag --> x cada prompt 1 chroma query + 1 RelevanceEvaluationAnalysis.cs <- si no es relevante para el turn, ignorar
                                                C --> smart-flow --> B + get chroma LONGMEMO + get chroma SHORTMEMO (systema B) + evaluation (C.2 --> rag-collection-selector + relevanceEval
             */
        }

        public string AgentName { get; set; }
        public string UserName { get; set; }
        public new ChatCollectionMetadata DefaultMetadata { get; set; }

        protected override void setDefaultMetadata()
        {
            DefaultMetadata = JsonSerializer.Deserialize<ChatCollectionMetadata>(JsonSerializer.Serialize(Metadata));
            Description = DefaultMetadata.TEXT;
        }
    }
}
