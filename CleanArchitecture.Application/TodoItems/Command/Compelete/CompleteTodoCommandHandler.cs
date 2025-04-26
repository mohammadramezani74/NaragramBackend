using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.ToDoItems;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Command.Compelete
{
    internal sealed class CompleteTodoCommandHandler(IApplicationUnitOfWork uow
        ,IDateTimeProvider dateTimeProvider) : ICommandHandler<CompleteTodoCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IDateTimeProvider _dateTimeProvider=dateTimeProvider;

        public async Task<OperationResult> Handle(CompleteTodoCommand request, CancellationToken cancellationToken)
        {
            var todoItem = await _uow.TodoItem 
                                                .SingleOrDefaultAsync(x=>x.Id==request.TodoItemId,cancellationToken);
            if (todoItem == null)
                return new OperationResult().NotFound("آیتم مورد نظر شما یافت نشد");
            if(todoItem.IsCompleted)
                return new OperationResult().Failed("این آیتم قبلا تکمیل شده است");
            todoItem.IsCompleted = true;
            todoItem.CompletedAt = _dateTimeProvider.Now;
            await _uow.SaveChangesAsync(cancellationToken);
            return new OperationResult().succedded();
        }
    }
}
