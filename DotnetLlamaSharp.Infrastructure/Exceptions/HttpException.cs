using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Exceptions
{
    public class HttpException : Exception
    {
        public HttpException(string message) : base(message)
        {
            StatusCode = HttpStatusCode.InternalServerError;    
        }
        public HttpException(HttpStatusCode code, string message) : base(message)
        {
            StatusCode = code;
        }
        public HttpStatusCode StatusCode { get; set; }
    }
}
