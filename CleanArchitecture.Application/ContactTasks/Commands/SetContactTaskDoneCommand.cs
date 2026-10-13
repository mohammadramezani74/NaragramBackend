using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks.Commands
{
    public sealed record SetContactTaskDoneCommand(Guid Id, bool Done) : ICommands<ContactTaskDto>;

    internal sealed class SetContactTaskDoneCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<SetContactTaskDoneCommand, ContactTaskDto>
    {
        public async Task<OperationResult<ContactTaskDto>> Handle(
            SetContactTaskDoneCommand request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            var task = await uow.ContactTasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.OwnerId == me, cancellationToken);

            if (task is null)
                return OperationResult.Failure<ContactTaskDto>(new OperationResult().NotFound("کار یافت نشد."));

            if (task.IsDone != request.Done)
            {
                // کاری که از «انجام‌شده» برمی‌گردد ته فهرست باز می‌نشیند.
                if (!request.Done)
                {
                    var maxOrder = await uow.ContactTasks
                        .Where(t => t.OwnerId == me && t.ContactId == task.ContactId && !t.IsDone)
                        .Select(t => (int?)t.SortOrder)
                        .MaxAsync(cancellationToken) ?? 0;
                    task.SetSortOrder(maxOrder + 1);
                }

                task.SetDone(request.Done);
                await uow.SaveChangesAsync(cancellationToken);
                await ContactTaskAccess.NotifyOwnerAsync(hub, me, task.ContactId);
            }

            return OperationResult.Success(ContactTaskDto.From(task));
        }
    }
}
