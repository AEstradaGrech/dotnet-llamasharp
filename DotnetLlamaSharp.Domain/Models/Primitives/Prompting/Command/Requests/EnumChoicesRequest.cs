using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Requests
{
    public class EnumChoicesRequest<TEnum> : PromptCommandRequest where TEnum : struct, Enum
    {
        public List<TEnum> Choices { get; set; }
    }
}
