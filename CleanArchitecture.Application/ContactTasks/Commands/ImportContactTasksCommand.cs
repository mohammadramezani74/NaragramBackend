using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks.Commands
{
    public sealed class ImportContactTaskItem
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int Order { get; set; }
    }

    /// <summary>
    /// انتقال یک‌باره‌ی چک‌لیست قدیمی (که فقط در مرورگر ذخیره می‌شد) به سرور.
    /// شناسه‌ی قدیمی هر مورد حفظ می‌شود، پس اگر از دو مرورگر یا دو بار اجرا شود
    /// مورد تکراری ساخته نمی‌شود.
    /// </summary>
    public sealed record ImportContactTasksCommand(Guid ContactId, List<ImportContactTaskItem> Items) : ICommands<int>;

    internal sealed class ImportContactTasksCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<ImportContactTasksCommand, int>
    {
        public async Task<OperationResult<int>> Handle(
            ImportContactTasksCommand request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            var items = (request.Items ?? new())
                .Where(i => i.Id != Guid.Empty && !string.IsNullOrWhiteSpace(i.Text))
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .Take(ContactTaskRules.MaxImportItems)
                .ToList();

            if (items.Count == 0)
                return OperationResult.Success(0);

            var contactError = await ContactTaskAccess.ValidateContactAsync(uow, me, request.ContactId, cancellationToken);
            if (contactError is not null)
                return OperationResult.Failure<int>(contactError);

            var ids = items.Select(i => i.Id).ToList();
            var existing = await uow.ContactTasks.AsNoTracking()
                .Where(t => ids.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            var fresh = items.Where(i => !existing.Contains(i.Id)).ToList();
            if (fresh.Count == 0)
                return OperationResult.Success(0);

            var maxOrder = await uow.ContactTasks
                .Where(t => t.OwnerId == me && t.ContactId == request.ContactId && !t.IsDone)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            var order = maxOrder;
            foreach (var item in fresh.OrderBy(i => i.Order))
            {
                var task = ContactTask.Create(
                    me,
                    request.ContactId,
                    item.Text,
                    item.DueDate,
                    ++order,
                    id: item.Id,
                    createdAt: item.CreatedAt?.ToLocalTime());

                if (item.IsDone)
                    task.SetDone(true);

                uow.ContactTasks.Add(task);
            }

            await uow.SaveChangesAsync(cancellationToken);
            await ContactTaskAccess.NotifyOwnerAsync(hub, me, request.ContactId);

            return OperationResult.Success(fresh.Count);
        }
    }
}
