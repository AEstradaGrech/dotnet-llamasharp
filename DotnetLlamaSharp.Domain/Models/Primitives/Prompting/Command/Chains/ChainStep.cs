using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Schema;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ChainStep : IChaineable
    {
        private IJsoneable _command;
        private IChaineable _next;
        private IChaineable _prev;
        private bool _isPreloaded; 
        private string? _promptedInstruction = null; // The prompted instruction for this step. Serves as a log and also to generate the feedForwardInstruction (provide context about prev step). Null if !Executed
        private string? _feedForwardInstruction = null;   
        private PromptCommandRequest _request;
        public bool IsPreloaded => _isPreloaded;
        public string Input => _request.Prompt;
        public string? PromptedInstruction => _promptedInstruction;
        public List<string> InstructionsLog => _instructionsLog;

        private List<string> _instructionsLog = new List<string>();

        // Hail the Trinary Boolean, 3 options for the price of 2!
        // (remember those 'Y / N / IDK' questions?)
        public bool IsChained(bool? isForwardCheck = true)
            => isForwardCheck == null ? _next != null || _prev != null : isForwardCheck.Value ? _next != null : _prev != null;
        //{
        //    if(isForwardCheck == null) // CHECK BOTH
        //        return _next != null || _prev != null; 
            
        //    else // CHECK FWD || CHECK BWD
        //        return isForwardCheck.Value ? _next != null : _prev != null;
        //}
        public bool IsFirstStep()
            => IsChained(isForwardCheck: null) && _prev == null;

        public ChainLink OutputLink => _output;
        // -> !isPreloaded: ejecuta las cosas sin mas. es el comportamiento por defecto      
        // -> isPreloaded: sirve para cargar cadenas que se pueden:                        
        //          > Pasar como parametro a otras funciones (o agentes, supongo)
        //          > Serializar y guardar
        //          > Combinar (_pdfService.GetChunkingChain().Merge(_ragService.GetIngestionChain()).Merge(_svcX.SomeChain()) <- remove last step (if 'ThenFinish), pdfChain.LastStep.Then(ragChain.First())

        public ChainStep(IJsoneable command, PromptCommandRequest request, bool isPreloaded, string? feedFwdInstruction = null)
        {
            _command = command;
            _isPreloaded = isPreloaded;
            _request = request;
            _feedForwardInstruction = feedFwdInstruction;
        }

        private ChainLink _output;
        public IJsoneable Command => _command;
        public IChaineable Next => _next;
        public IChaineable Previous => _prev;

        private string preInstructionTag = "# INSTRUCTION: ";

        public void Link(IChaineable step, bool isForward, bool isTwoWay)
        {
            if (isForward)
                _next = step;

            else _prev = step;

            if (isTwoWay)
                step.Link(this, isForward: !isForward);
        }

        public async Task<ChainResult> ExecuteChain(bool withUserFriendlyMessage = true)
        {
            if(IsPreloaded)
            {
                var firstStep = getFirstStep(current: this);

                var finalStep = await firstStep.Forge(null);

                var typedResult = finalStep.OutputLink.SerializedResult;

                var finalMessage = new ChatMessage(ChatRole.Assistant.ToString(), string.Empty);

                if(withUserFriendlyMessage)
                {
                    var finalizer = ExpandTo<MessagePromptCommand, ChatMessage>(instruction: "Analyze the provided context data from the previous outputs and return the assistant answer (wich is in JSON string format) as a text to keep on chatting",
                        new PromptCommandRequest($"> Original user input: {firstStep.Input}", null, _request.Model));
                    
                    var jsonMessage = await finalizer.Forge(finalStep);

                    finalMessage = jsonMessage.GetOutputAs<ChatMessage>();
                }
               
                return new ChainResult(jsonResult: typedResult, finalStep.OutputLink.JsonSchema, chainInput: firstStep.Input, stepsLog: finalStep.InstructionsLog, processedResult: finalMessage);
            }

            return new ChainResult();
        }

        private IChaineable getFirstStep(IChaineable current)
            => current.IsFirstStep() ? current : getFirstStep(current.Previous);
        
        public async Task<IChaineable> Forge(IChaineable previous)
        {
            if(!IsFirstStep())
            {
                // The guidance message is appended to the command final system message in this way: _systemMessage (optional. guidance. on constructor) + dbMessage (optional. main msg. on construction) | defaultInstruction(optional. main msg. hardcoded) + _request.Guidance
                _request.GuidanceMessage = @$"
# CONTEXT: A previous process on your current task has reported the next results for this user query and instruction, 
use this information if you find it relevant for your current task.

- PREVIOUS PROMPT: {_request.Prompt}
- PREVIOUS TASK: {previous.PromptedInstruction.Replace(preInstructionTag,"").Trim()}

- PREVIOUS OUTPUT:

{previous.OutputLink.SerializedResult}

- PREVIOUS OUTPUT SCHEMA:

{previous.OutputLink.SchemaForMessage()}
";   
                if(!string.IsNullOrEmpty(previous.OutputLink.GuidanceMessage))
                    _request.GuidanceMessage += $"\n# PREVIOUS STEP REQUEST (this is what you are expected to do with the previous output data): {previous.OutputLink.GuidanceMessage}";

                _instructionsLog.AddRange(previous.InstructionsLog);
            }

            var jsonResult = await _command.JsonPrompt(_request, returnFullInstruction: false, preInstruction: preInstructionTag); //Skip GuidanceMessage, return only instruction for this step

            _promptedInstruction = jsonResult.Instruction;

            _instructionsLog.Add(_promptedInstruction);

            _output = new ChainLink(jsonResult.RawJson, JsonSerializerOptions.Default.GetJsonSchemaAsNode(jsonResult.Type), jsonResult.Type, feedForwardMessage: _feedForwardInstruction);

            return _next != null ? await _next.Forge(this) : this;
        }

        public IChaineable ExpandTo(IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null)
            => Activator.CreateInstance(this.GetType(), command, request, _isPreloaded, feedForwardInstruction) as IChaineable;

        public IChaineable ExpandTo<TCommand, TResult>(string instruction, PromptCommandRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new()
            => ExpandTo(Activator.CreateInstance(typeof(TCommand), _command.BorrowLlama, instruction, request.Settings) as IJsoneable, request, feedForwardInstruction);
        
        public TDeserialized GetOutputAs<TDeserialized>() where TDeserialized : class
            => JsonSerializer.Deserialize<TDeserialized>(_output.SerializedResult) 
                    ?? throw new InvalidOperationException($"Failed to deserialize JSON to type {typeof(TDeserialized).Name}");
    }
}
