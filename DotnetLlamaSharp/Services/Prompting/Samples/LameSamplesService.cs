using Dotnet.Chroma.Repositories.Models;
using Dotnet.LangSearch.SDK;
using Dotnet.LangSearch.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Bases;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.AtomicValues;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.Evaluators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Core.TextGenerators;
using Dotnet.OllamaSharp.LameChain.SDK.Command.Responses.StructuredOutputs;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Core.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.AtomicValues;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Request.QueryCommands;
using Dotnet.OllamaSharp.LameChain.SDK.Extensions;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces.Model;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Models.Shared;
using Dotnet.OllamaSharp.LameChain.SDK.Interfaces.Command.Services;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Request;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Response;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Step;
using Dotnet.OllamaSharp.LameChain.SDK.Models.Step.ValueObjects;
using DotnetLlamaSharp.Domain.Models.Entities.Chroma;
using DotnetLlamaSharp.Domain.Models.Enums;
using DotnetLlamaSharp.Domain.Models.Primitives.Chroma;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting;
using DotnetLlamaSharp.Domain.Models.Request;
using DotnetLlamaSharp.Domain.Services.Embeddings;
using DotnetLlamaSharp.Domain.Services.Prompting.Samples;
using OllamaSharp.Models.Chat;
using System.Text;

namespace DotnetLlamaSharp.Services.Prompting.Samples
{
    public class LameSamplesService : ILameSamplesService
    {
        private readonly IPromptCommandsFactory _factory;
        private readonly ILangSearchService _langSearch;
        private readonly IChromaService _chromaService;

        public LameSamplesService(IPromptCommandsFactory promptsFactory, ILangSearchService langSearch,  IChromaService chromaService)
        {
            _factory = promptsFactory;
            _langSearch = langSearch;
            _chromaService = chromaService;
        }

        public async Task<ChatPrompt> TemplatedExamples(ChainedPrompt request)
        {
            // Example of simple sequencing. This is just a demonstration of how a simple action like a Question-Answer chatbot
            // can be splitted in smaller substasks than can iteratively process the user input to produce enhanced results
            // Every steps executes a task that can be configured with it's own settings. The chain executes the instructions
            // sequentially optionally passing comments on the output for the next step. The next reads the previous results, executes its
            // task and triggers the next step until the chain is completed
            var finalStep = await LameChain
                .StartWith(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the user input and extract the intent", 
                        request.Settings),
                    new StepSettings(new PromptCommandRequest("How does a RAG application work?")) //this should be always the request.UserPrompt. This is persisted along the chain and passed as context to all the steps to give the context about the goal of the whole chain                   
                    ),
                    defaultSettings: request.Settings, // This will be the settings for all the chain steps unless you pass specific settings in the step Command instantiation.
                    finalSysMessage: "", // Skip this since it is meant to guide the assistant in its final 'user friendly' message and the chain does not need it (it returns already a ChatMessage)
                    chainIntent: "Enhances Question-Answer chat" // You can use this to keep the consistency of the overall chain context so all the steps know what are they doing in general terms. Leave it empty if you don't need it
                )
                .Then(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Explain the previous output in less than 100 words.", 
                        settings: request.Settings),
                     new StepSettings(new PromptCommandRequest("")), // You can leave the user prompt empty and try with the instruction only, but if you are using local models you may need to enforce the instruction with a more specific user prompt
                     feedFwdInstruction: "Use this as a base or guidance to develop the topic" // You can pass guidance instructions to the next step to get more focused results from the LLM
                )
                .Then(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Develop the topic of the previous output to provide an enhanced version of around 400-500 words."), // This one will use the DefaultSettings (no individual settings passed to the step command)
                     new StepSettings(new PromptCommandRequest("")),
                     feedFwdInstruction: "Use the provided topic information and explain it to the user according to your role / character" // You can leave this empty (it is not required),
                )                                                                                                                           // but in this case is being used to enforce the next instruction and avoid hallucinations with the provided enterprise assistant profile
                .Then(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the content of the previous output and rewrite it as if you were <<INSERT COMPLIANT CORPORATIVE AI ASSISTANT PROFILE HERE>>",
                        settings: request.Settings),
                     new StepSettings(new PromptCommandRequest("")),
                     feedFwdInstruction: "" // You can use this to guide the final 'user friendly' message if you request one
                )
                .ThenExecuteAsync(withFinalMessage: false, withReplay: false); 
            // In case the output of the las step is a complex model and you are using LameChain for chat aplications, you can request a final 'user friendly message' that will try to syntetize the chain output in a single ChatMessage to keep chatting with the user
            // Additionally, you can request the chain replay, with information about the input and output of every chain step. This is useful for debugging.
            // This setup represents a Chat loop iteration for that must perform some business-required pre-processing before answering the user (for example a Q&A bot for a website that must answer to specific name and inform the user about business related questions only)
            // The chain already returns a ChatMessage and it is supposed to be tested so there is no 'replay' needed


            // Example of splitting and merging chains: The previous example demonstrated a very simple sequential chain. With LameChain you can also make LLM requests in parallel
            // that recieve the same input context from the chain, then run their own instruction and finally join the results according to another summarization process
            // You can do that using SpllitedSteps and you can chain them using .Tap([]), that runs all the instructions array in parallel, or
            // using .SplitThrough(cmd, []), which does the same but running a previous command that will provide the result as context for the rest (using a common external feed of data if configured)
            var chainResult = await LameChain
                .StartWith(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the user input and extract the intent. Write a brief explanation of what is the user querying about"
                    ),
                    new StepSettings(new PromptCommandRequest(request.Prompt))
                ), 
                defaultSettings: request.Settings,
                // in this case the Join() step will summarize two different sources and the pass the result to the final 'user-friendly' message that should follow some rules / constraints about business rules
                finalSysMessage: "Analyze the content of the previous output and rewrite it as if you were <<INSERT COMPLIANT CORPORATIVE AI ASSISTANT PROFILE HERE>>", 
                chainIntent:"Analyze and provide a report about market stock data")
                .Tap([
                    // This simulates a parallel process in which one instruction will try to get a brief anwer with a private LLM model that will provide a general but 'accurate / custom' answer 
                    // and the other process retrieves data from a vector db similar to the user input to feed a summarizing step with the output of both processes
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Answer the user query in general terms. Dont'be too specific, instead try to give a global overview of the topic the user is querying about and mention other relevant / related topics"
                        ),
                        //Here I'm supposing a fine-tuned model of your property aligned with your business rules and feeded with, maybe, your private enterprise data about stock market
                        new StepSettings(new PromptCommandRequest(request.Prompt, model:"your-fine-tuned-secret-model"))
                    ),
                    new StepInstruction(
                         _factory.GetSourceable<VectorSearchSourceable>(), // you can use different commands in the chain to perform different processes to generate the final chain output
                        new StepSettings(new VectorSearchRequest( // Simply pass the proper request type for the command with its own parameters etc
                            index: "stock-data-2026",  // In this case the example is simulating a similarity search on a vector db to retrieve stock data that will be used in the next step to generate a summary
                            query: request.Prompt, // the user input is embedded and the used to query the vectors db
                            embedder: "nomic-embed-text",
                            dimensions: 512,
                            results: 10)
                        )
                    )
                ])
                .Join(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Analyze carefully the content of the previous outputs to generate a summary of around 600 word containing the a consistent answer to the user query along with a final 'Related Topics of Interest' with information that can be useful to the user to do a follow-up question",  
                            // suppose you have a request dto that passes specific command settings for this step because you need more focused or more creative settings or different CommandValidation settings
                            request.Settings // This could be low temp & low top-p settings to summarize more accordingly to the sources, for example
                        ),
                        new StepSettings(new PromptCommandRequest("Generate a report with a main section and a 'Related Topics of Interest' as instructed")), // enforce the instruction (imagine you are getting 'almost-fine' results with a dumb model... this can help to fix that)
                        feedFwd: "Review the provided report and adapt it to your final response according to your instruction rules" // In this example I'm requesting a final message so I can guide the generation from the last step to produce more focused results
                    )
                )
                .ThenExecuteAsync(withFinalMessage: true, withReplay: true); 
                // in this example the chain is requesting a 'user-friendly' message that will filter the chain content through a 'business-compliant' bot that may answer always within the scope of the business domain area and / or according to some character profile
                // 'withReplay' will add a collection of ReplayLogs with detailed information about the chain steps execution
                


            //Example of how to continue adding steps to a splitted chains
            chainResult = await LameChain
               .StartWith(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the user input and extract the intent. Write a brief explanation of what is the user querying about"
                    ),
                    new StepSettings(new PromptCommandRequest(request.Prompt))
                ),
                defaultSettings: request.Settings,
                finalSysMessage: "Analyze the content of the previous output and rewrite it as if you were <<INSERT COMPLIANT CORPORATIVE AI ASSISTANT PROFILE HERE>>",
                chainIntent: "Analyze and provide a report about market stock data")
               // This splits the chain exactly like in the previous example, but now I'm going to continue chaining commands to each tap command output to keep processing the results separately piping commands on each subchain
                .Tap([
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Answer the user query in general terms. Dont'be too specific, instead try to give a global overview of the topic the user is querying about and mention other relevant / related topics"
                        ),
                        //Here I'm supposing a fine-tuned model of your property aligned with your business rules and feeded with, maybe, your private enterprise data about stock market
                        new StepSettings(new PromptCommandRequest(request.Prompt, model:"your-fine-tuned-secret-model")) 
                    ),
                    new StepInstruction(
                         _factory.GetSourceable<VectorSearchSourceable>(), 
                        new StepSettings(new VectorSearchRequest( 
                            index: "stock-data-2026",  
                            query: request.Prompt, 
                            embedder: "nomic-embed-text",
                            dimensions: 512,
                            results: 10)
                        )
                    )
                ])
                // Once you SPLIT a chain you will get ONE OUTPUT for every plugged command. So now you need to use '.Pipe()' to continue the chain.
                .Pipe( // Pipe will execute the instruction command over each output from the prevous step, producing an equal number of outputs until you .Join() the subchains
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore. Ensure it is self-consistent (be sure you use any feeded data from the previous output", 
                        request.Settings),
                    new StepSettings(new PromptCommandRequest("")),
                    pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile."
                )
                // You can keep piping as many commands as you want, then Join the results like in the previous example and return the chain output with the optional 'user-friendly' message
               .Join(new StepInstruction(
                   _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                           instruction: "",
                           request.Settings
                       ),
                       new StepSettings(new PromptCommandRequest(""))
                   )
               )
               .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            // Example of how to split a chain through a command with shared common context inyected via rebujito
            chainResult = await LameChain
                .StartWith(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the user input and extract the intent. Write a brief explanation of what is the user querying about"
                    ),
                    new StepSettings(new PromptCommandRequest(request.Prompt))
                ),
                defaultSettings: request.Settings,
                finalSysMessage: "Analyze the content of the previous output and rewrite it as if you were <<INSERT COMPLIANT CORPORATIVE AI ASSISTANT PROFILE HERE>>",
                chainIntent: "Analyze and provide a report about market stock data")
                .ExposeThisId(out var thenId) // Expose the chain Ids of the steps you want to use as feeds for the rest
                 // This is the other way to split a chain. It is like a .Tap() but it executes a command that will pass the result to the plugged branches
                .SplitThrough(
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Use the analyzed 'User Intent' from the previous output along with the user input to write 3 variants of the user question that might enhance the expected response in terms of content detail", 
                            request.Settings), // <- suppose a creative settings configuration here to generate the variants
                        // Passing the PromptCommandRequest.Prompt wouldn't be necessary because the Splitted Command
                        // recieves the User Prompt as global context and the User Intent from the previous
                        // But you can add it to enforce the instruction and get better results
                        new StepSettings(new PromptCommandRequest($"- USER INPUT: {request.Prompt}")),
                        feedFwd: "You can add a guidance message here FROM the Splitted Command TO each plugged instruction / command"), 
                    [
                        new StepInstruction(
                            _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Answer the user query in general terms. Dont'be too specific, instead try to give a global overview of the topic the user is querying about and mention other relevant / related topics"
                        ),
                        //Here I'm supposing a fine-tuned model of your property aligned with your business rules and feeded with, maybe, your private enterprise data about stock market
                        // You can also add a rebujito of sources at step level if necessary. Here I'm simulating a different set of data sources that
                        // you can store with a key that will be used as an instruction / guidance message for all the dataset to help the LLM to use the sources properly according to its instruction
                        new StepSettings(new PromptCommandRequest(request.Prompt, model:"your-fine-tuned-secret-model"))
                            .WithDataBoost("<instruction / context for the source>", ["source1", "source2", "sourceN..."])
                        ),
                        new StepInstruction(
                            _factory.GetSourceable<VectorSearchSourceable>(),
                            new StepSettings(new VectorSearchRequest(
                                index: "stock-data-2026",
                                query: request.Prompt,
                                embedder: "nomic-embed-text",
                                dimensions: 512,
                                results: 10)
                            )
                        )
                    ]
                )
                // Boost ALL COMMANDS in the step with the same external source (otherwise use .Then().Tap() and feed the instructions separately):
                // You can add a mix of data from one or many sources to the step to help the LLM in its generation.
                // Add a nice rebujito of sources to ALL the instructions in the SplittedStep to give them the same context despite their different tasks
                .WithRebujito(
                    // In this case the example simulates a web search about the topic that might be relevant to ALL processess in the step (splitted AND branches)
                    await _langSearch.SearchRankedTexts(new RankedPageRequest { Count = 3, Query = "Market trends 2026" }, returnSnippet: false),
                    guidance: "Use the below data to do X (here you can add a guidance message for the LLM that will be fed into the context window of each request)", 
                    feedDose: 800 // Also, you can clamp each source in the list to a specific string size to avoid bloating the context window (Note: this chops every source without any content check)
                 ) 
                .Join(new StepInstruction(_factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                          instruction: request.Instructions.Skip(4).First().SystemMessage,
                          request.Settings), // <- and suppose a very focused settings configuration to prevent the LLM adding new content to the summarization
                      // You can also FEED Steps through the INDIVIDUAL SETTINGS (in case you want to feed a specific instruction in a SplitterStep, for example)
                      new StepSettings(new PromptCommandRequest(""))
                        .FeedFrom(thenId)) // Feed from the FIRST STEP output
                        
                 )
                 .ThenExecuteAsync(withFinalMessage: true, withReplay: true);


            // Example of multiple piped commands with different feeds and SingleThrowStep data boost
            chainResult = await LameChain
                .StartWith(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Analyze the user input and extract the intent. Write a brief explanation of what is the user querying about"
                    ),
                    new StepSettings(new PromptCommandRequest(request.Prompt))
                ),
                defaultSettings: request.Settings,
                finalSysMessage: "Analyze the content of the previous output and rewrite it as if you were <<INSERT COMPLIANT CORPORATIVE AI ASSISTANT PROFILE HERE>>",
                chainIntent: "Analyze and provide a report about market stock data")
                .ExposeThisId(out var firstId)
                .SplitThrough(
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Use the analyzed 'User Intent' from the previous output along with the user input to write 3 variants of the user question that might enhance the expected response in terms of content detail",
                            request.Settings),
                        new StepSettings(new PromptCommandRequest($"- USER INPUT: {request.Prompt}")),
                        feedFwd: "You can add a guidance message here FROM the Splitted Command TO each plugged instruction / command"),
                    [
                        new StepInstruction(
                            _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Answer the user query in general terms. Dont'be too specific, instead try to give a global overview of the topic the user is querying about and mention other relevant / related topics"
                        ),
                        new StepSettings(new PromptCommandRequest(request.Prompt, model:"your-fine-tuned-secret-model"))
                            .WithDataBoost("<instruction / context for the source>", ["source1", "source2", "sourceN..."])
                        ),
                        new StepInstruction(
                            _factory.GetSourceable<VectorSearchSourceable>(),
                            new StepSettings(new VectorSearchRequest(
                                index: "stock-data-2026",
                                query: request.Prompt,
                                embedder: "nomic-embed-text",
                                dimensions: 512,
                                results: 10)
                            )
                        )
                    ]
                )
                .ExposeThisId(out var splitId)
                .Pipe( // Pipe will execute the instruction command over each output from the prevous step, producing an equal number of outputs until you .Join() the subchains
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore. Ensure it is self-consistent (be sure you use any feeded data from the previous output",
                        request.Settings),
                    new StepSettings(new PromptCommandRequest("")),
                    pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile."
                )
                .Pipe( // This will keep processing each output produced in the splitter but will use the content of the first step (SingleThrow type) and the splitter (whose content is applied by default to the next, but in case you want to reuse it in this pipe, this is how)
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the output so far, the input and the goal or expected output and modify it if necessary to ensure it is adapted to the provided game lore. Ensure it is self-consistent (be sure you use any feeded data from the previous output",
                        request.Settings),
                    new StepSettings(new PromptCommandRequest("")),
                    pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile."
                )
                .ChainFeedsFrom([firstId, splitId]) // Feed the pipe from a previous SingleThrow / Splitted step
                .Join(new StepInstruction(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "",
                            request.Settings
                        ),
                        new StepSettings(new PromptCommandRequest(""))
                    )
                )
                // You can add rebujitos to SingleThrow steps too.
                .WithRebujito(
                    await _langSearch.SearchRankedTexts(new RankedPageRequest { Count = 3, Query = "Market trends 2026" }, returnSnippet: false),
                    guidance: "Use the below data to do X (here you can add a guidance message for the LLM that will be fed into the context window of each request)",
                    feedDose: 800
                 )
                .ThenExecuteAsync(withFinalMessage: true, withReplay: true);

            // In this example an API response is being simulated using the chain final 'user-friendly' message but for debugging
            // you should work with the ChainResult with the replay in it
            return new ChatPrompt
            {
                Model = request.Settings.Model ?? "app-default",
                Input = request.Prompt,
                Output = chainResult.Json,
                ChatHistory = [chainResult.SystemInstructions, new ChatMessage(ChatRole.User.ToString(), request.Prompt), chainResult.Message]
            };
        }


        /// <summary>
        /// This example simulates a Character creation pipeline for videogames, scripts, books. 
        /// LameChain can be used as a tool for creators by easying the creative process with well defined
        /// workflows that can generate concepts according to the user needs.
        /// 
        /// Ideally you would refine much more the data sources that are being used to bias the style, and surely load long well-structured system messages
        /// from DB for every step or using more complex annotated models to get more detailed step outputs, but this is just a proof-of-concept
        /// 
        /// The example demonstrates how to:
        ///     - start a chain from a request DTO
        ///     - split it using individual feeds for the substeps 
        ///     - using different commands (AtomicValueCommands) in parallell
        ///     - usage of FeedForward messages
        ///     - continueing / pipeing a splitted chain applying a rebujito of sources on each branch to influence the LLM generation.
        ///     - using a JunctionStep to .Join() the branches using a summarizing instruction
        ///     - use the 'user friendly' message to return a final message based on the chain result to continue chatting with the user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ChainResult> ParallelChainExample(ChainedPrompt request)
            => await LameChain
                .StartWith(new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Generate a character name and assign it an age (the age might be a specific number or a rough string approximation).", //This should be the 'request.SystemMessage'
                            settings: request.Settings),
                        settings: new StepSettings(new PromptCommandRequest(
                            message: "Generate a character for a game ambiented in Spain in the XVI century", // This should be the 'request.Prompt'
                            guidanceMessage: "You are a character concept creator for a videogames company" // bias the output of the first step by assigning it a role that makes it an expert in the topic.
                        )),
                        feedFwd: ""),
                    defaultSettings: request.Settings, // default settings for all the chain. If you don't pass individual settings to a command, this will be used instead
                    finalSysMessage: "Ensure you output your answer in old castillian spanish style, but be consistent with the provided context data.", //This is just to demonstrate how to use the Final Message. Let's say you are doing some tests about how should the character speak in the game
                    chainIntent: "Create game character") // This will be passed to all steps so every LLM request has a clear idea of what is the final task / overall goal
                .ExposeThisId(out var startId)
                .Then(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Reduce the previous output to ensure it only contains the requested name and age, remove the rest",
                        request.Settings), // pass indiviual settings for each step if necessary to regulate how focused or creative is the LLM
                    new StepSettings(new PromptCommandRequest("")),
                    feedFwdInstruction: "Use the name and the age to develop your part of the character") // Help the next step with its task by passing 'Feed Forward' messages that will be added to the context along with the previous output.
                .ExposeThisId(out var thenId)
                .SplitThrough(
                //This step will split the chain but executing a command first that will feed every plugged substep.
                // The splitted step recieves the previous output (ideally only the name and age after the reduction) and generate some kind of descriptive 'picture' of a possible character that will be passed
                // to the parallel branches for further post-processing.
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Generate a succinct 'iconic moment' for the character, that is: a description of a typical situation or scene for this character, a moment that should represent its nature and the way it is. Use no more than 50-60 words",
                            settings: request.Settings), // You could pass some very creative settings in this step for example
                        settings: new StepSettings(new PromptCommandRequest(message: "")),
                        feedFwd: "Use this character typical scene as a character concept to inspire your creations"), [
                            //Based on this little character concept (that inherits the name and age) the other steps will pick a faction, a game location as hometowm and generate an extensive background story to use it as a base for the final result (that will merge the three outputs)
                        new StepInstruction(
                            _factory.GetStringChoiceCommand( //You can use different type of Lame Commands in the chain. Here I'm using an AtomicValue Command that selects a single string from the passed list based on the given instruction
                                guidanceMessage: "Select a faction from the available list for the game character you are creating. Use the provided data to select the faction that fits best or choose at random if none stands out."),
                            new StepSettings(new StringChoiceRequest(
                                choices: ["Germaners", "Comuneros", "Tercio Imperial" ],
                                message: "Select a game faction for the character", // Enforce the instruction if you get hallucinations with dumb models or maybe the context adds too much noise and misleads the LLM
                                model: null))
                                .FeedFrom(startId), // This is just to demonstrate an individual feed in a splitted step from a previous process different than the previous
                            feedFwd: "Use this game related data to ground your profile to the game lore"), // Help the next worker focus on its task when you start to add too much content to the LLM's context window (for example joining three outputs, like in the next step)
                        new StepInstruction(
                            _factory.GetEnumChoiceCommand<EGameLocations>( // You can use other type of AtomicValue command for quick selections based on your app code. values  Here I'm using an AtomicValue command that selects an app enum value based on the instruction.
                                guidanceMessage: "Select a game location from the available list for the game character you are creating. Use the provided data to select the location that fits best with the profile."),
                            new StepSettings(new PromptCommandRequest(message: "Select a game location for the character as stated in your instruction")),
                            feedFwd: "Use the selected location as the character's place of birth"), // Every parallel branch can add its own Feed Forward message to help the next step to understand / use the output of its instruction
                        new StepInstruction(
                            _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                                instruction:"Generate a background story or profile for the character you are creating. Use the provided data to figure out what kind of character my ne appropriate in terms of style, mood, vibe..."),
                            settings: new StepSettings(new PromptCommandRequest(message: ""))
                                .WithDataBoost( // Steps can be boosted by feeding the output of previous steps but also by adding external string sources.
                                // In this case I'm adding semi-random data to the bias the generated character base profile towards specific styles.
                                // But ideally you would add here well processed sources (in this example it could be real human-made character concepts made by the game studio artists
                                    "Use the below data to bias your final response towards that style, ambience, topic or vibe",
                                    await _langSearch.SearchWebTexts(new WebSearchRequest("Don Pablo o La vida del buscón. Lazarillo de Tormes, sinopsis.", 5), returnSnippet: false, resultsClamp: 100)),
                            feedFwd: "Use this profile as an inspiration for your final character profile, but adapt it to the game lore") // Guide the refining / summarizing step to leverage the output of the different branches and get more consistent results
                    ])
                // Now that the chain has been splitted in 3 branches, it is possible to work on each branch indepently by piping commands that will be executed on each recieved previous output
                // This part of the example tries to demonstrate how to use another step to post-process the generated outputs and get more consistent results by generating
                // some semi-random content that is based on the generated content so far (the goal is to augment the previous results and have more base material to work with in the joining step)
                // Note that now every FeedForward message from the splitter will be 'spent' in this step, you have to use the pipeFeedFwd message to guide the next one
                .Pipe(
                    _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                        instruction: "Review the content so far and combine it with the provided sources in a very short story plot (around 50 words)",
                        settings: request.Settings),
                    pipedSettings: new StepSettings(new PromptCommandRequest(message: "")),
                    pipeFeedFwd: "Review all the sources and get a consistent overview of the expected character profile.")
                // You can add rebujitos of data to influence the outputs of each branch of the pipe if you need.
                // In this case I'm simulating more style biasing without filtering it, but ideally you would add business content that you would like to apply on each branch.
                // In this example, it could be more content produced by the game studio staff (like scene scripts or even quest scripts, the idea
                // is to add get results that are aligned with the game lore so the final junction step does not hallucinate and add content from it's training dataset
                .WithRebujito(
                    await _langSearch.SearchWebTexts(new WebSearchRequest("Revuelta de los Comuneros. Rebelion de las Germanias", results: 3), returnSnippet: false),
                    guidance: "Use this data as a source of style references and add merge them in your final response along with the generated game lore. Output your response in always in English despite the source language.", // It is possible to add guidance instruction about the rebujito content to help the lLM to use it
                    feedDose: 100)
                .Join(
                    new StepInstruction(
                        _factory.GetAsJsoneable<MessagePromptCommand, ChatMessage>(
                            instruction: "Generate a full character profile (400-500 words) with the provided data. Include a background story, a psychological profile and a 'usual routines' section"),
                        settings: new StepSettings(new PromptCommandRequest(message: "Generate the requested character profile")) // Enforce the instruction (specially if you are using small local models and the context window is relatively filled)
                            .FeedFrom(thenId) // Feed the name and age to ensure it uses the one generated at this step (dumb models may have alucinated with the background story and the rebujito feeds)
                    )
                )
                .ThenExecuteAsync(request.WithFinalMessage, request.WithReport, request.FinalMessageSettings ?? request.Settings);
                // You can request a final 'user-friendly' message that would make use of the chain output to generate the response, but
                // allows individual guidancemessage and settings allowing more flexibility in the usage
                // For example, if this was the beginning of a game conversation with an NPC and the last Joining step was a CharacterModel.cs with many properties defining a detailed game character
                // then you could make use of the final 'user-friendly' message to start the conversation with the newly created NPC using the rich CharacterModel.cs as context.
                // Because the ChainResult includes both, you could then return the final ChatMessage and store the CharacterModel in DB from the client app.

        // CONDITIONAL STASH OF A SOURCEABLE COMMAND
        // So far I have provided examples of sequencing, parallelizing and feeding steps. In every step
        // One or many commands were executed and their output passed to the next and optionally to any other
        // if a feed is configured but, what if you want to just store data at some point in the chain to feed multiple steps with it?
        // And not what if that data might not be necessarily the product of a LLM request but maybe the product of a web search or a DB query?
        // Moreover, what if you want to create that data source IF a certain condition is met after an LLM evaluation?
        // Well, you can solve all that situations with LameChain StashedSteps and ConditionalSteps.
        // You can use a StashedStep to store the result of any SourceableCommand (that is a command that returns always a List<string>)
        // And then configure some feeds to route the results through the chain.
        // Also, you can use the .StashIf() extension method to set a ScoredBoolCommand that will evaluate something in the chain prompt and
        // return a scored bool with a justification. Depending on the bool the stash branch will be inserted into the chain and created
        // otherwise will continue the chain passing the justification comment to the next so the chain is awared of what's happening in the process
        //
        // Here is a very simple example:
        public async Task<ChainResult> StashExample(ChainedPrompt request)
            => await LameChain
                .StartWith(new StepInstruction(
                    // Start by analyzing the user intent with a Lame Core Command
                    // This is very useful to help the chain understand what is the user trying to do or querying about.
                    // Together with the 'chainIntent' and the '_runner.UserPrompt' this is the best way to
                    // capture the general goal and keep it as global context along the chain
                    _factory.GetCommand<UserIntentCommand, ChatMessage>(systemMessage: "Try to be as specific as possible when defining the user intent", request.Settings), //You can help the LLM to generate your required outputs by passing more guidance messages 
                    new StepSettings(new PromptCommandRequest(request.Prompt)), // pass the prompt to analyze
                    feedFwd: "Use the definition of the intent to understand better what is the user trying to do and use the information in your deliverances"), // enforce the analysis of the intent in the scored bool command to help the LLM decide if it is necesary to retrieve data from previous chats to answer the user
                    request.Settings, // Default settings for all the cahin
                    finalSysMessage: "", // No final message required (the last step is a Chat command afterall)
                    chainIntent: "Conversation with the user with recurrent access to a memories database")
                .StashIf(new StepInstruction(
                        _factory.GetCommand<ScoredBoolCommand, ScoredBoolResponse>(systemMessage: @"Analyze the user input and any other provided support data and determine if is necessary to retrieve previous chat conversations that the system has stored. 
Note if the user is making references to past conversations with you or events of which you are supposed to be aware of. Check if the user is speaking in past or present, including or mentioning you or speaking in a way that makes you think that you should be aware of some information that is not present in the input"),
                        new StepSettings(new PromptCommandRequest($"Analyze the user input and reason if it would be necessary to review previous conversations to answer the user or it not. User Input: {request.Prompt}"))), // This would not be necessary because the instruction is clear, but it is usefult for local dumb models
                    stashInstruction: // Add the stash. In this case is a VectorSearchSourceable that will return a list of chat chunks sorted by distance to the query
                        new StepInstruction(
                            _factory.GetSourceable<VectorSearchSourceable>(),
                            new StepSettings(new VectorSearchRequest(
                                index: "UserName-AgentProfileName", // hypothetic chat collection where the session chats are stored
                                query: request.Prompt,
                                embedder: "nomic-embed-text",
                                dimensions: 512,
                                results: 4)
                            ),
                        feedFwd: "Use this fragments of conversation to get a better understanding of the current user input and provide a more consistent response"),
                    // This are the only 'rules' for a stash:
                    isGreedy: true, // Greedy = the next step WON'T read the stash as it does with the other steps (this is because you want to configure every stash feed manually)
                    isIsolated: true) // Isolated = the stash WON'T read the previous output as the other steps do (this is because you don't want to add noise to the context of the sourceable command or because you just don't need it, like in a VectorSearchSourceable that requires only the user Prompt
                .Then(_factory.GetMessagePromptCommand(
                    systemMessage: "You are a helpful assistant. Chat with the user in a natural way and use any provided data about previous conversations to offer a more contextualized response to the user"), // settings here are the same that came in the request and passed as Default 
                    new StepSettings(new PromptCommandRequest(request.Prompt))) // pass the user input to the final chat step, that will answer to any "new topic" prompt without checking the chat history or perform a DB search to retrieve data to add it to the context in the final step
                //In a real use case you would create for example a PersistentChatCommand that makes the LLM request, updates the DB chat history and returns the result to make the loop work
                .ThenExecuteAsync(withFinalMessage: false, withReplay: true);

        // STASH A SOURCEABLE WITH SUBPROCESSES (instantiates the right type, chains N steps and forwards the firs step with type check)
        // The previous example was a minimal demonstration of the usage of ConditionalSteps and StashedSteps but it was more a conceptual example than
        // a real use case. The example below works (as long as you have a Estrada.ChromaDB.Repository with two collections well taged and descripted)
        // It also demonstrates how add a subchain with multiple steps to the code and how you can use different Sourceables that generate their stash
        // using an LLM. This Lame Chain executes the next process:
        // 1 - Evaluate the user intent to get a clear statement defining what is the user trying / querying about to help the next LLM decision with it
        // 2 - Evaluate if the intent is related to any of the provided db collections and justify the answer (this will output something like 'yes, it is related to X collection' and will help the SmartQuery decide too)
        // 3 - Generate a response without 'reliable' DB data to increase the similarity points that match the query topic in general terms
        // 4 - Generate N copies of the user input to do the same
        // 5 - Execute a SmartQueryCommand that will select the best db collection to query from based on the user input, with the additional support of the ScoredBool justification comment that enabled the subchaina
        //     Then with the contents of the stashes that contain the augmentation texts it performs a query in the DB to data from reliable (or user selected) sources to use them as a source in the final step (this steps is stashed too)
        // 6 - Finally, the RagCommand executes the final LLM request using the reliable DB data if it was potentially relevant for the answer or generate the response with the 'built-in' LLM knowledge from the training dataset
        public async Task<ChainResult> SmartRagChain(SimpleCommandRequest request)
            => await LameChain
                .StartWith(new StepInstruction(
                    _factory.GetCommand<UserIntentCommand, ChatMessage>(systemMessage: "", request.Settings),
                    new StepSettings(new PromptCommandRequest(request.Prompt)),
                    feedFwd: "Use the generated User Intent to guide you in your task"),
                    request.Settings, //Default Settings for all the chains
                    finalSysMessage: "", // There is no need to fill this since there is no user final message required
                    chainIntent: "")
                .StashIf(new StepInstruction(
                    _factory.GetCommand<ScoredBoolCommand, ScoredBoolResponse>(
                        systemMessage: $"Your task is to determine whether the provided User Intent is related to any of the Collections of the list below"),
                    new StepSettings(new PromptCommandRequest(""))
                        .WithDataBoost("AVAILABLE COLLECTIONS:", await getChromaCollectionChoices(withChatCollections: false)),
                    feedFwd: "Use this data as a reliable source to answer the user"),
                    trueBranch: LameChain.SubChainWith<StashedStep>(
                        new StepInstruction(
                            _factory.GetSourceable<RagExpansionCommand>(),
                            new StepSettings(new RagExpansionRequest(expansions: 1, request.Prompt))
                        ),
                        false, //TODO: StashSettings : StepSettings & StepWithCustomParamsSettings : StepSettings y quitar esta guarrada
                        true
                     )
                    .ExposeThisStep(out var ragExpansion)
                    .Stash(new StepInstruction(
                        _factory.GetSourceable<QueryAugmentationCommand>(),
                        new StepSettings(new RagExpansionRequest(expansions: 3, request.Prompt, withFewShot: true, 2))),
                        out var queryAugmentId,
                        isGreedy: true,
                        isIsolated: true)
                    .Stash(new StepInstruction(
                        _factory.GetEmbeddedSourceable<SmartQuerySourceable>(
                            _chromaService.SimilaritySearch,
                            llamaGuidance: "Select ONLY the 'COLLECTION NAME' value of the provided list OR empty list if there are no collections relevant for the user query."
                        ), // appended to Core Message (default | db)
                        new StepSettings(
                            new SmartQueryRequest(
                                request.Prompt,
                                collectionChoices: await getChromaCollectionChoices(withChatCollections: false), // add a formated catalogue of DB collections with descriptions and topics to help the LLM decide
                                maxChoices: 1,
                                resultsPerChoice: 3,
                                guidanceMessage: string.Empty, // this is overwritten by prev_ctx so leave it empty
                                dimensions: 512,
                                model: "nomic-embed-text",
                                filters: null),
                            withFullContext: false,
                            withPrevSchema: false
                            )// A nested feed is a feed that should go to a sub step or a sub command inside a step
                             // In this case Im passing the result of the ScoredBool (with the justification comment) to the MultiChoice command that selects the best available collections.
                             // This will help the LLM decide and will retur more accurate results when there are overlapping collection topics
                            .WithNestedFeed(nameof(MultiChoiceCommand), [ragExpansion.WhoIsPrevious], isForStep: false) //This is the only way of feeding a subranch nested COMMAND (not a step substep) from the owning step
                        ), 
                        out var smartQueryId)
                    // Feed the VectorSearch command (which is the 'main' command of the SmartQueryCommand)
                    // with the expansion stashes
                    .ChainFeedsFrom([ragExpansion.GetRunnerId, queryAugmentId])
                    // At this point you are working with the last subchain step
                    // so you have to finish the subchain by passing the first step to be linked properly with the ConditionalStep
                    // use this method to do that and cast it to the right type (this feature is still under development)
                    .ForwardFirstType<StashedStep>() 
                 )
                .Then(_factory.GetCommand<RagQueryCommand, ChatMessage>(systemMessage: request.SystemMessage),
                      new StepSettings(new ChatCommandRequest(request.Prompt, request.SystemMessage)))
                .ChainFeedsFrom([smartQueryId]) // retrieve the output of the SmartQueryCommand that made the query with the expansions
                .ThenExecuteAsync(withFinalMessage: false, withReplay: true);

        private async Task<List<string>> getChromaCollectionChoices(bool withChatCollections = false)
        {
            var formattedChoices = new List<string>();

            var collections = await _chromaService.GetAllFileCollections();

            collections.ForEach(collection => formattedChoices.Add(getFileCollectionRagText(collection)));

            if (withChatCollections)
            {
                var chats = await _chromaService.GetAllChatCollections();

                chats.ForEach(chat => formattedChoices.Add(getChatCollectionRagText(chat)));
            }

            return formattedChoices;
        }

        private string getFileCollectionRagText(ChromaFilesCollection collection, string itemSplitMark = null)
        {
            var sb = new StringBuilder();

            sb.Append(string.IsNullOrEmpty(itemSplitMark) ? "" : itemSplitMark)
                  .AppendLine()
                  .AppendLine($"## COLLECTION NAME = {collection.Name}")
                  .AppendLine(string.IsNullOrEmpty(collection.Description) ? "" : $"\n> {collection.Name} DESCRIPTION: {collection.Description}")
                  .Append($"> {collection.Name} TOPICS: ")
                  .Append(collection.GetMeta<FileCollectionMetadata>().TOPICS);

            return sb.ToString();
        }
        private string getChatCollectionRagText(ChromaChatsCollection collection)
        {
            var sb = new StringBuilder();

            sb.Append("\n\n- COLLECTION NAME =  ")
              .Append(collection.Name)
              .Append($" - {collection.Description}\n");

            return sb.ToString();
        }

        // COMMANDS
        /// <summary>
        /// Instantiates and executes a command that returns TResult
        /// This is the most basic usage of Lame Commands. It has no DB dependency
        /// and no dedicated command request. It makes use of the request SystemMessage and 
        /// the request prompt to return a simple ChatMessage
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ChatMessage> GuidedPromptCommand(SimplePromptRequest request)
        {
            var command = _factory.GetCommand<GuidedStructuredPrompt<ChatMessage>, ChatMessage>(request.SystemMessage, request.Settings);

            return await command.Prompt(new PromptCommandRequest(request.Prompt, request.Settings.Model));
        }

        /// <summary>
        /// This example demonstrates how to use Lame Commands to perform actions that do not require an LLM
        /// VectorSearchCommands are commands that require an Embeddings Generator to work and they are meant
        /// to query VectorDatabases 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<List<ILameSearchResult>> VectorSearchCommand(VectorSearchRequest request)
        {
            var command = _factory.GetSimilaritySearchCommand(_chromaService.SimilaritySearch);

            var commandRequest = new VectorSearchRequest(index: request.QueryIndex, query: request.Prompt, embedder: request.Model, dimensions: request.Dimensions, results: request.ReturnedResults);

            return await command.Prompt(commandRequest);
        }

        /// <summary>
        /// This is a Sourceable version of a VectorSearch command. Sourceables are necessary to create StashedSteps so
        /// you need to create a Sourceable version of your VectorSearchCommands
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<List<string>> VectorSearchSourceable(VectorSearchRequest request)
        {
            var command = _factory.GetVectorSearchSourceable(_chromaService.SimilaritySearch);

            var commandRequest = new VectorSearchRequest(index: request.QueryIndex, query: request.Prompt, embedder: request.Model, dimensions: request.Dimensions, results: request.ReturnedResults);

            return await command.Prompt(commandRequest);
        }

        /// <summary>
        /// This are Commands that read their Core Message from a database using a lambda function that must accept two string input parms and return a Task<string> that will retrieve 
        /// the system message on chain execution
        /// 
        /// The example demonstrates how you can use an AtomicValue command with a custom DB instruction that gives you better results 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ChatMessage> DbPromptCommand(SimplePromptRequest request)
        {
            var command = _factory.GetDbCommand<ScoredBoolCommand, ScoredBoolResponse>(source: "", messageName: "", _chromaService.GetSystemInstruction, request.SystemMessage, request.Settings);

            var response = await command.Prompt(new PromptCommandRequest(request.Prompt, request.Settings.Model));

            return new ChatMessage(ChatRole.Assistant.ToString(), $"{(response.Answer ? "YES" : "NO")}: {response.Justification}");
        }

    
        /// <summary>
        /// You may need for some commands the capability of being able to work with or without a DB message. In those cases
        /// You can use DBPromptCommands with DefaultMode and add a default instruction in the command and / or use them with request guidance messages
        /// To do that, just null the DB params and the command will try to build the system message with the default properties
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ChatMessage> DbPromptCommandDefaultCompatible(SimplePromptRequest request)
        {
            //This will force the command to look for a default hardcoded message instead of trying to get the instruction from DB. In case there is not a default message, it can work with the 'command guidance' and the 'request guidance' messages
            var command = _factory.GetDbCommand<ScoredBoolCommand, ScoredBoolResponse>(source: null, messageName: null, retrieverLambda: null, guidanceMessage: request.SystemMessage, settings: request.Settings);

            var response = await command.Prompt(new PromptCommandRequest(request.Prompt, guidanceMessage: "This can be used to compose the system message too", request.Settings.Model));

            return new ChatMessage(ChatRole.Assistant.ToString(), $"{(response.Answer ? "YES" : "NO")}: {response.Justification}");
        }


    }
}
