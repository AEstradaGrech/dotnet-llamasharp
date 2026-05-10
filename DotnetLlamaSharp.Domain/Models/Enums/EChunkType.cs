using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Enums
{
    public enum EChunkType
    {
        DEFAULT = 0,
        COLLECTION = 1,
        FILE = 2,
        SYSTEM = 3,
        CHAT = 4,
        SESSION = 5
    }
}
