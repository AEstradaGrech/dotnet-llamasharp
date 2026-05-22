using AutoMapper;
using DocumentFormat.OpenXml.EMMA;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
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
        private readonly ILangSearchService _langSearch;
        private readonly RequestOptions _settings;
        private readonly ILogger<OllamaSharpService> _logger;

        public OllamaSharpService( IPromptCommandsFactory promptsFactory, ILangSearchService langSearch, IOptions<RequestOptions> settings, ILogger<OllamaSharpService> logger)
        {
            _promptsFactory = promptsFactory;
            _langSearch = langSearch;
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

        public async Task<ChatPrompt> GuidedBatchChainPrompt(ChainTestRequest request)
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


            //var chainResult = await FluentChainExtensions
            //   .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //            instruction: request.Instructions.First(),
            //            request.Settings),
            //            userPrompt: request.Prompt,
            //        finalSysmessage: request.SystemMessage,
            //        chainIntent: "",
            //        feedFwd: ""), 
            //        defaultSettings: request.Settings)
            //   .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: "Reduce the previous output if necessary to ensure it only includes the requested name and the age, remove the rest", request.Settings), feedFwd: "Use the name and age to develop your character part"),
            //    [
            //        new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings), feedFwd: "Use this game related data to ground your profile to the game lore"),
            //        new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
            //    ],
            //    new StepSettings())
            //   .ExposeThisId(out var id)
            //   //.Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings), feedFwd: "Use this game related data to ground your profile to the game lore"),
            //   //      new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
            //   //      inputCommandReq)
            //   .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //            instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore", request.Settings), 
            //            pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
            //   .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //            instruction: "Review the content so far and ensure that the character original town is Madriz. Rewrite the content to adapt it to this requirement if necessary", request.Settings), 
            //            pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
            //   //.WithFeedsFrom([id1, id2]) // añade 'id1.FeedFwd, id2.FeedFwd' a SU feedFwd. ES LO QUE SE LE AÑADE AL SIGUIENTE SIEMPRE. Se guardan los IDs y se busca msg OnRun en ChainRunner
            //   .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings), feedFwd: ""), 
            //         stepRequest: new StepSettings())
            //   .ThenExecuteAsync(withFinalMessage: true);


            //return new ChatPrompt { 
            //    Model = request.Settings.Model ?? "app-default",
            //    Input = request.Prompt, 
            //    Output = chainResult.Json, 
            //    ChatHistory = [chainResult.SystemInstructions, new ChatMessage(ChatRole.User.ToString(), request.Prompt), chainResult.Message] };

            return new ChatPrompt();
        }


        public async Task<ChainResult> GuidedBatchChain(ChainTestRequest request)
        {
            var inputCommandReq = new PromptCommandRequest { Model = request.Settings.Model, Prompt = request.Prompt };

            //return await FluentChainExtensions
            //   .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //            instruction: request.Instructions.First(), 
            //            request.Settings),
            //        userPrompt: request.Prompt,
            //        finalSysmessage: request.SystemMessage,
            //        chainIntent: "",
            //        feedFwd:""))
            //   .Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(1).First(), request.Settings), 
            //            feedFwd: "Use this game related data to ground your profile to the game lore"),
            //         new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings), 
            //            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
            //         stepRequest: new ChainPromptRequest())
            //   .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore. Ensure it is self-consistent (be sure you use any feeded data from the previous output", request.Settings),
            //        pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.",
            //        stepRequest: new ChainPromptRequest())
            //   .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //            instruction: request.Instructions.Skip(3).First(), 
            //            request.Settings), 
            //        feedFwd: ""), 
            //        stepRequest: new ChainPromptRequest())
            //   .ThenExecuteAsync(withFinalMessage: true);

            //return await FluentChainExtensions
            //  .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.First(),
            //           request.Settings),
            //       userPrompt: request.Prompt,
            //       finalSysmessage: request.SystemMessage,
            //       chainIntent: "Create game character",
            //       feedFwd: "Use the name and the age to develop you part of the character"),
            //       defaultSettings: request.Settings)
            //  .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //      instruction: request.Instructions.Skip(1).First(), 
            //      request.Settings),new ChainPromptRequest(), 
            //    feedFwdInstruction: "Use this character typical scene as a character concept to inspire your creations")
            //  .WithRebujito([/*api call to LangSearch, some loaded pdfs about X, DB_DATA of some kind, whatever*/], guidance: "Do X with this")
            //  .ExposeThisId(out var thenId)
            //  .Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings),
            //            feedFwd: "Use this game related data to ground your profile to the game lore"),
            //         new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings),
            //            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
            //        stepRequest: new ChainPromptRequest())
            //  .ExposeThisId(out var tapId)
            //  .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.Skip(4).First(),
            //           request.Settings),
            //       feedFwd: ""),  
            //       stepRequest: new ChainPromptRequest()
            //        .FeedFrom(thenId))
            //  .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            var langSearchRebujito = await _langSearch.SearchRankedTexts(new RankedPageRequest { Count = 3, Query = "Dunwich alike horror stories in the style of H.P Lovecraft o The Mist by Stephen King" }, returnSnippet: false);

            //return await FluentChainExtensions
            //  .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.First(),
            //           request.Settings),
            //       userPrompt: request.Prompt,
            //       finalSysmessage: request.SystemMessage,
            //       chainIntent: "Create game character",
            //       feedFwd: "Use the name and the age to develop you part of the character"),
            //       defaultSettings: request.Settings)
            //  .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //      instruction: request.Instructions.Skip(1).First(),
            //      request.Settings), new ChainPromptRequest(),
            //    feedFwdInstruction: "Use this character typical scene as a character concept to inspire your creations")
            //  .WithRebujito(langSearchRebujito, guidance: "Use the below data to get inspiration and capture the desired style and mood for your character")
            //  .ExposeThisId(out var thenId)
            //  .Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings),
            //            feedFwd: "Use this game related data to ground your profile to the game lore"),
            //         new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings),
            //            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
            //        stepRequest: new ChainPromptRequest())
            //  .ExposeThisId(out var tapId)
            //  .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.Skip(4).First(),
            //           request.Settings),
            //       feedFwd: ""),
            //       stepRequest: new ChainPromptRequest()
            //        .FeedFrom(thenId))
            //  .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            //return await FluentChainExtensions
            //  .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.First(),
            //           request.Settings),
            //       userPrompt: request.Prompt,
            //       finalSysmessage: request.SystemMessage,
            //       chainIntent: "Create game character",
            //       feedFwd: "Use the name and the age to develop you part of the character"),
            //       defaultSettings: request.Settings)
            //  .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //      instruction: request.Instructions.Skip(1).First(),
            //      request.Settings), new ChainPromptRequest(),
            //    feedFwdInstruction: "Use this character typical scene as a character concept to inspire your creations")
            //  .ExposeThisId(out var thenId)
            //  .Tap([new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(2).First(), request.Settings),
            //            feedFwd: "Use this game related data to ground your profile to the game lore"),
            //         new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(instruction: request.Instructions.Skip(3).First(), request.Settings),
            //            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")],
            //        stepRequest: new ChainPromptRequest())
            //  .WithRebujito(langSearchRebujito, guidance: "Use the below data to get inspiration and capture the desired style and mood for your character")
            //  .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //           instruction: request.Instructions.Skip(4).First(),
            //           request.Settings),
            //       feedFwd: ""),
            //       stepRequest: new ChainPromptRequest()
            //        .FeedFrom(thenId))
            //  .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            //return await FluentChainExtensions
            // .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //          instruction: request.Instructions.First(),
            //          request.Settings),
            //      userPrompt: request.Prompt,
            //      finalSysmessage: request.SystemMessage,
            //      chainIntent: "Create game character",
            //      feedFwd: ""),
            //      defaultSettings: request.Settings)
            // .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //     instruction: "Reduce the previous output to ensure it only contains the requested name and age, remove the rest",
            //     request.Settings), new ChainPromptRequest(),
            //   feedFwdInstruction: "Use the name and the age to develop you part of the character")
            // .ExposeThisId(out var thenId)
            // .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //        instruction: request.Instructions.Skip(1).First(), request.Settings), 
            //        feedFwd: "Use this character typical scene as a character concept to inspire your creations"), [
            //            new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //                    instruction: request.Instructions.Skip(2).First(), 
            //                    request.Settings), 
            //                feedFwd: "Use this game related data to ground your profile to the game lore"),
            //            new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //                    instruction: request.Instructions.Skip(3).First(), 
            //                    request.Settings), 
            //                feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
            //        ],
            //    new ChainPromptRequest())
            // .WithRebujito(langSearchRebujito, guidance: "Use the below data as an inspiration for your typical scene generation") // En SplitThrough el rebujito va para el splitted y fan-out al resto con el resultado
            // .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //          instruction: request.Instructions.Skip(4).First(),
            //          request.Settings),
            //      feedFwd: ""),
            //      stepRequest: new ChainPromptRequest()
            //       .FeedFrom(thenId))
            // .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            //return await FluentChainExtensions
            // .StartWith(new FirstInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //          instruction: request.Instructions.First(),
            //          request.Settings),
            //      userPrompt: request.Prompt,
            //      finalSysmessage: request.SystemMessage,
            //      chainIntent: "Create game character",
            //      feedFwd: ""),
            //      defaultSettings: request.Settings)
            // .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //     instruction: "Reduce the previous output to ensure it only contains the requested name and age, remove the rest",
            //     request.Settings), new ChainPromptRequest(),
            //   feedFwdInstruction: "Use the name and the age to develop you part of the character")
            // .ExposeThisId(out var thenId)
            // .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //        instruction: request.Instructions.Skip(1).First(), request.Settings),
            //        feedFwd: "Use this character typical scene as a character concept to inspire your creations"), [
            //            new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //                    instruction: request.Instructions.Skip(2).First(),
            //                    request.Settings),
            //                feedFwd: "Use this game related data to ground your profile to the game lore"),
            //            new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //                    instruction: request.Instructions.Skip(3).First(),
            //                    request.Settings),
            //                feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
            //        ],
            //    new ChainPromptRequest())
            // .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //        instruction: "Review the content so far and ensure that the character original town is Madriz. Rewrite the content to adapt it to this requirement if necessary", request.Settings),
            //        pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
            // .WithRebujito(langSearchRebujito, guidance: "Use the below data to bias your final response towards that style, ambience, topic or vibe", feedDose: 800) // En SplitThrough el rebujito va para el splitted y fan-out al resto con el resultado
            // .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
            //          instruction: request.Instructions.Skip(4).First(),
            //          request.Settings),
            //      feedFwd: ""),
            //      stepRequest: new ChainPromptRequest()
            //       .FeedFrom(thenId))
            // .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Cada step tiene un command que recibe unos settings propios o ningunos (y usa _runner.Default)
            // y el step lleva la request del comando configurada
            //      > Nota: NUNCA PASAR PromptCommandRequest.Settings (si null los lee del comando, que son los buenos con 'CommandValidations' etc

            // Tambien tienen una Instruction asignada que mapea a 'prompt propio + _systemMessage' para el comando (excepto el primero, que lee req.Prompt y req.SystemMessage)
            // Y un FeedFwd para guiar al siguiente con sus resultados
            // Tambien se puede pedir un mensaje opcional que intenta devolver el resultado de la cadena en forma de ChatMessage
            // Ese mensaje se puede guiar con el req.FinalSysMessage que viaja en el ChainRunner. Tambien puede ejecutarse con unos settings propios o con los default
            return await FluentChainExtensions
             .StartWith(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                      instruction: request.Instructions.First().SystemMessage, settings: request.Settings), // _sysMsg        "FirstCmd = userPrompt
                      settings: new StepSettings(new PromptCommandRequest(
                          message: request.Prompt, // esto y Instructions.First().Prompt es lo mismo ->  
                          guidanceMessage: request.SystemMessage // initial step guidance
                       )),
                      feedFwd: ""),
                  defaultSettings: request.Settings,
                  finalSysMessage: request.FinalSystemMessage, // Esto deberia ser propio de un ChainRequestDto.cs
                  chainIntent: "Create game character") // use as 'Overall goal' (appended to Step.ContextMessage to guide the llm).
             .ExposeThisId(out var startId)
             .Then(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                 instruction: "Reduce the previous output to ensure it only contains the requested name and age, remove the rest",
                 request.Settings), new StepSettings(new PromptCommandRequest("")), //StepInstruction (& sons) llevan Icommand, step Feed y la TCommReq
               feedFwdInstruction: "Use the name and the age to develop you part of the character")
             .ExposeThisId(out var thenId)
             .SplitThrough(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                    instruction: request.Instructions.Skip(1).First().SystemMessage, settings: request.Settings), // Instruction[N].SystemMessage
                    settings: new StepSettings(new PromptCommandRequest(message: request.Instructions.Skip(1).First().Prompt)), // Instruction[N].Prompt  Instruction (instruction ,prompt) = (_system, prompt) --> STEP = instruction + Guidance (prev_ctx) & prompt ?? default 'Complete task'
                    feedFwd: "Use this character typical scene as a character concept to inspire your creations"), [
                        new StepInstruction(_promptsFactory.GetStringChoiceCommand( 
                                dbMessageName: "", //Whatever command you want to execute. It is 'paired' to its specific request using the next param. Stored in step until inference
                                guidanceMessage: request.Instructions.Skip(2).First().SystemMessage), //THIS MAPS TO COMMAND._systemMessage (cmd.guidance is generated for the next inside the steps on runtime
                            new StepSettings(new StringChoiceRequest(
                                choices: [ // Specific StepCommand request configuration
                                    "Miguelianos",
                                    "SGAE",
                                    "Panaderia y Reposteria Nuestra Señora del Carmen"
                                ], 
                                message: request.Instructions.Skip(2).First().Prompt, // A clear user prompt for the command. Step.Hardcoded message if none
                                settings: null, // specific settings for this command, otherwise _runner.DefaultSettings = reqDto.Settings
                                model: null)) // some specific model for this step command
                                .FeedFrom(startId), 
                            feedFwd: "Use this game related data to ground your profile to the game lore"),// STEP FeedForwardMessage for the next one (next._request.GuidanceMessage) with instructions & context for the next 
                        new StepInstruction(_promptsFactory.GetEnumChoiceCommand<EGameLocations>(
                            dbMessageName: string.Empty,
                            guidanceMessage: request.Instructions.Skip(3).First().SystemMessage), 
                            new StepSettings(new PromptCommandRequest(message: request.Instructions.Skip(3).First().Prompt)), 
                            feedFwd: "Use the selected location as the character's born place"),
                        new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                                instruction: request.Instructions.Skip(4).First().SystemMessage),
                                settings: new StepSettings(new PromptCommandRequest(message: request.Instructions.Skip(4).First().Prompt))
                                    .WithDataBoost("Use the below data to bias your final response towards that style, ambience, topic or vibe", 
                                        await _langSearch.SearchRankedTexts(new RankedPageRequest { Count = 6, Query = "Summary of the Horror of Dunwich, by H.P. Lovecraft. Summary of The Mist, by Stephen King" }, returnSnippet: true)),
                            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore")
                    ])
             .Pipe(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                    instruction: "Review the content so far and combine it with the provided sources in a very short story plot (around 50 words)", settings: request.Settings),
                    pipedSettings: new StepSettings(new PromptCommandRequest(message:"")),
                    pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
             .WithRebujito(await _langSearch.SearchRankedTexts(new RankedPageRequest { Count = 4, 
                 Query = "1984, George Orwell. The Terror, Dan Simmons. A Brave New World, Aldous Huxley" }, returnSnippet: false),
                guidance: "Use the below data to bias your final response towards that style, ambience, topic or vibe", feedDose: 600)
             .Join(new StepInstruction(_promptsFactory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                      instruction: request.Instructions.Skip(5).First().SystemMessage),
                      settings: new StepSettings(new PromptCommandRequest(message: request.Instructions.Skip(5).First().SystemMessage))
                        .FeedFrom(thenId),
                      feedFwd: ""))
             .ThenExecuteAsync(request.WithFinalMessage, request.WithReport, request.FinalMessageSettings ?? request.Settings);
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
