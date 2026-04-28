using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Exceptions
{
    public class ChromaClientException : HttpException
    {
        public ChromaClientException(string message) : base(message) 
        {
            StatusCode = HttpStatusCode.BadRequest;
        }

        public ChromaClientException(HttpStatusCode code, string message) : base(message)
        {
            StatusCode = code;
        }
    }
}
