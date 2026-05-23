using Dotnet.OllamaSharp.LameChain.SDK.Command.Requests;
using System.ComponentModel;

namespace DotnetLlamaSharp.Domain.Models.Request
{
    public class SmartQueryRequest : SimpleCommandRequest
    {
        [Description(nameof(ChatCommandRequest))]
        public bool WithChatCollections { get; set; } // include the catalogue of chat collections in the available collections catalogue;
        //[CommandName=nameof(QueryAugmentationCommand))]
        public int QueryAugments { get; set; } // generate N variants of the input to cover more similarity points;
        //[CommandName=nameof(RagExpansionCommand))]
        public int RagExpansions { get; set; } // generate a brief response without rag and use the result along the user query to create the rag
        public bool WithFewShotExpansion { get; set; }
        public int MaxFewShotExamples { get; set; }
        //[CommandName="LangSearch | LangSearchAndRang]
        public bool WithLangSearch { get; set; } // use LangSearch if the query is not related to any rag collection

        public int CollectionRetrievals { get; set; }
        public int MaxCollectionChoices { get; set; }
        public float IntentConfidenceThreshold { get; set; }

        /*
            TCommand | string CommandFor(nameof(QueryAutments)) <- lee attribute y devuelve command? devuelve nombre de command (factory fuera de request)

            (de alguna manera me lo curro para ir sacando los nombres de commands de la request)
            var reqCmds = req.GetAgentCommands() <- this.GetProperties().ForEach(p => p.GEtAttributes().Where(x) blah blah y pillo los nombres
            reqCmds.ForEach(cmd => {
            _factory.GetByName(cmd) <- MOVIDA: como coño lo paso de name a TCommand -->Singleton{ DictionaryCache<name,TCommand> AllCommands | RagCommands, ChatCommands, NosequeCommands, EtcCommands (y para eso sirve el Singleton)}
            
            ActivatorByName: https://learn.microsoft.com/en-us/answers/questions/2088439/how-do-you-instantiate-a-class-given-its-name-in-c

            ---------https://www.c-sharpcorner.com/blogs/create-instance-from-string-name-in-c-sharp1-----
            Assembly asm = Assembly.GetEntryAssembly(); <- GetCommandsAssembly

            string path = asm.GetName().ToString();

            path = path.Substring(0, path.IndexOf(","));

            string formname = path + "Your_Form_or_class_name_here";

            Type formtype = asm.GetType(formname);

            Form f = (Form)Activator.CreateInstance(formtype);

            f.Show();
        
            })
         ------------------------------------------------------

            el caso es que consigo una lista de TCommands y da igual que no esten en orden porque los voy pillando de la lista / dict en el proceso
            Y todo esto para hacer algo como :

                SmartQueryRequest(req)
                    => handle(req)  <- lee cmds, ejecuta sus movidas etc, no usa SimpleRaqQuery
         */
    }
}
