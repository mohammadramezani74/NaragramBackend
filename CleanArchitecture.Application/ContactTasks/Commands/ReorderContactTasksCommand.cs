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
    /// <summary>ترتیب جدید کارهای باز. شناسه‌هایی که مال کاربر نیستند نادیده گرفته می‌شوند.</summary>
    public sealed record ReorderContactTasksCommand(Guid ContactId, List<Guid> OrderedIds) : ICommands;

    internal sealed class ReorderContactTasksCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<ReorderContactTasksCommand>
    {
        public async Task<OperationResult> Handle(
            ReorderContactTasksCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var me = userManager.UserId!.Value;

            if (request.OrderedIds is null || request.OrderedIds.Count == 0)
                return op.succedded();

            var tasks = await uow.ContactTasks
                .Where(t => t.OwnerId == me && t.ContactId == request.ContactId && !t.IsDone)
                .ToListAsync(cancellationToken);

            var position = request.OrderedIds
                .Distinct()
                .Select((id, index) => (id, index))
                .ToDictionary(x => x.id, x => x.index);

            // آن‌هایی که در فهرست ارسالی نیستند (مثلاً از دستگاه دیگری اضافه شده‌اند)
            // ترتیب نسبی خودشان را حفظ می‌کنند و بعد از بقیه می‌آیند.
            var ordered = tasks
                .OrderBy(t => position.TryGetValue(t.Id, out var p) ? p : int.MaxValue)
                .ThenBy(t => t.SortOrder)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
                ordered[i].SetSortOrder(i + 1);

            await uow.SaveChangesAsync(cancellationToken);
            await ContactTaskAccess.NotifyOwnerAsync(hub, me, request.ContactId);

            return op.succedded();
        }
    }
}
