using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DotnetLlamaSharp.Infrastructure.Exceptions
{
    public class BadRequestException : HttpException
    {
        public BadRequestException(string message) : base(HttpStatusCode.BadRequest, message) {}
    }
}
