using System.Text.Json;

namespace DotnetLlamaSharp.Models.Common
{
    public class ApiError
    {
        public ApiError() { }
        public ApiError(int code, string message) 
        {
            Code = code;
            Message = message;
        }

        public int Code { get; set; }
        public string Message { get; set; }

        public override string ToString()
            => JsonSerializer.Serialize(this);
    }
}
