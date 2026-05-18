using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System.Text.Json;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public abstract class ChainStep : IChaineable
    {

        public delegate void OnRunNotify(ForgeLog log); //communicate / persist data during execution
        public event OnRunNotify onRunNotify;

        public delegate void OnReportReplay(ReplayLog log);
        public event OnReportReplay onReportReplay;

        protected readonly Guid _id;
        protected readonly ForgeLog _forgeLog;
        protected IChaineable _next;
        protected IChaineable _prev;
        protected string preInstructionTag = "# INSTRUCTION: ";
        protected string? _promptedInstruction = null; // The prompted instruction for this step. Serves as a log and also to generate the feedForwardInstruction (provide context about prev step). Null if !Executed
        protected string? _feedForwardInstruction = null;
        protected PromptCommandRequest _request;
        protected List<string> _instructionsLog = new List<string>();
        protected List<IJsoneable> _commands = new List<IJsoneable>();
        protected List<ChainLink> _outputs = new List<ChainLink>();
        protected ChainRunner? _runner = null;
        protected DateTime _passCatchTimestamp;

        public Guid Id => _id;
        public IChaineable Next => _next;
        public IChaineable Previous => _prev;
        public List<IJsoneable> Commands => _commands;
        public List<ChainLink> Outputs => _outputs;
        public string Input => _request.Prompt;
        public bool IsRunning => _runner != null;
        public string? FeedForwardInstruction => _feedForwardInstruction;
        public string? PromptedInstruction => _promptedInstruction;
        public List<string> InstructionsLog => _instructionsLog;
        public bool IsMultiSocket => this.GetType() == typeof(SplitterStep) || this.GetType() == typeof(PipedStep);
        public bool IsForged => _outputs.Count > 0;
        // Hail the Trinary Boolean, 3 options for the price of 2!
        // (remember those 'Y / N / IDK' questions?)
        public bool IsChained(bool? isForwardCheck = true)
            => isForwardCheck == null ? _next != null || _prev != null : isForwardCheck.Value ? _next != null : _prev != null;

        public bool IsFirstStep()
            => IsChained(isForwardCheck: null) && _prev == null;

        //Checks that _cmds.Count > 0 & the input type of step to ensure that the pieces match (single from single, collector from multi or pipe, multi from single, pipe from multi or pipe
        public abstract bool CanBeForged(IChaineable previous);
        //protected abstract Task forgeLink(IChaineable previous); // this makes the request and add the resulting link to the outputs list (allows multi-socket)
        public ChainStep() 
        { 
            _id = Guid.NewGuid();
            _forgeLog = new ForgeLog(_id, _feedForwardInstruction);
        }

        public ChainStep(PromptCommandRequest request, string? feedForwardMessage = null) : this() 
        { 
            _request = request; 
            _feedForwardInstruction = feedForwardMessage;
        }
        public void Link(IChaineable step, bool isForward, bool isTwoWay)
        {
            if (isForward)
                _next = step;

            else _prev = step;

            if (isTwoWay)
                step.Link(this, isForward: !isForward);
        }

        public IChaineable GetFirstStep() => _prev != null ? getFirstStep(_prev) : this;
        public Dictionary<Guid, List<ChainLink>> GrouppedOutputs()
        {
            var result = new Dictionary<Guid, List<ChainLink>>();

            _outputs.GroupBy(link => link.StepId)
                    .Select(group => new { group.Key, Value = group.ToList() })
                    .ToList()
                    .ForEach(group => result.Add(group.Key, group.Value));

            return result;
        }

        public ChainRunner PassRunner()
        {
            var reference = _runner;
            _runner = null;
            return reference;
        }
        public virtual async Task<ChainResult> ExecuteChainAsync(bool withUserFriendlyMessage = true) { throw new NotImplementedException(); }

        public abstract Task<IChaineable> Forge(IChaineable previous);

        protected virtual void submitForgeLog()
        {
            if (!IsRunning) return;
            // the other forge log data is setted up on construction
            // or at runtime, when the value is generated (json result, prompted instruction...)
            // this is just a centralized way to submit the log to the ChainRunner object

            if (_prev != null)
                _forgeLog.PrevId = _prev.Id;
            
            if(_next != null)
                _forgeLog.NextId = _next.Id;

            onRunNotify(_forgeLog);
        }
        public SingleThrowStep PassTo(bool swapRunner, IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"{nameof(ChainStep)} >> {nameof(PassTo)} >> This method is to instantiate steps with a copy of the chain runner and the current caller is not the runner");

            // la mitica cruz de tres cuartos
            var newRunner = swapRunner ? _runner.Clone() : _runner;

            //every step of this sub chain gets a new nullable runner with the previous log, but they subscribe to their own runner
            // this is to run chains in parallel. The last runner of each subChain has the report for the original Multithrow Step so they can be appended
            // to the original / main runner and deleted
            return Activator.CreateInstance(typeof(SingleThrowStep),  command, newRunner, request, feedForwardInstruction) as SingleThrowStep;
        }
        public SingleThrowStep ExpandTo(IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null)
            => Activator.CreateInstance(typeof(SingleThrowStep), command, request, feedForwardInstruction) as SingleThrowStep;
        public SingleThrowStep ExpandTo<TCommand, TResult>(string instruction, PromptCommandRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new()
            => ExpandTo(Activator.CreateInstance(typeof(TCommand), Commands.First().BorrowLlama, instruction, request.Settings) as IJsoneable, request, feedForwardInstruction);

        public TStep ExpandTo<TStep>(IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null) where TStep : ChainStep
            => Activator.CreateInstance(typeof(TStep), command, request, feedForwardInstruction) as TStep;
        public TStep ExpandTo<TStep>(StepInstruction command, PromptCommandRequest request) where TStep : ChainStep
            => Activator.CreateInstance(typeof(TStep), command, request) as TStep;
        public TStep ExpandTo<TStep, TCommand, TResult>(string instruction, PromptCommandRequest request, string? feedForwardInstruction = null)
            where TCommand : BasePromptCommand<TResult>, new()
            where TStep : ChainStep
                => ExpandTo<TStep>(Activator.CreateInstance(typeof(TCommand), Commands.First().BorrowLlama, instruction, request.Settings) as IJsoneable, request, feedForwardInstruction);

        public SplitterStep Plug(List<StepInstruction> instructions, PromptCommandRequest request, string? splitterFeedFwd = null)
          => Activator.CreateInstance(typeof(SplitterStep), instructions, request, splitterFeedFwd) as SplitterStep;
        public SplitterStep SplitTo(StepInstruction splitted, List<StepInstruction> instructions, PromptCommandRequest request)
          => Activator.CreateInstance(typeof(SplitterStep),  splitted, instructions, request) as SplitterStep;
        public TDeserialized GetOutputAs<TDeserialized>() where TDeserialized : class
            => IsForged ? JsonSerializer.Deserialize<TDeserialized>(Outputs.First().SerializedResult) ??
                throw new InvalidOperationException($"Failed to deserialize JSON to type {typeof(TDeserialized).Name}") :
                throw new InvalidOperationException("The chain step has not been forged and has no output value");

        protected IChaineable getFirstStep(IChaineable current)
            => current.IsFirstStep() ? current : getFirstStep(current.Previous);

        protected void checkCanForge(IChaineable previous)
        {
            if (!CanBeForged(previous))
                throw new InvalidOperationException($"{nameof(SingleThrowStep)} >> {nameof(Forge)} >> An error has occured while forging the chain link. STEP CANNOT BE FORGED");

            if(!IsRunning)
                throw new InvalidOperationException($"{nameof(SingleThrowStep)} >> {nameof(Forge)} >> The current step has no runner object configured. STEP CANNOT BE FORGED");
            //It has ChainReport object (es el portador del balon en la jugada)
            // I is subscribed
        }

        protected async Task forgeLinkForPlug(IChaineable previous, int plugIdx = 0)
        {
           
            if (!IsFirstStep() && previous.IsForged)
            {
                // The guidance message is appended to the command final system message in this way: _systemMessage (optional. guidance. on constructor) + dbMessage (optional. main msg. on construction) | defaultInstruction(optional. main msg. hardcoded) + _request.Guidance
                _request.GuidanceMessage = guidanceMessageFrom(_request.Prompt, previous.PromptedInstruction, previous.Outputs.First().SerializedResult, previous.Outputs.First().SchemaForMessage(), previous.Outputs.First().GuidanceMessage);
                
                _instructionsLog.AddRange(previous.InstructionsLog);
            }

            await forgeLink(plugIdx);
        }

        protected async Task forgeLink(int idx = 0)
        {
            if (idx >= _commands.Count) return;

            // CreateLogWith:
            //  prev guidance esta en request
            //  main instruction lo pillo de jsonResult.Instruction
            //  preInstruction es tag pero podria se ina preInstruccion de usuario
            //  FEED NEXT _feedFwdMessag
            // Y lo guardo aqui hasta que chainResult haga onReport()
            // Es decir: se pasa el instructions log que es el step-by-step lite que ya funciona. sirve como sumary para Steps si hace falta.
            //           opcionalmente se puede pedir el reporte completo ordenado por fecha de ejecucion con todos los mensajes internos (StepLogs con instrucciones)
            
            //  COMO ME SUSCRIBO A UN EVENTO DE UN OBJETO QUE TODAVIA NO EXISTE??
            //      A: report chain -> el primero lleva el evento onReport. ExpandTo tiene que suscribir al EXPANDIDO al evento del anterior y asi sucesivamente --> OnReport va pasando el balon hacia atras (como es logico). Devuelve el balon + su log
            //          Cuando llega al ala ya tiene toda la info y es solo correr a la zona de try
            //      B: un tipo recibe todo y lo ordena segun triggerTimestamp (EL MOMENTO EN QUE SE DIO LA ORDEN, NO CUANDO SE CUMPLIO. Los dos timestamps son utiles
            //      C: true rugby: ChainReport ES el balon. El que lleva el balon es el primero siempre y corre palante
            //                     Execute = choque + pase y seguir corriendo
            //                     el siguiente recibe el balon y se suscribe (el balon lleva el evento) y sigue jugando
            //                     cuando el ultimo ensaya todo el mundo esta suscrito al balon
            //                     balon.Replay() <- collect logs <- cada jugador cuenta lo que ha hecho en la jugada
            //                    Como se pasa el balon? --> en el expand. Lo crea el primero y se va pasando como el instructionsLog
            var jsonResult = await _commands[idx].JsonPrompt(_request, returnFullInstruction: false, preInstruction: preInstructionTag); //Skip GuidanceMessage, return only instruction for this step

            _promptedInstruction = jsonResult.Instruction;

            _instructionsLog.Add(_promptedInstruction);

            _outputs.Add(new ChainLink(_id, jsonResult.RawJson, JsonSerializerOptions.Default.GetJsonSchemaAsNode(jsonResult.Type), jsonResult.Type, feedForwardMessage: _feedForwardInstruction));

            _forgeLog.ForgeTimestamp = DateTime.Now;
            _forgeLog.JsonResult = jsonResult.RawJson;
            _forgeLog.CommandInstruction = _promptedInstruction + (string.IsNullOrEmpty(_request.GuidanceMessage) ? "" : $"\n{_request.GuidanceMessage}");
            _forgeLog.Prompt = _request.Prompt;
        }

        protected string guidanceMessageFrom(string prompt, string instruction, string serialized, string jsonSchema, string? outputGuidance = null)
         => @$"
# CONTEXT: A previous process on your current task has reported the next results for this user query and instruction, 
use this information if you find it relevant for your current task.

- PREVIOUS PROMPT: {prompt}
- PREVIOUS TASK: {instruction.Replace(preInstructionTag, "").Trim()}

- PREVIOUS OUTPUT:

{serialized}

- PREVIOUS OUTPUT SCHEMA:

{jsonSchema}

{(string.IsNullOrEmpty(outputGuidance) ? string.Empty : $"# PREVIOUS STEP REQUEST (this is what you are expected to do with the previous output data): {outputGuidance}")}";

        public bool hasCatchedPass(IChaineable previous)
        {
            _runner = previous.PassRunner();

            onRunNotify += _runner.OnRunnerNotification;
            _runner.onReplayRequest += SendReplay;

            checkCanForge(previous);

            _passCatchTimestamp = DateTime.Now;

            return IsRunning;
        }

        // override in Multi-Socket to include subchain results
        public void SendReplay()
        {
            //build report
            var report = new ReplayLog(_forgeLog);

            report.FeededMessage = _request.GuidanceMessage;
            report.PassCatchTimestamp = _passCatchTimestamp;

            onReportReplay(report);
        }
    }
}
