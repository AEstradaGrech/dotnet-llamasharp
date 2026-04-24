namespace DotnetLlamaSharp.Models.Common
{
    public class ApiError
    {
        public ApiError() { }
        public ApiError(int code, string message) { }

        public int Code { get; set; }
        public string Message { get; set; }
    }
}
