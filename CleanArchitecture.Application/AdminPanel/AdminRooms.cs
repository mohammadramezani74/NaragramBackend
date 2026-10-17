using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    // گروه‌ها (Conversation با IsPrivate = false) و کانال‌ها (Channel) در پنل ادمین؛ Kind: group | channel.

    // ================================================================== DTOها

    public sealed record AdminRoomRow(
        Guid Id, string Title, int Members, int Messages7d, DateTime? LastActivity,
        string? CreatorName, DateTime CreatedAt, bool IsArchived);

    public sealed record AdminRoomsPage(List<AdminRoomRow> Items, int Total, int Page, int PageSize, int AllCount, int ArchivedCount);

    public sealed record AdminRoomMember(Guid UserId, string Name, bool IsAdmin, bool IsCreator, bool IsDisabled);

    public sealed record AdminRoomDetail(
        Guid Id, string Kind, string Title, string? Description, string? CreatorName, DateTime CreatedAt,
        DateTime? ArchivedAt, DateTime? LastActivity, int Messages7d, int MessagesTotal,
        List<int> Daily14, List<AdminRoomMember> Members, List<AdminAuditItem> Log);

    internal static class AdminRoomKind
    {
        public static bool IsValid(string? kind) => kind is "group" or "channel";
    }

    // ================================================================== فهرست

    /// <summary>Status: all | active | archived</summary>
    public sealed record GetAdminRoomsQuery(string Kind, int Page, int PageSize, string? Search, string? Status) : IQuery<AdminRoomsPage>;

    internal sealed class GetAdminRoomsQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetAdminRoomsQuery, AdminRoomsPage>
    {
        public async Task<OperationResult<AdminRoomsPage>> Handle(GetAdminRoomsQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminRoomsPage>(AdminPanelKit.Denied());
            if (!AdminRoomKind.IsValid(request.Kind))
                return OperationResult.Failure<AdminRoomsPage>(new OperationResult().BadRequest("نوع نامعتبر است."));

            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 10, 100);
            var term = request.Search?.Trim();
            var since = DateTime.Now.AddDays(-7);

            List<AdminRoomRow> items;
            int total, all, archived;

            if (request.Kind == "group")
            {
                var baseQuery = uow.Conversation.AsNoTracking().Where(c => !c.IsPrivate && !c.Deleted);
                all = await baseQuery.CountAsync(ct);
                archived = await baseQuery.CountAsync(c => c.ArchivedAt != null, ct);

                var q = baseQuery;
                if (!string.IsNullOrWhiteSpace(term)) q = q.Where(c => c.Title.Contains(term));
                if (request.Status == "active") q = q.Where(c => c.ArchivedAt == null);
                if (request.Status == "archived") q = q.Where(c => c.ArchivedAt != null);

                total = await q.CountAsync(ct);
                var rows = await q
                    .OrderByDescending(c => c.LastMessageSentAt ?? c.CreateDate)
                    .Skip((page - 1) * size).Take(size)
                    .Select(c => new
                    {
                        c.Id, c.Title, Members = c.Users.Count, c.LastMessageSentAt, c.CreateDate, c.ArchivedAt,
                        CreatorLast = c.CreatedByUser != null ? c.CreatedByUser.LastName : null,
                        CreatorFirst = c.CreatedByUser != null ? c.CreatedByUser.FirsName : null
                    })
                    .ToListAsync(ct);

                var ids = rows.Select(r => r.Id).ToList();
                var counts = await uow.Messages.AsNoTracking()
                    .Where(m => !m.Deleted && m.CreateDate >= since && m.ConversationId != null && ids.Contains(m.ConversationId.Value))
                    .GroupBy(m => m.ConversationId!.Value)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                items = rows.Select(r => new AdminRoomRow(
                    r.Id, r.Title, r.Members, counts.FirstOrDefault(x => x.Id == r.Id)?.Count ?? 0, r.LastMessageSentAt,
                    r.CreatorLast is null && r.CreatorFirst is null ? null : AdminPanelKit.Name(r.CreatorLast, r.CreatorFirst),
                    r.CreateDate, r.ArchivedAt != null)).ToList();
            }
            else
            {
                var baseQuery = uow.Channels.AsNoTracking().Where(c => !c.Deleted);
                all = await baseQuery.CountAsync(ct);
                archived = await baseQuery.CountAsync(c => c.ArchivedAt != null, ct);

                var q = baseQuery;
                if (!string.IsNullOrWhiteSpace(term)) q = q.Where(c => c.Title.Contains(term) || c.UserName.Contains(term));
                if (request.Status == "active") q = q.Where(c => c.ArchivedAt == null);
                if (request.Status == "archived") q = q.Where(c => c.ArchivedAt != null);

                total = await q.CountAsync(ct);
                var rows = await q
                    .OrderByDescending(c => c.LastMessageSentAt ?? c.CreateDate)
                    .Skip((page - 1) * size).Take(size)
                    .Select(c => new
                    {
                        c.Id, c.Title, Members = c.Members.Count, c.LastMessageSentAt, c.CreateDate, c.ArchivedAt,
                        CreatorLast = c.CreatedByUser != null ? c.CreatedByUser.LastName : null,
                        CreatorFirst = c.CreatedByUser != null ? c.CreatedByUser.FirsName : null
                    })
                    .ToListAsync(ct);

                var ids = rows.Select(r => r.Id).ToList();
                var counts = await uow.Messages.AsNoTracking()
                    .Where(m => !m.Deleted && m.CreateDate >= since && m.ChannelId != null && ids.Contains(m.ChannelId.Value))
                    .GroupBy(m => m.ChannelId!.Value)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                items = rows.Select(r => new AdminRoomRow(
                    r.Id, r.Title, r.Members, counts.FirstOrDefault(x => x.Id == r.Id)?.Count ?? 0, r.LastMessageSentAt,
                    r.CreatorLast is null && r.CreatorFirst is null ? null : AdminPanelKit.Name(r.CreatorLast, r.CreatorFirst),
                    r.CreateDate, r.ArchivedAt != null)).ToList();
            }

            return OperationResult.Success(new AdminRoomsPage(items, total, page, size, all, archived));
        }
    }

    // ================================================================== جزئیات

    public sealed record GetAdminRoomQuery(string Kind, Guid Id) : IQuery<AdminRoomDetail>;

    internal sealed class GetAdminRoomQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetAdminRoomQuery, AdminRoomDetail>
    {
        private const int MaxMembers = 500;

        public async Task<OperationResult<AdminRoomDetail>> Handle(GetAdminRoomQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminRoomDetail>(AdminPanelKit.Denied());
            if (!AdminRoomKind.IsValid(request.Kind))
                return OperationResult.Failure<AdminRoomDetail>(new OperationResult().BadRequest("نوع نامعتبر است."));

            var today = DateTime.Now.Date;
            var from = today.AddDays(-13);
            var weekAgo = DateTime.Now.AddDays(-7);
            var isGroup = request.Kind == "group";

            string title; string? description; Guid? creatorId; DateTime createdAt; DateTime? archivedAt; DateTime? last;
            List<AdminRoomMember> members;

            if (isGroup)
            {
                var c = await uow.Conversation.AsNoTracking()
                    .Where(x => x.Id == request.Id && !x.IsPrivate && !x.Deleted)
                    .Select(x => new { x.Title, x.Description, x.CreatedByUserId, x.CreateDate, x.ArchivedAt, x.LastMessageSentAt })
                    .FirstOrDefaultAsync(ct);
                if (c is null)
                    return OperationResult.Failure<AdminRoomDetail>(new OperationResult().NotFound("گروه یافت نشد."));
                (title, description, creatorId, createdAt, archivedAt, last) = (c.Title, c.Description, c.CreatedByUserId, c.CreateDate, c.ArchivedAt, c.LastMessageSentAt);

                var rows = await uow.ConversationUser.AsNoTracking()
                    .Where(cu => cu.ConversationId == request.Id)
                    .OrderByDescending(cu => cu.Role)
                    .Take(MaxMembers)
                    .Select(cu => new { cu.UserId, cu.User.LastName, cu.User.FirsName, cu.Role, cu.User.IsDisabled })
                    .ToListAsync(ct);
                members = rows.Select(r => new AdminRoomMember(
                    r.UserId, AdminPanelKit.Name(r.LastName, r.FirsName),
                    r.Role == ConversationRole.Owner || r.Role == ConversationRole.Admin,
                    r.UserId == creatorId || r.Role == ConversationRole.Owner, r.IsDisabled)).ToList();
            }
            else
            {
                var c = await uow.Channels.AsNoTracking()
                    .Where(x => x.Id == request.Id && !x.Deleted)
                    .Select(x => new { x.Title, x.Description, x.CreatedByUserId, x.CreateDate, x.ArchivedAt, x.LastMessageSentAt })
                    .FirstOrDefaultAsync(ct);
                if (c is null)
                    return OperationResult.Failure<AdminRoomDetail>(new OperationResult().NotFound("کانال یافت نشد."));
                (title, description, creatorId, createdAt, archivedAt, last) = (c.Title, c.Description, c.CreatedByUserId, c.CreateDate, c.ArchivedAt, c.LastMessageSentAt);

                var adminIds = await uow.ChannelAdmins.AsNoTracking()
                    .Where(a => a.ChannelId == request.Id).Select(a => a.UserId).ToListAsync(ct);
                var rows = await uow.ChannelMembers.AsNoTracking()
                    .Where(cm => cm.ChannelId == request.Id)
                    .Take(MaxMembers)
                    .Select(cm => new { cm.UserId, cm.User.LastName, cm.User.FirsName, cm.User.IsDisabled })
                    .ToListAsync(ct);
                members = rows.Select(r => new AdminRoomMember(
                        r.UserId, AdminPanelKit.Name(r.LastName, r.FirsName),
                        adminIds.Contains(r.UserId) || r.UserId == creatorId, r.UserId == creatorId, r.IsDisabled))
                    .OrderByDescending(m => m.IsCreator).ThenByDescending(m => m.IsAdmin).ThenBy(m => m.Name)
                    .ToList();
            }

            var messages = uow.Messages.AsNoTracking().Where(m => !m.Deleted
                && (isGroup ? m.ConversationId == request.Id : m.ChannelId == request.Id));
            var total = await messages.CountAsync(ct);
            var week = await messages.CountAsync(m => m.CreateDate >= weekAgo, ct);
            var perDay = await messages
                .Where(m => m.CreateDate >= from)
                .GroupBy(m => m.CreateDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var daily = Enumerable.Range(0, 14)
                .Select(i => perDay.FirstOrDefault(p => p.Day == from.AddDays(i))?.Count ?? 0)
                .ToList();

            string? creatorName = null;
            if (creatorId is Guid cid)
            {
                var cr = await uow.Users.AsNoTracking().Where(u => u.Id == cid)
                    .Select(u => new { u.LastName, u.FirsName }).FirstOrDefaultAsync(ct);
                if (cr is not null) creatorName = AdminPanelKit.Name(cr.LastName, cr.FirsName);
            }

            return OperationResult.Success(new AdminRoomDetail(
                request.Id, request.Kind, title, string.IsNullOrWhiteSpace(description) ? null : description,
                creatorName, createdAt, archivedAt, last, week, total, daily, members,
                await AdminPanelKit.LogForAsync(uow, request.Id, 30, ct)));
        }
    }

    // ================================================================== ویرایش

    public sealed record EditRoomCommand(string Kind, Guid Id, string Title, string? Description) : ICommands;

    internal sealed class EditRoomCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<EditRoomCommand>
    {
        public async Task<OperationResult> Handle(EditRoomCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();
            if (!AdminRoomKind.IsValid(request.Kind))
                return new OperationResult().BadRequest("نوع نامعتبر است.");

            var title = (request.Title ?? string.Empty).Trim();
            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (title.Length is < 1 or > 100)
                return new OperationResult().BadRequest("نام باید ۱ تا ۱۰۰ حرف باشد.");
            if (description?.Length > 500)
                return new OperationResult().BadRequest("توضیح حداکثر ۵۰۰ حرف است.");

            string before;
            if (request.Kind == "group")
            {
                var group = await uow.Conversation.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsPrivate && !c.Deleted, ct);
                if (group is null) return new OperationResult().NotFound("گروه یافت نشد.");
                before = group.Title;
                group.EditByAdmin(title, description);
            }
            else
            {
                var channel = await uow.Channels.FirstOrDefaultAsync(c => c.Id == request.Id && !c.Deleted, ct);
                if (channel is null) return new OperationResult().NotFound("کانال یافت نشد.");
                before = channel.Title;
                channel.EditByAdmin(title, description);
            }

            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "room.edit", request.Kind, request.Id, title,
                before == title ? null : "نام قبلی: " + before);
            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess ? new OperationResult().succedded() : new OperationResult().Failed("ذخیره نشد: " + saved.Message);
        }
    }

    // ================================================================== کارها

    /// <summary>Action: archive | restore | remove-member (برای remove-member، MemberId لازم است).</summary>
    public sealed record ManageRoomCommand(string Kind, Guid Id, string Action, Guid? MemberId, string? Reason) : ICommands;

    internal sealed class ManageRoomCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users,
        CleanArchitecture.Application.Hubs.HubRoomMembership rooms)
        : ICommandHandler<ManageRoomCommand>
    {
        public async Task<OperationResult> Handle(ManageRoomCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();
            if (!AdminRoomKind.IsValid(request.Kind) || request.Action is not ("archive" or "restore" or "remove-member"))
                return new OperationResult().BadRequest("درخواست نامعتبر است.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            string title;
            string? details = request.Reason;

            if (request.Kind == "group")
            {
                var group = await uow.Conversation.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsPrivate && !c.Deleted, ct);
                if (group is null) return new OperationResult().NotFound("گروه یافت نشد.");
                title = group.Title;

                if (request.Action == "archive") group.Archive();
                else if (request.Action == "restore") group.Restore();
                else
                {
                    var member = await uow.ConversationUser.Include(cu => cu.User)
                        .FirstOrDefaultAsync(cu => cu.ConversationId == request.Id && cu.UserId == request.MemberId, ct);
                    if (member is null) return new OperationResult().NotFound("این کاربر عضو گروه نیست.");
                    if (member.Role == ConversationRole.Owner || member.UserId == group.CreatedByUserId)
                        return new OperationResult().BadRequest("سازنده‌ی گروه را نمی‌شود بیرون کرد.");
                    uow.ConversationUser.Remove(member);
                    details = AdminPanelKit.Name(member.User?.LastName, member.User?.FirsName) + (string.IsNullOrWhiteSpace(request.Reason) ? "" : " · " + request.Reason);
                }
            }
            else
            {
                var channel = await uow.Channels.FirstOrDefaultAsync(c => c.Id == request.Id && !c.Deleted, ct);
                if (channel is null) return new OperationResult().NotFound("کانال یافت نشد.");
                title = channel.Title;

                if (request.Action == "archive") channel.Archive();
                else if (request.Action == "restore") channel.Restore();
                else
                {
                    if (request.MemberId == channel.CreatedByUserId)
                        return new OperationResult().BadRequest("سازنده‌ی کانال را نمی‌شود بیرون کرد.");
                    var member = await uow.ChannelMembers.Include(cm => cm.User)
                        .FirstOrDefaultAsync(cm => cm.ChannelId == request.Id && cm.UserId == request.MemberId, ct);
                    if (member is null) return new OperationResult().NotFound("این کاربر عضو کانال نیست.");
                    uow.ChannelMembers.Remove(member);
                    var adminRows = await uow.ChannelAdmins.Where(a => a.ChannelId == request.Id && a.UserId == request.MemberId).ToListAsync(ct);
                    uow.ChannelAdmins.RemoveRange(adminRows);
                    details = AdminPanelKit.Name(member.User?.LastName, member.User?.FirsName) + (string.IsNullOrWhiteSpace(request.Reason) ? "" : " · " + request.Reason);
                }
            }

            AdminPanelKit.Audit(uow, actor, "room." + request.Action, request.Kind, request.Id, title, details);
            var saved = await uow.SaveChangesAsync(ct);
            if (!saved.IsSuccess)
                return new OperationResult().Failed("ذخیره نشد: " + saved.Message);
            // عضو بیرون‌شده همان لحظه پیام‌های زنده را نگیرد و گفتگو از فهرستش برداشته شود.
            if (request.Action == "remove-member" && request.MemberId is Guid removed)
                await rooms.RemovedAsync(request.Id, removed);
            return new OperationResult().succedded();
        }
    }
}
