namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting
{
    public class CommandSettings : PromptSettings
    {
        public int CommandValidations { get; set; } = 0;
        public bool IsTwoStepValidation { get; set; } = false;
    }
}
