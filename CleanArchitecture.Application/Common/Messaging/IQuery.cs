using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Messaging
{
    public interface IQuery<TResponse> : IRequest<OperationResult<TResponse>>
    {
    }
}
