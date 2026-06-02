using Dotnet.OllamaSharp.LameChain.SDK.Commands.Base;
using Dotnet.OllamaSharp.LameChain.SDK.Commands.Response.StructuredOutputs.Attributes;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.LameChain
{
    [OllamaJsonOutput("Representation of a game character with pyschological profile for NPC dialogue generation", 
        Description = @"This schema captures the essential traits, personality facets, and demographic info needed 
                        for LLMs to generate in-character dialogue, behavioral responses, and consistent interaction patterns. 
                        Used for comprehensive characterization.")]
    [OllamaJsonRequirement("- All traits, personalities, and role must be consistent with each other")]
    [OllamaJsonRequirement("- Traits (3-5) should be concrete, observable characteristics")]
    [OllamaJsonRequirement("- Personalities (3-5) should represent emotional/psychological archetypes")]
    [OllamaJsonRequirement("- Age should be specific enough to inform voice and perspectiver")]
    public class GameCharacter : StructuredOutput
    {
        [OllamaJsonProperty(Title = "Description", PromptDescription = "Character's primary function in game world (2-5 words, max 50 chars).")]
        [OllamaJsonHint("This determines available interactions, dialogue tone, and behavioral constraints.")]
        [OllamaJsonProperty(Title = "Examples", PromptDescription = "Merchant, Guard, Bandit")]
        [OllamaJsonRequirement("- This MUST be one of the provided game roles or Citizen of no game roles available")]
        public string Role { get; set; }

        [OllamaJsonProperty(Title = "Description", PromptDescription = "Name of the Character's game faction or guild if any (it can be 'None' for unaligned character).")]
        [OllamaJsonProperty(Title = "Options", PromptDescription = "'R.C. PONENT', 'TORO R.C.', 'BAHIA R.C.'")]
        [OllamaJsonHint("Factions can be enemies, allies or indiferent. The selected faction determines the mood and attitude of character towards the other.")]
        public string Faction { get; set; }
        /*
         This groups attributes by section name and generates something like:

            > PROPERTY NAME: Role
            > DESCRIPTION: Character's primary function in game world (2-5 words, max 50 chars).
            > HINT: This determines available interactions, dialogue tone, and behavioral constraints.
            > REQUIREMENTS: - This MUST be one of the provided game roles or Citizen of no game roles available
            > EXAMPLES: "Merchant, Guard, Bandit"

            > PROPERTY NAME: Faction
            > DESCRIPTION: Name of the Character's game faction or guild if any (it can be 'None' for unaligned character).
            > OPTIONS: "'R.C. PONENT', 'TORO R.C.', 'BAHIA R.C.'"
            > HINT: Factions can be enemies, allies or indiferent. The selected faction determines the mood and attitude of character towards the other.

        // So it is possible to stack many Attributes with the same Title and they will be groupped in the same saction
        // And they will be appended to the system message in the order they are stacked

        // JsonHint is a class just to enforce a certaing usage (because it does not represent description but 'how to use' or 'what should be' or 'relates to this other property in this way'.... it is a hint)
         */
    }
}
