using AutoMapper;
using DocumentFormat.OpenXml.EMMA;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.AtomicValues;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests.Chains;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.StructuredOutput;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Repositories.Chroma;
using DotnetLlamaSharp.Domain.Services.Inference;
using DotnetLlamaSharp.Domain.Services.Prompting;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
using MicrosoftAI = Microsoft.Extensions.AI;

namespace DotnetLlamaSharp.Services.Prompting
{
    // FYI: https://elbruno.com/tag/ollama/
    public class OllamaSharpService : IOllamaSharpService
    {
        //private readonly IOllamaInferenceService _ollamaService;
        private readonly IPromptCommandsFactory _promptsFactory;

        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;

        public OllamaSharpService( IPromptCommandsFactory promptsFactory, IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _promptsFactory = promptsFactory;
            _settings = settings.Value ?? new RequestOptions();
            _logger = logger;

            if (settings.Value == null)
                _logger.LogWarning("-- No RequestOptions injected from app settings. Using default OllamaSharp request options --");
        }

        // call to '/api/generate' endpoint for single Q&A inference
        public async Task<ChatPrompt> SimplePrompt(SimplePromptRequest request)
        {
            if (string.IsNullOrEmpty(request.SystemMessage))
                request.SystemMessage = "You are a helpful assistant";

            var messages = new List<ChatMessage> {
                new ChatMessage(ChatRole.System.ToString(), request.SystemMessage),
                new ChatMessage(ChatRole.User.ToString(), request.Prompt)
            };

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage);

            var response = await command.Prompt(new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt, Settings = _settings });

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = messages };
        }

        // call to '/api/chat' endpoint to handle user-assistant chat turns
        public async Task<ChatPrompt> ChatPrompt(ChatPromptRequest request, bool bWithSysmsgUpdate)
        {
            var sb = new System.Text.StringBuilder();

            var command = _promptsFactory.GetMessagePromptCommand(request.SystemMessage);

            var response = await command.Prompt(new ChatCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt, Settings = _settings, ChatHistory = request.ChatHistory, IncludeSystemMessage = bWithSysmsgUpdate });

            request.ChatHistory.Add(new ChatMessage(response.Role.ToString(), response.Content));

            return new ChatPrompt { Model = request.Settings.Model, Input = request.Prompt, Output = response.Content ?? "", ChatHistory = request.ChatHistory };
        }

        public async Task<EmbedResponse> GetOllamaClientEmbeddings(EmbedRequest request)
        {
            request.Options = _settings;

            return null; //TODO: EMBEDDINGS COMMAND await _ollamaService.GetEmbeddings(request);
        }

        public async Task<ChatPrompt> GuidedBatchChain(GuidedBatchRequest request)
        {

            // TODO: request.Instructions = new Dictionary<ECommandType, string>() -> [ECommandType.GUIDED] = "Do blah blah", [ECommandType.DB] = "db-messsage-name"
            //       new ChainStep(req.Key == ECommandType.GUIDED ? 
            //          _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(guidanceMessage: request.Instructions.First().Value, request.Settings) :                      [JSONEABLE BASE]
            //          _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(dbMessageName: request.Instructions.First().Value, guidanceMessage: null, request.Settings)   [JSONEABLE CHROMA]
            //          isPreloaded: true);
            //var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };

            //IChaineable initialStep = new ChainStep(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, isPreloaded: true);

            //var currentStep = initialStep;
            //var instructions = request.Instructions.Skip(1).ToList();
            //request.Instructions.ForEach(instruction => currentStep.Then<MessagePromptCommand, ChatMessage>(request)); //TODO: comprobar esta FluentAPI tambien
            //instructions.ForEach(instruction => currentStep = currentStep.Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: instruction, settings: request.Settings), inputCommandReq));

            //var chainResult = await initialStep.ExecuteChain();

            //TODO: ChainService con getJsoneable<TComm, TResult> y asi es menos verboso. _chainService lleva _promptsFactory
            /*
             *  var chain = new ChainResult();
                var finalStep = FluentChainExtensions
                   .StartWith(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: request.Instructions.First(), request.Settings), isPreloaded: true)
                   .Then(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "Do this", settings: request.Settings), request)
                   .Then(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "Do that", settings: request.Settings), request)
                   .Then(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "And also this", settings: request.Settings), request)
                   .ThenExecute(out chain, withFinalMessage: true);

            */

            var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };

            //var chain = FluentChainExtensions
            //    .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, isPreloaded: true, feedFwdInstruction: "Enhance the extracted topic, but avoid repetition.")
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Explain the previous output in less than 200 words.", settings: request.Settings), inputCommandReq)
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words.", settings: request.Settings), inputCommandReq, feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character")
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Analyze the content of the previous output and rewrite it as if you were Master Miyagi, from the Karate Kid movie.", settings: request.Settings), inputCommandReq);

            //var chainResult = await chain.ExecuteChain(withUserFriendlyMessage: true);

            
            var finalStep = FluentChainExtensions
                .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, isPreloaded: true)
                .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Explain the previous output in less than 200 words.", settings: request.Settings), inputCommandReq)
                //.Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words.", settings: request.Settings), inputCommandReq, feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character")
                .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Analyze the content of the previous output and rewrite it as if you were Blackie Lawless, the singer and frontman of the band WASP.", settings: request.Settings), inputCommandReq)
                .ThenExecute(out var chainResult, withFinalMessage: true);

            //////////////////////////////////////////////////////////////////
            ///
            //initialStep.Then<NumericResponse>(request, _promptsFactory.GetAsPrompteable<NumericPromptCommand, NumericResponse>("", null, request.Settings))
            /*
             command<CreateCharacterCommand, CharacterResponse>(request, new PromptCommandReq&Sons)
                .Then<JsonValidationCommand, ScoredBoolResponse>(request, new PromptCommandReq&Sons)
                .Then<JsonReviewCommand, CharacterResponse>(request, new PromptCommandReq&Sons)

            ----------------------------------------------

             StartWith(request, new PromptCommandReq&Sons) [RETURN STEP]
                .Then<JsonValidationCommand, ScoredBoolResponse>(request, new PromptCommandReq&Sons)
                .Then<JsonReviewCommand, CharacterResponse>(request, new PromptCommandReq&Sons)

            step.From<UserIntentCommand, ChatMessage>()
                .Branch<ScoredBoolCommand, ScoredBoolResponse>("# USER INTENT:")
                
            ---------------------------------------------------

             step.From<UserIntentCommand, ChatMessage>()
                .Branch(request, validOption: IPrompteable<T1>, fallbackOption: IPrompteable<T2>, ScoredBoolCommand evaluator)
                .Then<QueryAugmentCommand, ChatMessage>(request) <-- esto es siempre validOption (if evaluator.True) si no fallbackOption tiene que terminar la cadena (throw new ChainFallbackException ? y todo se ejecuta siempre dentro de un try-catch (o todo va bien, o la pillo y hago X
                
             */

            return new ChatPrompt { 
                Model = request.Settings.Model ?? "app-default",
                Input = request.Prompt, 
                Output = chainResult.Json, 
                ChatHistory = [chainResult.SystemInstructions, new ChatMessage(ChatRole.User.ToString(), request.Prompt), chainResult.Message] };
        }

        //public async Task<ChatPrompt> BooleanQuestion(SimplePromptRequest req)
        //{
        //    var command = _promptsFactory.GetBoolPromptCommand("bool-resp");

        //    var response = await command.Prompt(_ollamaService, new PromptCommandRequest { Model = req.Model, Prompt = req.Prompt, Settings = _settings });

        //    if (response == null)
        //        throw new ArgumentNullException($"{nameof(OllamaSharpService)} >> {nameof(BooleanQuestion)} >> An error has ocurred while requesting the structured output, try again");

        //    var stringResponse = response ? "YES" : "NO";

        //    var messages = new List<ChatMessage> { new ChatMessage(ChatRole.System.ToString(), req.SystemMessage), new ChatMessage(ChatRole.User.ToString(), req.Prompt)};

        //    messages.Add(new ChatMessage(ChatRole.Assistant.ToString(), stringResponse));

        //    return new ChatPrompt { Model = req.Model, Input = req.Prompt, Output =  stringResponse, ChatHistory = messages };
        //}

    }
}
