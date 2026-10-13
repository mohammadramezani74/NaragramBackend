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
    /// <summary>حذف واقعی (نه نرم): این داده‌ی شخصی است و دلیلی برای نگه داشتنش نیست.</summary>
    public sealed record DeleteContactTaskCommand(Guid Id) : ICommands;

    internal sealed class DeleteContactTaskCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<DeleteContactTaskCommand>
    {
        public async Task<OperationResult> Handle(
            DeleteContactTaskCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var me = userManager.UserId!.Value;

            var task = await uow.ContactTasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.OwnerId == me, cancellationToken);

            if (task is null)
                return op.NotFound("کار یافت نشد.");

            uow.ContactTasks.Remove(task);
            await uow.SaveChangesAsync(cancellationToken);

            await ContactTaskAccess.NotifyOwnerAsync(hub, me, task.ContactId);
            return op.succedded();
        }
    }

    /// <summary>پاک کردن همه‌ی انجام‌شده‌های یک مخاطب.</summary>
    public sealed record ClearDoneContactTasksCommand(Guid ContactId) : ICommands;

    internal sealed class ClearDoneContactTasksCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<ClearDoneContactTasksCommand>
    {
        public async Task<OperationResult> Handle(
            ClearDoneContactTasksCommand request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            var removed = await uow.ContactTasks
                .Where(t => t.OwnerId == me && t.ContactId == request.ContactId && t.IsDone)
                .ExecuteDeleteAsync(cancellationToken);

            if (removed > 0)
                await ContactTaskAccess.NotifyOwnerAsync(hub, me, request.ContactId);

            return new OperationResult().succedded($"{removed} مورد پاک شد.");
        }
    }
}
