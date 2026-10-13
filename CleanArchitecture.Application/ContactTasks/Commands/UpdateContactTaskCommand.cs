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
    /// <summary>
    /// ویرایش متن، سررسید و یادآوری. همه‌ی مقدارها کامل فرستاده می‌شوند؛
    /// null یعنی «ندارد».
    /// </summary>
    public sealed record UpdateContactTaskCommand(Guid Id, string Text, DateTime? DueDate, DateTimeOffset? RemindAt) : ICommands<ContactTaskDto>;

    internal sealed class UpdateContactTaskCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<UpdateContactTaskCommand, ContactTaskDto>
    {
        public async Task<OperationResult<ContactTaskDto>> Handle(
            UpdateContactTaskCommand request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            var text = (request.Text ?? string.Empty).Trim();
            if (text.Length == 0)
                return OperationResult.Failure<ContactTaskDto>(new OperationResult().BadRequest("متن کار خالی است."));

            if (text.Length > ContactTask.MaxTextLength)
                return OperationResult.Failure<ContactTaskDto>(
                    new OperationResult().BadRequest($"متن کار حداکثر {ContactTask.MaxTextLength} نویسه است."));

            var task = await uow.ContactTasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.OwnerId == me, cancellationToken);

            if (task is null)
                return OperationResult.Failure<ContactTaskDto>(new OperationResult().NotFound("کار یافت نشد."));

            // زمان گذشته فقط وقتی رد می‌شود که کاربر واقعاً عوضش کرده باشد؛
            // یادآوریِ قبلاً ارسال‌شده همراه ویرایش متن برمی‌گردد و نباید خطا بدهد.
            var newRemindUtc = request.RemindAt?.UtcDateTime;
            var reminderChanged = newRemindUtc?.Ticks / TimeSpan.TicksPerMinute
                                  != task.RemindAtUtc?.Ticks / TimeSpan.TicksPerMinute;
            if (reminderChanged)
            {
                var reminderError = ContactTaskRules.ValidateReminder(request.RemindAt);
                if (reminderError is not null)
                    return OperationResult.Failure<ContactTaskDto>(reminderError);
            }

            task.SetText(text);
            task.SetDueDate(request.DueDate);
            if (reminderChanged)
                task.SetReminder(newRemindUtc);
            await uow.SaveChangesAsync(cancellationToken);

            await ContactTaskAccess.NotifyOwnerAsync(hub, me, task.ContactId);

            return OperationResult.Success(ContactTaskDto.From(task));
        }
    }
}
