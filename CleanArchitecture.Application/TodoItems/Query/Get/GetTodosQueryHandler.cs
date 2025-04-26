using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using EFCoreSecondLevelCacheInterceptor;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Query.Get
{
    internal sealed class GetTodosQueryHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork uow,
        IMapper mapper) : IQueryHandler<GetTodosQuery, TodoResponse[]>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IMapper _mapper = mapper;

        public async Task<OperationResult<TodoResponse[]>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
        {
            var userId = _userManager.UserId!.Value;
            var TodoItems= await _uow.TodoItem.AsNoTracking()
                .Where(x=>x.CreatedByUserId== userId)
               .Cacheable()
            .ToArrayAsync(cancellationToken);
           var response= _mapper.Map<TodoResponse[]>(TodoItems);
            return response;
        }
    }
}
