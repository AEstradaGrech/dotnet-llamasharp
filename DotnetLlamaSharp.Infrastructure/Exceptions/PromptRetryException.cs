using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Exceptions
{
    public class PromptRetryException : HttpException
    {
        private const string _message = "En error has occured while requesting a ollama prompt >> RETRIES LIMIT REACHED >> <RETRIES> >> MESSAGE: <EXCEPTION>";
        public PromptRetryException(string exMessage, int retries) : base(_message.Replace("<RETRIES>", $"{retries}").Replace("<EXCEPTION>", exMessage)) {}
    }
}
