using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Command.Delete
{
    internal sealed class DeleteTodoCommandHandler(IApplicationUnitOfWork uow) : ICommandHandler<DeleteTodoCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var todoItem=await _uow.TodoItem.AsNoTracking()
                                                    .SingleOrDefaultAsync(x => x.Id == request.TodoItemId);
            if (todoItem == null)
            {
                return op.NotFound("آیتم مورد نظر شما یافت نشد");
            }
            _uow.TodoItem.Remove(todoItem);
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();

        }
    }
}
