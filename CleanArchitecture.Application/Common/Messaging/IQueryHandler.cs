

using CleanArchitecture.Application.Common.Models;
using MediatR;

namespace CleanArchitecture.Application.Common.Messaging;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, OperationResult<TResponse>>
     where TQuery : IQuery<TResponse>
{
}
