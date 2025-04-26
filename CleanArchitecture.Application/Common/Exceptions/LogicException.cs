using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Exceptions;

public class LogicException:AppException
{
    const string LogicError = "Logical Error";
    public LogicException()
       : base(LogicError)
    {
    }

    public LogicException(string message)
        : base(new OperationResult().Logic(message), message)
    {
    }

    public LogicException(object additionalData)
        : base(new OperationResult().Logic("خطای سرور"), additionalData)
    {
    }

    public LogicException(string message, object additionalData)
        : base(new OperationResult().Logic(message), message, additionalData)
    {
    }

    public LogicException(string message, Exception exception)
        : base(new OperationResult().Logic(message), message, exception)
    {
    }

    public LogicException(string message, Exception exception, object additionalData)
        : base(new OperationResult().Logic(message), message, exception, additionalData)
    {
    }
}
