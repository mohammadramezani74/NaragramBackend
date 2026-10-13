using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks.Commands
{
    /// <param name="SourceMessageId">اختیاری: پیامی که کار از روی آن ساخته می‌شود.</param>
    /// <param name="RemindAt">اختیاری: زمان یادآوری، با آفست منطقه‌ی زمانی.</param>
    public sealed record CreateContactTaskCommand(
        Guid ContactId,
        string Text,
        DateTime? DueDate,
        Guid? SourceMessageId,
        DateTimeOffset? RemindAt = null) : ICommands<ContactTaskDto>;

    internal sealed class CreateContactTaskCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<CreateContactTaskCommand, ContactTaskDto>
    {
        public async Task<OperationResult<ContactTaskDto>> Handle(
            CreateContactTaskCommand request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            var text = (request.Text ?? string.Empty).Trim();
            if (text.Length == 0)
                return Fail(new OperationResult().BadRequest("متن کار خالی است."));

            if (text.Length > ContactTask.MaxTextLength)
                return Fail(new OperationResult().BadRequest($"متن کار حداکثر {ContactTask.MaxTextLength} نویسه است."));

            var reminderError = ContactTaskRules.ValidateReminder(request.RemindAt);
            if (reminderError is not null)
                return Fail(reminderError);

            var contactError = await ContactTaskAccess.ValidateContactAsync(uow, me, request.ContactId, cancellationToken);
            if (contactError is not null)
                return Fail(contactError);

            var mine = uow.ContactTasks.Where(t => t.OwnerId == me && t.ContactId == request.ContactId);

            var total = await mine.CountAsync(cancellationToken);
            if (total >= ContactTaskRules.MaxTasksPerContact)
                return Fail(new OperationResult().BadRequest("فهرست این مخاطب پر است؛ چند مورد انجام‌شده را پاک کنید."));

            var maxOrder = await mine.Where(t => !t.IsDone)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            var task = ContactTask.Create(me, request.ContactId, text, request.DueDate, maxOrder + 1);
            task.SetReminder(request.RemindAt?.UtcDateTime);

            if (request.SourceMessageId is Guid messageId)
            {
                // پیام باید در گفتگوی خصوصیِ همین دو نفر باشد؛ وگرنه کسی می‌تواند
                // با حدس شناسه، متن پیام‌های دیگران را در کارهای خودش ببیند.
                var source = await uow.Messages.AsNoTracking()
                    .Where(m => m.Id == messageId
                                && !m.Deleted
                                && m.ConversationId != null
                                && m.Conversation!.IsPrivate
                                && m.Conversation.Users.Any(u => u.UserId == me)
                                && m.Conversation.Users.Any(u => u.UserId == request.ContactId))
                    .Select(m => new { m.Id, m.Content, m.MessageType, m.CreateDate })
                    .FirstOrDefaultAsync(cancellationToken);

                if (source is null)
                    return Fail(new OperationResult().NotFound("پیام در گفتگوی شما با این مخاطب پیدا نشد."));

                task.LinkToMessage(source.Id, Preview(source.Content, source.MessageType), source.CreateDate);
            }

            uow.ContactTasks.Add(task);
            await uow.SaveChangesAsync(cancellationToken);

            await ContactTaskAccess.NotifyOwnerAsync(hub, me, request.ContactId);

            return OperationResult.Success(ContactTaskDto.From(task));
        }

        private static string Preview(string? content, MessageType type)
        {
            if (type == MessageType.Text && !string.IsNullOrWhiteSpace(content))
                return content;

            return type switch
            {
                MessageType.Image => "تصویر",
                MessageType.Document => "فایل",
                MessageType.Audio => "صدا",
                MessageType.Video => "ویدیو",
                MessageType.Location => "موقعیت مکانی",
                _ => string.IsNullOrWhiteSpace(content) ? "پیام" : content!,
            };
        }

        private static OperationResult<ContactTaskDto> Fail(OperationResult op)
            => OperationResult.Failure<ContactTaskDto>(op);
    }
}
