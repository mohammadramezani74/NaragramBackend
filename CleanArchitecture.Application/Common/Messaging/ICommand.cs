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
    public interface ICommands : IRequest<OperationResult>, IBaseCommand
    {
    }
    public interface ICommands<TResponse> : IRequest<OperationResult<TResponse>>, IBaseCommand
    {
    }
    public interface IBaseCommand
    {

    }
}
