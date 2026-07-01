using AutoMapper;
using Dotnet.Chroma.Repositories.Models.Enums;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Core.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.Storeables;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting.Samples;
using DotnetLlamaSharp.Models.Request;
using DotnetLlamaSharp.Models.Request.Chains;
using DotnetLlamaSharp.Models.Request.Chains.Samples;
using DotnetLlamaSharp.Models.Request.Chroma;
using DotnetLlamaSharp.Models.Request.Embeddings;
using DotnetLlamaSharp.Models.Response;
using DotnetLlamaSharp.Models.Response.Chroma;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DotnetLlamaSharp.Controllers.Samples
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(LameSamplesController))]
    public class LameSamplesController(IMapper mapper, ILameSamplesService samplesService, IPromptCommandsService commandsService, IChromaService chromaService) : ControllerBase
    {
        private readonly IMapper _mapper = mapper;
        private readonly ILameSamplesService _samplesService = samplesService;
        private readonly IChromaService _chromaService = chromaService;

        // Inject the CommandsService in your app to execute commands or use the IPromptCommandsFactory if you prefer it
        private readonly IPromptCommandsService _ollamaCommands = commandsService;

        [HttpPost("/chains/example/parallel")]
        public async Task<IActionResult> ChainTests([FromBody] ChainedPromptDto request)
        {
            var response = await _samplesService.ParallelChainExample(_mapper.Map<ChainedPromptDto, ChainedPrompt>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/chains/example/parallel/subchains")]
        public async Task<IActionResult> SubChainTests([FromBody] ChainedPromptDto request)
        {
            var response = await _samplesService.ParallelSubChainsExample(_mapper.Map<ChainedPromptDto, ChainedPrompt>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/chains/example/smart-rag")]
        public async Task<IActionResult> SmartRagChain([FromBody] SimplePromptRequestDto request)
        {
            var response = await _samplesService.SmartRagChain(_mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/chains/example/chat")]
        public async Task<IActionResult> ConditionalChatChain([FromBody] ChatPromptRequestDto request)
        {
            var response = await _samplesService.ConditionalChatChain(_mapper.Map<ChatPromptRequestDto, CommandChatRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>Get
        /// Pass a list of choices and ask the LLM to select the best value. Use the system message to guide the LLM if necessary
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/single-choice/prompt")]
        public async Task<IActionResult> SingleChoice([FromBody] StringChoiceRequestDto dto)
        {
            var response = await _ollamaCommands.StringChoice(dto.Prompt, dto.Choices, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Pass a list of choices and ask the LLM to select the best value. Use the system message to guide the LLM if necessary
        /// The scored version returns the selected choice along with the confidence score and a comment explaining the reason
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/scored-choice/prompt")]
        public async Task<IActionResult> ScoredChoice([FromBody] StringChoiceRequestDto dto)
        {
            /*Here is a sample of a request. You can use this request in Swagger to test the endpoint:
             (Note: you can add extra instructions to modify the output using the request system message
             {
              "prompt": "Which month corresponds to the spring season?",
              "systemMessage": "# IMPORTANT: You MUST add the '#warning:' tag to your justification comment when you return an empty value",
              "settings": {
                "model": "llama3.1-lexi-v2",
                "maxTokens": 300,
                "contextLength": 4096,
                "temperature": 0.9,
                "topP": 0.8,
                "topK": 30,
                "miroStat": 1,
                "miroStatEta": 0.4,
                "miroStatTau": 15.0,
                "repeatPenalty": 1.3,
                "repeatLastN": 20,
                "commandValidations": 0,
                "validationType": 0,
                "useDefaultCommandMessage": true
              },
              "choices": [
                "JANUARY", "DECEMBER", "JULY", "OCTOBER"
              ]
            }
             */
            var response = _mapper.Map<ScoredStringChoice, ScoredChoiceDto>(
                await _ollamaCommands.ScoredChoice(dto.Choices, dto.Prompt, dto.SystemMessage,dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Pass a list of choices and ask the LLM to select the best value. Use the system message to guide the LLM if necessary
        /// The scored version returns the selected choice along with the confidence score and a comment explaining the reason</summary
        /// The LLM will select up to N values depending on the user query or 0 if there is no logical selection for the input
        /// <param name="dto"></param>
        /// <param name="maxChoices"></param>
        /// <returns></returns>
        [HttpPost("/commands/multi-choice/prompt")]
        public async Task<IActionResult> MultiChoice([FromBody] StringChoiceRequestDto dto, [FromQuery] int maxChoices = 1)
        {
            /*
             * Heres is a real example you can solve with LameChain:
             * This apparently easy task seems to be impossible for most of the local models I use (ranging from llama3.1:8b to gemma3:27b)
             * The task is to select up to N maximum choices or none depending on how much related are to the user query ans output it as a List (so, output to structured class)
             * In the example the LLM has to select months that:
             *  - may not be in the list (must ignore 'related' or 'almost-fine' options and return an empty list)
             *  - may be more than the requested (checks that the model clamps the results to the max requested)
             *  - may be less than the requestd (at least one choice matching the intent)
             *  
             *  For some reason all models fail to execute this task (unless prompted explicitely to select the months, which is
             *  the equivalent to hardcoding a use-case and not valid for a general use command). 
             *  To solve that kind of problems you should use a smarter model, but if you are trying to run your app with the fastest local model ('dumb' models) you can use different command validations to review and rewrite 
             *  the fist output so the problem is solved iteratively working on the previous invalid results (note: 'dumb' models may return wrong responses more often. Just be aware of that if you are testing this endpoints)
             *  
             *  This example I finally made it work using hermes3 or qwen2.5:14b by passing the first output (that usually includes months that are not in the list or closest to the requested season but out of the intent scope)
             *  Once the validator recieves an 'almost-fine' result with wrong selections, it is easier for the LLM to review the instruction and rewrite the result in the same JSON format but with the right values.
             *  In case you are still having troubles with your chain and the reviewer is not smart enough to fix the problem, you can use 'BOOL_AND_REVIEW' (validation type 2) tha will first perform an ScoredBoolCommand
             *  to review the content and format AND pass a more specific message about the problem to the rewriter in case the output content is wrong, guiding further the reviwer and producing more focused responses)
             *  
             *  (Note: in this case I had to enforce the instruction with more guidance and a explicit user prompt. Sometimes is the only way, just try to give the instructions en general terms in the guidance
             *  and more specific details about the case your dealing with in the user prompt)
             *  
         
               {
                  "prompt": "Select any month corresponding to the summer season present in the choices list (according to the North Hemisphere seasons). ### IMPORTANT: If there are no matching choices in the list, return an empty list.",
                  "systemMessage": "Select a range of choices (if any) according to the maximum requested choices and the actual available choices given the user query",
                  "settings": {
                    "model": "hermes3",
                    "maxTokens": 600,
                    "contextLength": 6000,
                    "temperature": 0.0,
                    "topP": 0.1,
                    "topK": 5,
                    "miroStat": 0,
                    "miroStatEta": 0.4,
                    "miroStatTau": 4.0,
                    "repeatPenalty": 1.0,
                    "repeatLastN": -1,
                    "commandValidations": 1,
                    "validationType": 2,
                    "useDefaultCommandMessage": true
                  },
                  "choices": [
                    "DECEMBER", "JANUARY", "FEBRUARY", "OCTOBER", "APRIL"
                  ]
                }
             */
            var response = await _ollamaCommands.MultiChoice(dto.Prompt, dto.Choices, maxChoices:  maxChoices, guidanceMessage: dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns true or false for a given user question. Use the system message to guide the LLM if necessary
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/boolean/prompt")]
        public async Task<IActionResult> BooleanPrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Try different prompts to get a true / false response

            /*
             {
              "prompt": "Is Pepsi prefered world wide by the consumers rather than Coca Cola?",
              "systemMessage": "",
              "isGuidanceAppend": true,
               "settings": {
               "model": "hermes3",
               "maxTokens": 666,
               "contextLength": 6000,
               "temperature": 0.7,
               "topP": 0.9,
               "topK": 30,
               "miroStat": 1,
               "miroStatEta": 0.2,
               "miroStatTau": 5.0,
               "repeatPenalty": 1.5,
               "repeatLastN": -1,
               "commandValidations": 1,
               "validationType": 2,
               "useDefaultCommandMessage": true
             }
            }
             */
            var response = await _ollamaCommands.BooleanChoice(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns true or false for a given user question. Use the system message to guide the LLM if necessary
        /// returns also a confidence score and a comment explainig the reason
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/reasoned-boolean/prompt")]
        public async Task<IActionResult> ReasonedBooleanPrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Same than in the boolean prompt enpoint but scored and 'reasoned'
            // You can use the dto.SystemMessage to guide the LLM / help the model passing contextual data about the question
            var response = await _ollamaCommands.ReasonedBool(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns true or false for a given user question. Use the system message to guide the LLM if necessary
        /// returns also a confidence score and a comment explainig the reason
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/scored-boolean/prompt")]
        public async Task<IActionResult> ScoredBooleanPrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Same than in the boolean prompt enpoint but scored and 'reasoned'
            // You can use the dto.SystemMessage to guide the LLM / help the model passing contextual data about the question
            var response = await _ollamaCommands.ScoredBool(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns true or false for a given user question. Use the system message to guide the LLM if necessary
        /// returns also a confidence score and a comment explainig the reason
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/scored-response/prompt")]
        public async Task<IActionResult> ScoredPrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Same than in the boolean prompt enpoint but scored and 'reasoned'
            // You can use the dto.SystemMessage to guide the LLM / help the model passing contextual data about the question
            var response = await _ollamaCommands.ScoredPrompt(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
        /// <summary>
        /// Returns true or false for a given user question. Use the system message to guide the LLM if necessary
        /// returns also a confidence score and a comment explainig the reason
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/resoned-score/prompt")]
        public async Task<IActionResult> ReasonedScorePrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Same than in the boolean prompt enpoint but scored and 'reasoned'
            // You can use the dto.SystemMessage to guide the LLM / help the model passing contextual data about the question
            /*
             {
              "prompt": "Is Pepsi prefered world wide by the consumers rather than Coca Cola?",
              "systemMessage": "",
              "isGuidanceAppend": true,
               "settings": {
               "model": "hermes3",
               "maxTokens": 666,
               "contextLength": 6000,
               "temperature": 0.7,
               "topP": 0.9,
               "topK": 30,
               "miroStat": 1,
               "miroStatEta": 0.2,
               "miroStatTau": 5.0,
               "repeatPenalty": 1.5,
               "repeatLastN": -1,
               "commandValidations": 1,
               "validationType": 2,
   
               "useDefaultCommandMessage": true
             }
            }
             */
            var response = await _ollamaCommands.ReasonedScore(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        ///  string version of the BoolChoice in to be used directly in a chat loop or as context in a Lame Chain
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/yn/prompt")]
        public async Task<IActionResult> StringyBoolPrompt([FromBody] SimplePromptRequestDto dto)
        {
            var response = await _ollamaCommands.StringBoolChoice(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns a float number when the user question / instrucion can be solved with a decimal number or null if it cannot.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/numeric/prompt")]
        public async Task<IActionResult> NumericPrompt([FromBody] SimplePromptRequestDto dto)
        {
            // Try prompts that can be solved / answered with a number (decimals allowed) or null of the question cannot be answered with a float
            // note: this fails often with dumb / small local models (7b-8b models are have not proven to be too reliable for this task & output)
            var response = await _ollamaCommands.NumericResult(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            return response != null ? Ok(response) : Ok("The answer to the query cannot be a number");
        }

        /// <summary>
        /// Select one of the Enum values depending on the user query / instruction.
        /// Use the system message to guide the LLM if necessary
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/enum/prompt")]
        public async Task<IActionResult> EnumPrompt([FromBody] SimplePromptRequestDto dto)
        {
            //This example demonstrates how you can handle your app logic using your enums to make the LLM
            // select the best value for the given instruction. Here it is being use to select the best type of Chroma Chunks Collection
            // to store data depending on the user query
            var response = await _ollamaCommands.EnumChoice<EChunkType>(dto.Prompt, dto.SystemMessage, dto.IsGuidanceAppend, _mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(dto).Settings);

            return Ok(response);
        }

        /// <summary>
        /// This example demonstrates how you can use VectorSearchCommands to perform similarity searches on a vector DB
        /// using LameChain commands. In this case the setup is configured to query a Chroma DB, but the command
        /// works in a way in which you can plug any querying function to use other DBs.
        /// 
        /// To do that your embeddings response model must implement ILameSearchResult and
        /// the n you must create a retriever method matching this signature:
        /// 
        /// Task<List<ILameSearchResult>> YourFunctionName(string index, ReadOnlyMemory<float> queryEmbeddings, int resultsNumber, Dictionary<string, object> metadataFilters);
        /// 
        /// Then when you execute the command, the command.Request.UserPrompt is embedded and passed as a parameter in your retriever function that executes your vector search query using 
        /// whatever source you want.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("/commands/vector-search")]
        public async Task<IActionResult> VectorSearchCommand([FromBody] VectorSearchRequestDto dto)
            => Ok(await _samplesService.VectorSearchCommand(_mapper.Map<VectorSearchRequestDto, VectorSearchRequest>(dto)));


        /// <summary>
        /// This example demonstrates how to use storeable commands to store any type of model.
        /// In this case, a SystemChunk
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/commands/storeable/{collection}")]
        public async Task<IActionResult> StoreableCommand([FromBody] CreateSysChunkRequestDto dto, string collection)
        {
            // get a storeable of the type you want to store. Pass the lambda with the storing logic
            var command = _samplesService.StoreableCommand<ChromaSysChunk>(_chromaService.StoreSysChunk);

            // map dto to domain model and pass it in the storeable request along with the db collection name
            var commandRequest = new StoreableCommandRequest<ChromaSysChunk>(collectionName: collection, _mapper.Map<CreateSysChunkRequestDto, ChromaSysChunk>(dto));

            // Execute the command and get the result of the 'StoreSysChunk' lambda, which should be the persisted model with its DB ID
            var response = await command.Prompt(commandRequest);

            // Note: In case you are wondering 'why create a command to trigger some logic that is stored in a service. Why not use the service directly?'
            // then note that the command is intended to be used inside a chain that might recieve any type of output from the previous step, and it is supposed
            // to provide a generic way to store data (meaning, it should work with any type od DB), doing it this way you can use lambda functions to store
            // data in Chroma, Mongo, SQL, or whatever DB since the storing logic is completely left up to you (the base command basically triggers the stored lambda).
            // Also, the chain works with a command system and the only way to 'insert' business logic in it is encapsulating it in a dedicated command. Anyways,
            // if you are building your app just with Lame Commands, the idea is that you extend the 'StoreableCommand<TStored' to adapt it o your needs and you would
            // encapsulate all your storing logic in your extended command, then using a simple _repository method with this signature 'Task<TStored> mth(TStored inserted, string index)' as command lambda
            // something like:
            //      'ChromaSysChunkStoreable : Storeable<ChromaSysChunk> <- already typed to your business models
            //      [...]
            //      public ChromaSysChunkStoreable(repositoryLambda) {} <- pass the Create method of your IRepository<TStored> (in this case would be a IChromaRepo<TCol, TStored>)
            //      override async Task<ChromaSysChunk> Prompt(request)
            //      {
            //          // validate stuff, check whatever, insert something in related table... your business logic here
            //
            //          return await base.Prompt(request) <- call the base class that simply triggers the lambda
            //      }

            if (response != null)
                return Ok(_mapper.Map<ChromaSysChunk, ChromaSysChunkDto>(response));

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/db-prompt")]
        public async Task<IActionResult> DbPromptCommand([FromBody] SimplePromptRequestDto request)
        {
            var response = await _samplesService.DbPromptCommand(_mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/db-prompt/default-mode")]
        public async Task<IActionResult> DbPromptCommandDefaultMode([FromBody] SimplePromptRequestDto request)
        {
            var response = await _samplesService.DbPromptCommandDefaultMode(_mapper.Map<SimplePromptRequestDto, SimpleCommandRequest>(request));

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("/commands/message")]
        public async Task<IActionResult> ProviderMessageTest([FromBody] ChatPromptRequestDto request)
        {
            var chatCommandReq = _mapper.Map<ChatPromptRequestDto, CommandChatRequest>(request);

            var response = await _ollamaCommands.PromptCommand<MessagePromptCommand, ChatMessage>(_mapper.Map<CommandChatRequest, ChatCommandRequest>(chatCommandReq), request.SystemMessage, chatCommandReq.Settings);

            if (response != null)
                return Ok(response);

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
