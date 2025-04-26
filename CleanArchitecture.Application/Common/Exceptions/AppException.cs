using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Exceptions
{
    public class AppException:Exception
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public OperationResult ApiStatusCode { get; set; }
        public object AdditionalData { get; set; }

        public AppException()
           : this("ServerError")
        {
        }

        public AppException(OperationResult statusCode)
            : this(statusCode, string.Empty)
        {
        }

        public AppException(string message)
            : this(new OperationResult().Failed(message), message)
        {
        }

        public AppException(OperationResult statusCode, string message)
            : this(statusCode, message, HttpStatusCode.InternalServerError)
        {
        }

        public AppException(string message, object additionalData)
            : this(new OperationResult().Failed(message), message, additionalData)
        {
        }

        public AppException(OperationResult statusCode, object additionalData)
            : this(statusCode, string.Empty, additionalData)
        {
        }

        public AppException(OperationResult statusCode, string message, object additionalData)
            : this(statusCode, message, HttpStatusCode.InternalServerError, additionalData)
        {
        }

        public AppException(OperationResult statusCode, string message, HttpStatusCode httpStatusCode)
            : this(statusCode, message, httpStatusCode, string.Empty)
        {
        }

        public AppException(OperationResult statusCode, string message, HttpStatusCode httpStatusCode, object additionalData)
            : this(statusCode, message, httpStatusCode, new ArgumentNullException(), additionalData)
        {
        }

        public AppException(string message, Exception exception)
            : this(new OperationResult().Failed(""), message, exception)
        {
        }

        public AppException(string message, Exception exception, object additionalData)
            : this(new OperationResult().Failed(""), message, exception, additionalData)
        {
        }

        public AppException(OperationResult statusCode, string message, Exception exception)
            : this(statusCode, message, HttpStatusCode.InternalServerError, exception)
        {
        }

        public AppException(OperationResult statusCode, string message, Exception exception, object additionalData)
            : this(statusCode, message, HttpStatusCode.InternalServerError, exception, additionalData)
        {
        }

        public AppException(OperationResult statusCode, string message, HttpStatusCode httpStatusCode, Exception exception)
            : this(statusCode, message, httpStatusCode, exception, string.Empty)
        {
        }

        public AppException(OperationResult statusCode, string message, HttpStatusCode httpStatusCode, Exception exception, object additionalData)
            : base(message, exception)
        {
            ApiStatusCode = statusCode;
            HttpStatusCode = httpStatusCode;
            AdditionalData = additionalData;
        }
    }
}
