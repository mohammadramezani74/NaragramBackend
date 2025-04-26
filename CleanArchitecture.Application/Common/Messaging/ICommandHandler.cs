using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Messaging
{
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, OperationResult>
       where TCommand : ICommands
    {

    }
    public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, OperationResult<TResponse>>
     where TCommand : ICommands<TResponse>
    {

    }
}
