using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public abstract class ChainStep : IChaineable
    {

        public delegate void OnRunNotify(ForgeLog log, bool appendLogs = true); //communicate / persist data during execution
        public event OnRunNotify onRunNotify;

        public delegate void OnReportReplay(ReplayLog log);
        public event OnReportReplay onReportReplay;

        public delegate void OnSupportAcknowledge(IChaineable id);
        public event OnSupportAcknowledge onSupportIncoming;

        protected readonly Guid _id;
        protected readonly ForgeLog _forgeLog;
                                                                      
        protected IChaineable _next;
        protected IChaineable _prev;
        protected string preInstructionTag = "# INSTRUCTION: ";
        protected string? _promptedInstruction = null; // The prompted instruction for this step. Serves as a log and also to generate the feedForwardInstruction (provide context about prev step). Null if !Executed
        protected string? _feedForwardInstruction = null;
        protected ChainPromptRequest _request;
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
        public ChainRunner? Runner => _runner;
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

        public ChainStep(ChainPromptRequest request, string? feedForwardMessage = null) : this() 
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

        public ChainRunner Drop()
        {
            
            var reference = _runner;
            _runner = null;
            return reference;
        }
        
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

            _forgeLog.CommandInstruction = _promptedInstruction;
            _forgeLog.StepInstruction = _promptedInstruction + (string.IsNullOrEmpty(_request.GuidanceMessage) ? "" : $"\n{_request.GuidanceMessage}");

            _forgeLog.Prompt = _request.Prompt;
            _forgeLog.FeedForwardMessage = _feedForwardInstruction;
            _forgeLog.RunnersLog = _instructionsLog;
            
            onRunNotify(_forgeLog);
        }

        protected void sendForgeLog(ForgeLog log, bool updateRunnersLog = true)
        {
            onRunNotify(log, updateRunnersLog);
        }
        public SingleThrowStep ThrowTo(bool swapRunner, IJsoneable command, ChainPromptRequest? request = null, string? feedForwardInstruction = null)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"{nameof(ChainStep)} >> {nameof(ThrowTo)} >> This method is to instantiate steps with a copy of the chain runner and the current caller is not the runner");

            var newRunner = swapRunner ? _runner.Clone() : _runner;

            //every step of this sub chain gets a new nullable runner with the previous log, but they subscribe to their own runner
            // this is to run chains in parallel. The last runner of each subChain has the report for the original Multithrow Step so they can be appended
            // to the original / main runner and deleted
            return Activator.CreateInstance(typeof(SingleThrowStep), command, newRunner, feedForwardInstruction) as SingleThrowStep;
        }
        public SingleThrowStep ExpandTo(IJsoneable command, ChainPromptRequest? request, string? feedForwardInstruction = null)
            => Activator.CreateInstance(typeof(SingleThrowStep), command, request, feedForwardInstruction) as SingleThrowStep;
        public SingleThrowStep ExpandTo<TCommand, TResult>(string instruction, ChainPromptRequest? request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new()
            => ExpandTo(Activator.CreateInstance(typeof(TCommand), Commands.First().BorrowLlama, instruction, request.StepSettings) as IJsoneable, request, feedForwardInstruction);

        public TStep ExpandTo<TStep>(IJsoneable command, ChainPromptRequest? request, string? feedForwardInstruction = null) where TStep : ChainStep
            => Activator.CreateInstance(typeof(TStep), command, request, feedForwardInstruction) as TStep;
        public TStep ExpandTo<TStep>(StepInstruction command, ChainPromptRequest? request) where TStep : ChainStep
            => Activator.CreateInstance(typeof(TStep), command, request) as TStep;
        public TStep ExpandTo<TStep, TCommand, TResult>(string instruction, ChainPromptRequest request, string? feedForwardInstruction = null)
            where TCommand : BasePromptCommand<TResult>, new()
            where TStep : ChainStep
                => ExpandTo<TStep>(Activator.CreateInstance(typeof(TCommand), Commands.First().BorrowLlama, instruction, request.Settings) as IJsoneable, request, feedForwardInstruction);

        public SplitterStep Plug(List<StepInstruction> instructions, ChainPromptRequest? request, string? splitterFeedFwd = null)
          => Activator.CreateInstance(typeof(SplitterStep), instructions, request, splitterFeedFwd) as SplitterStep;
        public SplitterStep SplitTo(StepInstruction splitted, List<StepInstruction> instructions, ChainPromptRequest? request = null)
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

        protected string getStepGuidanceMessage(IChaineable previous)
        {
            var sb = new StringBuilder();

            if (!IsFirstStep() && previous.IsForged)
            {
                // The guidance message is appended to the command final system message in this way: _systemMessage (optional. guidance. on constructor) + dbMessage (optional. main msg. on construction) | defaultInstruction(optional. main msg. hardcoded) + _request.Guidance
                sb.AppendLine()
                  .AppendLine(guidanceMessageFrom(
                    previous.Outputs.First().Instruction,
                    previous.Outputs.First().SerializedResult,
                    previous.Outputs.First().SchemaForMessage(), // deberia ser opcional
                    previous.Outputs.First().GuidanceMessage));
            }

            return sb.ToString();
        }
        protected async Task forgeLinkForPlug(IChaineable previous, int plugIdx = 0)
        {
            // any context injected / generated previously in the step process + default previous context mesage
            _request.GuidanceMessage += string.IsNullOrEmpty(_request.GuidanceMessage) ? getStepGuidanceMessage(previous) : $"\n{getStepGuidanceMessage(previous)}";
            //_request.Feeds.Foreach guid => _request.GuidanceMessage += _runner.TryFind(out guid)
            await forgeLink(plugIdx);
        }

        protected async Task forgeLink(int idx = 0)
        {
            if (idx >= _commands.Count) return;

            // Es decir: se pasa el instructions log que es el step-by-step lite que ya funciona. sirve como sumary para Steps si hace falta.
            //           opcionalmente se puede pedir el reporte completo ordenado por fecha de ejecucion con todos los mensajes internos (StepLogs con instrucciones)
            _request.Prompt = IsFirstStep() && _runner.RunnedInstructions.Count == 0 ? _runner.UserPrompt : "Complete the specified instruction. Do not chat with the user, just output the requested data as described.";
            
            if (_request.Settings == null)
                _request.Settings = _request.Settings != null ?_request.Settings : _runner.DefaultSettings.ToOllamaRequest();
            
            var sb = new StringBuilder();
            
            _request.ChainFeeds.ForEach(runnerId =>
            {
                if (_runner.TryFindForged(runnerId, out var forged))
                    sb.AppendLine()
                      .AppendLine(guidanceMessageFrom(
                        forged.CommandInstruction,
                        forged.JsonResult,
                        forged.JsonSchema,
                        forged.FeedForwardMessage));
            });

            if(_request.Boosters.Count > 0)
                sb.AppendLine()
                  .AppendLine(_request.BoostersFeedText());

            _request.GuidanceMessage += $"\n{sb.ToString()}".Trim();

            var jsonResult = await _commands[idx].JsonPrompt(_request, returnFullInstruction: false, preInstruction: preInstructionTag); //Skip GuidanceMessage, return only instruction for this step

            _promptedInstruction = jsonResult.Instruction;

            _instructionsLog.Add(_promptedInstruction);

            var link = new ChainLink(_id, _promptedInstruction, jsonResult.RawJson, JsonSerializerOptions.Default.GetJsonSchemaAsNode(jsonResult.Type), jsonResult.Type, feedForwardMessage: _feedForwardInstruction);

            _outputs.Add(link);

            _forgeLog.ForgeTimestamp = DateTime.Now;
            _forgeLog.JsonResult = jsonResult.RawJson;
            _forgeLog.JsonSchema = link.SchemaForMessage();
        }

        public string getContextMessageHeader()
            => !IsRunning ? string.Empty : @$"
# CONTEXT: This section contains relevant information about previous instruction. Use the provided data as a guidance to complete your own instruction. Use each section 
description (wrapped in parenthesis) to understand what does it represent and how can it help you to complete your task but, REMEMBER: your instruction is ALWAYS your priority.

{(!string.IsNullOrEmpty(_runner.Intent) ? $"> CHAIN INTENT (this is the overall goal of the whole chain in a categoric manner. Use it to have a very general idea about the task that the chain is trying to complete): {_runner.Intent}\n" : string.Empty)}
> USER INPUT (this is the actual user request. Use it to understand what the user is trying to do in general terms): {_runner.UserPrompt}";

        protected string guidanceMessageFrom(string instruction, string serialized, string jsonSchema, string? feededInstruction = null)
         => @$"
> PREVIOUS INSTRUCTION (use this to have a clear idea of what was exactly the previous worker task and understand its output):

 {instruction.Replace(preInstructionTag, "").Trim()}

> PREVIOUS OUTPUT (this is the raw json output of the previous instruction):

{serialized}

{(string.IsNullOrEmpty(jsonSchema) ? string.Empty : $"> PREVIOUS OUTPUT SCHEMA (use this to understand the previous output json model):\n\n{jsonSchema}")}

{(string.IsNullOrEmpty(feededInstruction) ? string.Empty : $"> GUIDANCE MESSAGE FROM PREVIOUS WORKER (this states what the previous worker expects you to do with his output data): {feededInstruction}")}";

        public bool hasCatchedThrow(IChaineable previous)
        {
            _runner = previous.Drop();

            _runner.OnDropTo(this, previous);

            onRunNotify += _runner.OnRunnerNotification;
            
            checkCanForge(previous);

            //'what is the play about!'

            _request.GuidanceMessage += getContextMessageHeader();

            _passCatchTimestamp = DateTime.Now;

            return IsRunning;
        }

        protected virtual ReplayLog replayFromLog() => new ReplayLog(_forgeLog);
        // override in Multi-Socket to include subchain results
        public void SendReplay()
        {
            //build report
            var report = replayFromLog();
            
            report.RunnersLog = _instructionsLog; //cada miembro de la subchain hace append de SU instruction. si es sub-sub chain, contiene el log ya formateado (porque se hace report de pieza main) 
                                                                                                      //ej: sub-> [- do X] [ - do Y] [-TAP\n-SUBCHAIN:\n-Do Z\n-SUBCHAIN:\n-Do ETC] - [- do ..]
            report.FeededMessage = _request.GuidanceMessage;
            report.PassCatchTimestamp = _passCatchTimestamp;

            onReportReplay(report);
        }

        // 19-05-2026 --> esto realmente rompe el patron rugby (info al runner y solo un runner)
        //                si tengo que llamar a esto en algun sitio es que lo estoy haciendo mal
        public IChaineable OnRunnerCall()
            => this;

        public void OnRunnerSupport(Guid id)
        {
            if (_id == id)
                onSupportIncoming(this);
        }

        public void FollowRunner(IChaineable current)
        {
            if (!current.IsRunning) return;

            onSupportIncoming += current.Runner.OnSupporterResponse;
            onReportReplay += current.Runner.OnReplayReport;
        }

        public bool IsReady() => IsRunning && _runner.CanRun();

        public void BoostWith(List<string> feeds, string? feedMessage) => _request.WithDataBoost(feedMessage, feeds);
    }
}
