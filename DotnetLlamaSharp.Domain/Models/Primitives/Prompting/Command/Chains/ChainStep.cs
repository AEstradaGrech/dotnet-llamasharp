using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Bases;
using DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests;
using System.Text.Json;
using System.Text.Json.Schema;


namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class ChainStep : IChaineable
    {

        private IJsoneable _command;
        private IChaineable _next; // Si el sistema funciona Then = execute & store esto no tiene sentido. Si funciona tipo Load & Run si, pero entonces necesito _prev para reconstruir la cadena
        private IChaineable _prev;
        private bool _isPreloaded; // RunThen | LoadNext AKA Chain
        private string? _promptedInstruction = null; // The prompted instruction for this step. Serves as a log and also to generate the feedForwardInstruction (provide context about prev step). Null if !Executed
        private string? _feedForwardInstruction = null;    // 
        private PromptCommandRequest _request;
        public bool IsPreloaded => _isPreloaded;
        public string? PromptedInstruction => _promptedInstruction;
        public bool IsChained(bool checkNextOnly = true) 
            => checkNextOnly ? _next != null : _next != null || _prev != null;

        public bool IsFirstStep()
            => IsChained() && _prev == null;

        // IOutputable (esconde el tipado)
        public ChainLink OutputLink => _output;
        // -> ThenExe: ejecuta las cosas sin mas. es el comportamiento por defecto      API >> .RunThen() [MOVIDA: implementacion Run / Load PARA CADA METODO (RunBranch, RunFor, RunBlah
        // -> Load&Run: sirve para cargar cadenas que se pueden:                        API >> .Then() <- Que sea Run | Load deberia depender de primer step (bIsBuffered
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

        ChainLink IChaineable.OutputLink { get => OutputLink; set => throw new NotImplementedException(); }

        /*
            Link(step, fwd, isTwoWayLink) <- configura un eslabon / union de ChainSteps
            Forge(prevStep) <- 'ejecuta' / crea el eslablon que une dos ChainSteps, continuando con la cadena durante el Run con el resultado de la anterior
            RunChain(prevLink) <- ejecuta el step (command) y forja el eslabon
            ExecuteChain() <- ejecuta todo y devuelve mensaje final
            JsonPromptResult BeginChain() <- ejecuta todo hasta el ultimo eslabón y lo devuelve en el formato generalizado para continuar con otra cadena o para lo que sea
         */
        public void Link(IChaineable step, bool isForward, bool isTwoWay)
        {
            if (isForward)
                _next = step;

            else _prev = step;

            if (isTwoWay)
                step.Link(this, isForward: !isForward);
        }

        //ESTO EN PRINCIPIO DEVUELVE FINAL RESULT. YA NO SE PODRIA ENCADENAR CON OTRAS CHAINS
        public async Task<ChainResult> ExecuteChain(bool withUserFriendlyMessage = true)
        {
            if(IsPreloaded)
            {
                var firstStep = getFirstStep(current: this);

                var finalStep = await firstStep.Forge(null);

                // if(finalStep OK) -> toFinalMessage & return --> QUE COJONES ES EL FINAL MESSAGE --> ChainResult.cs --> RunChain devuelve JsonPromptResult (ejecutado), ExecuteChain devuelve ChainResult w/ChatMessag & OptLink.JSON_DATA (Raw & schema to string

                //  finalStep.Output --> es un structured output que puede variar desde simple text msg, scored bool | choice hasta modelos muy complejos (CharacterDto, p.ej)
                var typedResult = finalStep.OutputLink.SerializedResult; //AQUI HACE FALTA ALGUN TIPO DE PROCESO DEL PALO ANALYZE JSON & SCHEMA & ORIGINAL USERQUERY & (optional) USERINTENT y FINAL MESSAGE ES SIEMPRE EL RESULTADO DE ESO
                
                if(withUserFriendlyMessage)
                {
                    
                }
                // OPCION B: FINAL MESSAGE ES UN MODELO DE LAME_CHAIN_SDK. Contiene  un ChatMessage porque es el modelo mas general de la api, PERO tambien lleva la info JSON del ultimo output para que el hipotético usuario haga lo 
                //          que quiera (deserializar el json a un tipo que el espera o usar el ChatMessage si está pasando texto a otra LLM o si le vale recibir ya el resultado final en formato texto-instrucción o texto-chat porque
                //          realmente me la suda añadir movidas del output que ya tengo en final step.OutputLink)

                return new ChainResult(jsonResult: typedResult, finalStep.OutputLink.JsonSchema, chainInput: "");

                //return firstStep.Forge(previous)
                // jsonResult = step.Command.JsonPrompt();
                //  _output = new ChainLink(jsonResult); <- step.Forge() <- fancy api name executes prompt and stores result and connects to next running step
                // return step.Next.Forge(this) que ya tiene el output
            }
            return new ChainResult();
        }
        private IChaineable getFirstStep(IChaineable current)
            => IsFirstStep() ? this : getFirstStep(_prev);

        public async Task<IChaineable> Forge(IChaineable previous)
        {
            if(!IsFirstStep())
            {
                // The guidance message is appended to the command final system message in this way: _systemMessage (optional. guidance. on constructor) + dbMessage (optional. main msg. on construction) | defaultInstruction(optional. main msg. hardcoded) + _request.Guidance
                _request.GuidanceMessage = @$"# CONTEXT: A previous process on your current task has reported the next results for this user query and instruction, 
use this information if you find it relevant for your current task.

- PREVIOUS PROMPT: {_request.Prompt}
- PREVIOUS TASK: {previous.PromptedInstruction}

- PREVIOUS OUTPUT:

{previous.OutputLink.SerializedResult}

- PREVIOUS OUTPUT SCHEMA:

{previous.OutputLink.SchemaForMessage()}
";   
                if(!string.IsNullOrEmpty(previous.OutputLink.GuidanceMessage))
                    _request.GuidanceMessage += $"\n> This is what you are expected to do with the previous output data: {previous.OutputLink.GuidanceMessage}";
            }

            var jsonResult = await _command.JsonPrompt(_request, returnFullInstruction: false); //Skip GuidanceMessage, return only instruction for this step

            _promptedInstruction = jsonResult.Instruction;

            _output = new ChainLink(jsonResult.RawJson, JsonSerializerOptions.Default.GetJsonSchemaAsNode(jsonResult.Type), feedForwardMessage: _feedForwardInstruction);

            return _next != null ? await _next.Forge(this) : this;
        }

        public IChaineable ExpandTo(IJsoneable command, PromptCommandRequest request, string? feedForwardInstruction = null)
            => Activator.CreateInstance(this.GetType(), command, request, _isPreloaded, feedForwardInstruction) as IChaineable;

        public IChaineable ExpandTo<TCommand, TResult>(string instruction, PromptCommandRequest request, string? feedForwardInstruction = null) where TCommand : BasePromptCommand<TResult>, new()
            => ExpandTo(Activator.CreateInstance(typeof(TCommand), instruction, request.Settings) as IJsoneable, request, feedForwardInstruction);
        
    }
}
