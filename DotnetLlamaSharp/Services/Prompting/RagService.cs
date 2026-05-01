using AutoMapper;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting;
using System.Text;

namespace DotnetLlamaSharp.Services.Prompting
{
    public class RagService : IRagService
    {
        private readonly IOllamaSharpService _chatService;
        private readonly IChromaService _chromaService;
        private readonly ILogger<RagService> _logger;
        private readonly IMapper _mapper;

        public RagService(ILogger<RagService> logger, IMapper mapper, IOllamaSharpService chatService, IChromaService chromaService)
        {
            _chatService = chatService;
            _chromaService = chromaService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<RagPrompt> SimpleRagQuery(RagPromptRequest request)
        {
            ChromaQuery chromaQuery = null;
            var chunksCollection = new Dictionary<string, string>();
            var includedChunks = new List<ChromaQueryChunk>();
            if(request.QueryCollections.Count > 0)
            {
                foreach(var name in request.QueryCollections)
                {
                    chromaQuery = await _chromaService.QueryCollection(name, request.Prompt, request.CollectionRetrievals, request.EmbeddingFilters);

                    foreach(var chunk in chromaQuery.Chunks)
                    {
                        if (request.MinDistance != null && chunk.Distance > request.MinDistance) break;
                        
                        includedChunks.Add(chunk);
                        chunksCollection.Add(chunk.Id, name);
                    };
                }
            }

            var sb = new StringBuilder();

            includedChunks
                .OrderBy(chunk => chunk.Distance)
                .ToList()
                .ForEach(chunk =>
                    sb.Append($"\n- Topic: ")
                      .Append(chunksCollection[chunk.Id])
                      .Append("\n- Source document: ")
                      .Append(chunk.DefaultMetadata.DOCUMENT)
                      .Append("\n- Data:\n\n")
                      .Append(chunk.DefaultMetadata.TEXT)
                      .Append("\n"));

            var baseIntruction = request.SystemMessage.Trim();

            var systemMessage = @$"{(string.IsNullOrEmpty(baseIntruction) ? "You are a helpful assistant." : baseIntruction)}
            Your task is to answer any user query using the RETRIEVED_DATA as a grounded source of truth.
            
            Each item in the retrieve data section has three sections that will help you to understand the source and topic
            of the data along with the relevant document data that you must use to generate your response.

            Use the retrieved information to answer the user questions only if the question is related to
            the retrieved data topic.

            ### IMPORTANT: follow this STEPS in order to generate your response:

            # STEP 1: Analyze the content of the user query to get an idea of the topic he is querying about.
            # STEP 2: Review the RETRIEVED DATA section and try to find relevant data to use as source to generate your response (the 'Topic' and 'Source' values of 
                      each data item will help you to do that).
            # STEP 3: Use your reasonaments from STEP 1 and STEP 2 to decide if the RETRIEVED DATA is meaningful in order to generate your response according to
                      the queried topic.
            # STEP 4: Based on your analysis from STEP 3 generate a coherent response using the RETRIEVED DATA to support your answer if you have decided that it
                      is aligned with the general topic of the user question. In case you have decided that the user query is unrelated to the retrived data, 
                      ignore the RETRIEVED DATA and try to answer the anyways but always informing the user that you have not found any relevant data in your Knowledge Base about the queried topic.

            ### RETRIEVED DATA:

            {sb.ToString().Trim()}";
        
            var chatRequest = _mapper.Map<RagPromptRequest, ChatPromptRequest>(request);
            chatRequest.SystemMessage = systemMessage;
      
            var response = await _chatService.SimplePrompt(chatRequest);
            
            return new RagPrompt
            {
                Model = response.Model,
                EmbeddingModel = chromaQuery != null ? chromaQuery.EmbeddingModel : null,
                Input = request.Prompt,
                Output = response.Output,
                IncludedChunks = includedChunks,
                ChatHistory = response.ChatHistory,
                InputEmbedding = chromaQuery != null ? chromaQuery.QueryEmbedding : null
            };
        }

        public async Task<RagPrompt> RagChatPropt(RagPromptRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
