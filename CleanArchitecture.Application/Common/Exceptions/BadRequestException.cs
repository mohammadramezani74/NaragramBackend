using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Exceptions
{
    public class BadRequestException : AppException
    {
        public BadRequestException()
            : base(new OperationResult().BadRequest("دیتای وارد شده معتبر نمیباشد"), System.Net.HttpStatusCode.BadRequest)
        {
        }

        public BadRequestException(string message)
            : base(new OperationResult().BadRequest(message), message, System.Net.HttpStatusCode.BadRequest)
        {
        }

        public BadRequestException(object additionalData)
            : base(new OperationResult().BadRequest("دیتای وارد شده معتبر نمیباشد"), string.Empty, System.Net.HttpStatusCode.BadRequest, additionalData)
        {
        }

        public BadRequestException(string message, object additionalData)
            : base(new OperationResult().BadRequest(message), message, System.Net.HttpStatusCode.BadRequest, additionalData)
        {
        }

        public BadRequestException(string message, Exception exception)
            : base(new OperationResult().BadRequest(message), message, exception, System.Net.HttpStatusCode.BadRequest)
        {
        }

        public BadRequestException(string message, Exception exception, object additionalData)
            : base(new OperationResult().BadRequest(message), message, System.Net.HttpStatusCode.BadRequest, exception, additionalData)
        {
        }
    }
}
