using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Exceptions
{
    public class NotFoundException:AppException
    {
        public NotFoundException()
           : base(new OperationResult().NotFound("دیتای مورد نظر یافت نشد"), System.Net.HttpStatusCode.NotFound)
        {
        }

        public NotFoundException(string message)
            : base(new OperationResult().NotFound(message), message, System.Net.HttpStatusCode.NotFound)
        {
        }

        public NotFoundException(object additionalData)
            : base(new OperationResult().NotFound("دیتای مورد نظر یافت نشد"), string.Empty, System.Net.HttpStatusCode.NotFound, additionalData)
        {
        }

        public NotFoundException(string message, object additionalData)
            : base(new OperationResult().NotFound(message), message, System.Net.HttpStatusCode.NotFound, additionalData)
        {
        }

        public NotFoundException(string message, Exception exception)
            : base(new OperationResult().NotFound(message), message, exception, System.Net.HttpStatusCode.NotFound)
        {
        }

        public NotFoundException(string message, Exception exception, object additionalData)
            : base(new OperationResult().NotFound(message), message, System.Net.HttpStatusCode.NotFound, exception, additionalData)
        {
        }
    }
}
