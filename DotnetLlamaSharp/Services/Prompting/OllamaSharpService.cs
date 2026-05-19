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

        public async Task<ChatPrompt> GuidedBatchChainPrompt(GuidedBatchRequest request)
        {

            // TODO: request.Instructions = new Dictionary<ECommandType, string>() -> [ECommandType.GUIDED] = "Do blah blah", [ECommandType.DB] = "db-messsage-name"
            //       new ChainStep(req.Key == ECommandType.GUIDED ? 
            //          _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(guidanceMessage: request.Instructions.First().Value, request.Settings) :                      [JSONEABLE BASE]
            //          _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(dbMessageName: request.Instructions.First().Value, guidanceMessage: null, request.Settings)   [JSONEABLE CHROMA];
            //var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };

            //IChaineable initialStep = new ChainStep(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, isPreloaded: true);

            //var currentStep = initialStep;
            //var instructions = request.Instructions.Skip(1).ToList();
            //request.Instructions.ForEach(instruction => currentStep.WireTo<MessagePromptCommand, ChatMessage>(request)); //TODO: comprobar esta FluentAPI tambien
            //instructions.ForEach(instruction => currentStep = currentStep.WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: instruction, settings: request.Settings), inputCommandReq));

            //var chainResult = await initialStep.ExecuteChain();

            //TODO: ChainService con getJsoneable<TComm, TResult> y asi es menos verboso. _chainService lleva _promptsFactory
            /*
             *  var chain = new ChainResult();
                var finalStep = FluentChainExtensions
                   .StartWith(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: request.Instructions.First(), request.Settings))
                   .WireTo(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "Do this", settings: request.Settings), request)
                   .WireTo(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "Do that", settings: request.Settings), request)
                   .WireTo(chainStepFor<MessagePromptCommand, ChatMessage>(guidanceMessage: "And also this", settings: request.Settings), request)
                   .WireToExecute(out chain, withFinalMessage: true);

            */

            var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };

            //var chain = FluentChainExtensions
            //    .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, feedFwdInstruction: "Enhance the extracted topic, but avoid repetition.")
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Explain the previous output in less than 200 words.", settings: request.Settings), inputCommandReq)
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words.", settings: request.Settings), inputCommandReq, feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character")
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Analyze the content of the previous output and rewrite it as if you were Master Miyagi, from the Karate Kid movie.", settings: request.Settings), inputCommandReq);

            //var chainResult = await chain.ExecuteChain(withUserFriendlyMessage: true);


            //var finalStep = FluentChainExtensions
            //    .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq)
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Explain the previous output in less than 200 words.", settings: request.Settings), inputCommandReq)
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words.", settings: request.Settings), inputCommandReq, feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character")
            //    .WireTo(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Analyze the content of the previous output and rewrite it as if you were Blackie Lawless, the singer and frontman of the band WASP.", settings: request.Settings), inputCommandReq)
            //    .ThenExecute(out var chainResult, withFinalMessage: true);

            //var chainResult = await FluentChainExtensions
            //    .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq, null)
            //    .Tap([new StepInstruction(), new StepInstruction()], inputCommandReq)
            //    .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), pipeFeedFwd: string.Empty)
            //    .Join(new StepInstruction(), inputCommandReq)
            //    .Feed(new StepInstruction(), [new StepInstruction(), new StepInstruction(), new StepInstruction()], inputCommandReq)
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Explain the previous output in less than 200 words.", settings: request.Settings), inputCommandReq)
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words.", settings: request.Settings), inputCommandReq, feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character")
            //    .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Analyze the content of the previous output and rewrite it as if you were Blackie Lawless, the singer and frontman of the band WASP.", settings: request.Settings), inputCommandReq)
            //    .ThenExecuteAsync(withFinalMessage: true);


            var chainResult = await FluentChainExtensions
               .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq)
               .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Reduce the previous output if necessary to ensure it only includes the requested name and the age, remove the rest", request.Settings), feedFwd: "Use the name and age to develop your character part"),
                [
                    new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings), feedFwd: "Use this game related data to ground your profile to the game lore"),
                    new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
                ],
                inputCommandReq)
               .ExposeThisId(out var id)
               //.Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings), feedFwd: "Use this game related data to ground your profile to the game lore"),
               //      new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
               //      inputCommandReq)
               .Pipe(inputCommandReq,
                    _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore", request.Settings), 
                        pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
               .Pipe(inputCommandReq, 
                    _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the content so far and ensure that the character original town is Madriz. Rewrite the content to adapt it to this requirement if necessary", request.Settings), 
                        pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
               //.WithFeedsFrom([id1, id2]) // añade 'id1.FeedFwd, id2.FeedFwd' a SU feedFwd. ES LO QUE SE LE AÑADE AL SIGUIENTE SIEMPRE. Se guardan los IDs y se busca msg OnRun en ChainRunner
               .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings), feedFwd: ""), inputCommandReq)
               .ThenExecuteAsync(withFinalMessage: true);

            //////////////////////////////////////////////////////////////////
            ///
            //initialStep.WireTo<NumericResponse>(request, _promptsFactory.GetAsPrompteable<NumericPromptCommand, NumericResponse>("", null, request.Settings))
            /*
             command<CreateCharacterCommand, CharacterResponse>(request, new PromptCommandReq&Sons)
                .WireTo<JsonValidationCommand, ScoredBoolResponse>(request, new PromptCommandReq&Sons)
                .WireTo<JsonReviewCommand, CharacterResponse>(request, new PromptCommandReq&Sons)

            ----------------------------------------------

             StartWith(request, new PromptCommandReq&Sons) [RETURN STEP]
                .WireTo<JsonValidationCommand, ScoredBoolResponse>(request, new PromptCommandReq&Sons)
                .WireTo<JsonReviewCommand, CharacterResponse>(request, new PromptCommandReq&Sons)

            step.From<UserIntentCommand, ChatMessage>()
                .Branch<ScoredBoolCommand, ScoredBoolResponse>("# USER INTENT:")
                
            ---------------------------------------------------

             step.From<UserIntentCommand, ChatMessage>()
                .Branch(request, validOption: IPrompteable<T1>, fallbackOption: IPrompteable<T2>, ScoredBoolCommand evaluator)
                .WireTo<QueryAugmentCommand, ChatMessage>(request) <-- esto es siempre validOption (if evaluator.True) si no fallbackOption tiene que terminar la cadena (throw new ChainFallbackException ? y todo se ejecuta siempre dentro de un try-catch (o todo va bien, o la pillo y hago X
                
             */

            return new ChatPrompt { 
                Model = request.Settings.Model ?? "app-default",
                Input = request.Prompt, 
                Output = chainResult.Json, 
                ChatHistory = [chainResult.SystemInstructions, new ChatMessage(ChatRole.User.ToString(), request.Prompt), chainResult.Message] };
        }


        public async Task<ChainResult> GuidedBatchChain(GuidedBatchRequest request)
        {
            var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };
 
            return await FluentChainExtensions
               .StartWith(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.First(), request.Settings), inputCommandReq)
               .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                   instruction: "Reduce the previous output and ensure it only includes the requested name and the age, remove the rest. Do not provide any other information about the character, only the requested name and age (in whatever format is the age written)", request.Settings), 
                   feedFwd: "Use the name and age to develop your part of the character using the provided name and age"), // InSplitThrough feedFwd is the message for the plugged instructions (not the next step), in TAP is added to the next guidance (as a general guidance)
                [
                    new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings),
                        feedFwd: "Use this game related data to ground your profile to the game lore"),
                    new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), 
                        feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
                ],
                inputCommandReq)
               //.ExposeThisId(out var id)
               .Pipe(inputCommandReq,
                    _promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore. Ensure it is self-consistent (be sure you use any feeded data from the previous output", request.Settings),
                        pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
               .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings), feedFwd: ""), inputCommandReq)
               .ThenExecuteAsync(withFinalMessage: true);
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
