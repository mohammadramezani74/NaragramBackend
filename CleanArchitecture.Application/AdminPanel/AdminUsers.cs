using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Hubs.Services;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using Microsoft.AspNetCore.SignalR;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== DTOها

    public sealed record AdminRoleRef(Guid Id, string Name);

    public sealed record AdminUserRow(
        Guid Id, string Name, string? Post, string? City, string PhoneMasked,
        List<AdminRoleRef> Roles, DateTime? LastSeen, bool Online, int Messages7d,
        bool IsDisabled, DateTime? CreatedAt, bool IsAdmin);

    public sealed record AdminUsersPage(
        List<AdminUserRow> Items, int Total, int Page, int PageSize,
        List<string> Cities, int AllCount, int ActiveCount, int QuietCount, int DisabledCount);

    /// <summary>Kind: group | channel | bot</summary>
    public sealed record AdminMembership(Guid Id, string Name, string Kind, bool IsAdmin, bool IsArchived);

    public sealed record AdminUserDetail(
        Guid Id, string Name, string? Post, string? City, string PhoneMasked,
        List<AdminRoleRef> Roles, DateTime? LastSeen, bool Online, DateTime? CreatedAt,
        int Messages7d, int MessagesTotal, bool IsDisabled, DateTime? DisabledAt, bool IsAdmin,
        List<AdminMembership> Memberships, List<AdminAuditItem> Log,
        string NameFirst, string NameLast);

    // ================================================================== فهرست

    /// <summary>Status: all | active | quiet | disabled. فعال = در ۷ روز اخیر آنلاین بوده.</summary>
    public sealed record GetAdminUsersQuery(int Page, int PageSize, string? Search, string? Status, string? City, Guid? RoleId)
        : IQuery<AdminUsersPage>;

    internal sealed class GetAdminUsersQueryHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IAdminIdentityService identity, UserOnlineService online)
        : IQueryHandler<GetAdminUsersQuery, AdminUsersPage>
    {
        public async Task<OperationResult<AdminUsersPage>> Handle(GetAdminUsersQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminUsersPage>(AdminPanelKit.Denied());

            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 10, 100);
            var weekAgo = DateTimeOffset.Now.AddDays(-7);
            var onlineIds = online.OnlineUsers.Values.Select(x => x.UserId).Distinct().ToList();

            var people = uow.Users.AsNoTracking().Where(u => !u.IsBot);

            // شمارش‌های بالای صفحه (بدون فیلترهای جستجو)
            var allCount = await people.CountAsync(ct);
            var disabledCount = await people.CountAsync(u => u.IsDisabled, ct);
            var activeCount = await people.CountAsync(u => !u.IsDisabled
                && ((u.LastLoginDate != null && u.LastLoginDate >= weekAgo) || onlineIds.Contains(u.Id)), ct);

            var query = people;
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                var digits = AdminPanelKit.LatinDigits(term);
                query = query.Where(u =>
                    (u.FirsName != null && u.FirsName.Contains(term)) ||
                    (u.LastName != null && u.LastName.Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(digits)));
            }

            query = request.Status switch
            {
                "active" => query.Where(u => !u.IsDisabled && ((u.LastLoginDate != null && u.LastLoginDate >= weekAgo) || onlineIds.Contains(u.Id))),
                "quiet" => query.Where(u => !u.IsDisabled && (u.LastLoginDate == null || u.LastLoginDate < weekAgo) && !onlineIds.Contains(u.Id)),
                "disabled" => query.Where(u => u.IsDisabled),
                _ => query,
            };

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();
                query = city == "نامشخص"
                    ? query.Where(u => u.Address!.City == null || u.Address!.City == "")
                    : query.Where(u => u.Address!.City == city);
            }

            if (request.RoleId is Guid roleId)
            {
                var inRole = await identity.UserIdsInRoleAsync(roleId, ct);
                query = query.Where(u => inRole.Contains(u.Id));
            }

            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderByDescending(u => u.LastLoginDate)
                .ThenBy(u => u.LastName)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new
                {
                    u.Id, u.FirsName, u.LastName, u.bio, City = u.Address!.City, u.PhoneNumber,
                    u.LastLoginDate, u.IsDisabled, u.CreatedAt
                })
                .ToListAsync(ct);

            var ids = rows.Select(r => r.Id).ToList();
            var roles = await identity.RolesOfUsersAsync(ids, ct);
            var since = DateTime.Now.AddDays(-7);
            var counts = await uow.Messages.AsNoTracking()
                .Where(m => !m.Deleted && m.CreateDate >= since && m.CreatedByUserId != null && ids.Contains(m.CreatedByUserId.Value))
                .GroupBy(m => m.CreatedByUserId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var items = rows.Select(r => new AdminUserRow(
                    r.Id,
                    AdminPanelKit.Name(r.LastName, r.FirsName),
                    string.IsNullOrWhiteSpace(r.bio) ? null : r.bio,
                    string.IsNullOrWhiteSpace(r.City) ? null : r.City,
                    AdminPanelKit.MaskPhone(r.PhoneNumber),
                    roles.TryGetValue(r.Id, out var rs) ? rs.Select(x => new AdminRoleRef(x.Id, x.Name)).ToList() : new List<AdminRoleRef>(),
                    r.LastLoginDate?.LocalDateTime,
                    onlineIds.Contains(r.Id),
                    counts.FirstOrDefault(c => c.Id == r.Id)?.Count ?? 0,
                    r.IsDisabled,
                    r.CreatedAt?.LocalDateTime,
                    admin.IsAdmin(r.Id)))
                .ToList();

            var cities = await people
                .GroupBy(u => u.Address!.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return OperationResult.Success(new AdminUsersPage(
                items, total, page, size,
                cities.Select(c => string.IsNullOrWhiteSpace(c.City) ? "نامشخص" : c.City.Trim()).Distinct().ToList(),
                allCount, activeCount, Math.Max(0, allCount - activeCount - disabledCount), disabledCount));
        }
    }

    // ================================================================== جزئیات

    public sealed record GetAdminUserQuery(Guid Id) : IQuery<AdminUserDetail>;

    internal sealed class GetAdminUserQueryHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IAdminIdentityService identity, UserOnlineService online)
        : IQueryHandler<GetAdminUserQuery, AdminUserDetail>
    {
        public async Task<OperationResult<AdminUserDetail>> Handle(GetAdminUserQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminUserDetail>(AdminPanelKit.Denied());

            var u = await uow.Users.AsNoTracking()
                .Where(x => x.Id == request.Id && !x.IsBot)
                .Select(x => new
                {
                    x.Id, x.FirsName, x.LastName, x.bio, City = x.Address!.City, x.PhoneNumber,
                    x.LastLoginDate, x.IsDisabled, x.DisabledAt, x.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
            if (u is null)
                return OperationResult.Failure<AdminUserDetail>(new OperationResult().NotFound("کاربر یافت نشد."));

            var roles = await identity.RolesOfUsersAsync(new[] { u.Id }, ct);
            var since = DateTime.Now.AddDays(-7);
            var mine = uow.Messages.AsNoTracking().Where(m => !m.Deleted && m.CreatedByUserId == u.Id);
            var week = await mine.CountAsync(m => m.CreateDate >= since, ct);
            var total = await mine.CountAsync(ct);

            var groups = await uow.ConversationUser.AsNoTracking()
                .Where(cu => cu.UserId == u.Id && !cu.Conversation.IsPrivate && !cu.Conversation.Deleted)
                .Select(cu => new AdminMembership(
                    cu.ConversationId, cu.Conversation.Title, "group",
                    cu.Role == ConversationRole.Owner || cu.Role == ConversationRole.Admin,
                    cu.Conversation.ArchivedAt != null))
                .ToListAsync(ct);

            var channels = await uow.ChannelMembers.AsNoTracking()
                .Where(cm => cm.UserId == u.Id && !cm.Channel.Deleted)
                .Select(cm => new AdminMembership(
                    cm.ChannelId, cm.Channel.Title, "channel",
                    cm.Channel.CreatedByUserId == u.Id || cm.Channel.Admins.Any(a => a.UserId == u.Id),
                    cm.Channel.ArchivedAt != null))
                .ToListAsync(ct);

            var bots = await uow.BotSessions.AsNoTracking()
                .Where(s => s.UserId == u.Id && s.StartedAt != null && s.Bot != null)
                .Select(s => new AdminMembership(s.BotId, s.Bot!.User != null ? s.Bot.User.FirsName ?? s.Bot.Handle : s.Bot.Handle, "bot", false, false))
                .ToListAsync(ct);

            return OperationResult.Success(new AdminUserDetail(
                u.Id,
                AdminPanelKit.Name(u.LastName, u.FirsName),
                string.IsNullOrWhiteSpace(u.bio) ? null : u.bio,
                string.IsNullOrWhiteSpace(u.City) ? null : u.City,
                AdminPanelKit.MaskPhone(u.PhoneNumber),
                roles.TryGetValue(u.Id, out var rs) ? rs.Select(x => new AdminRoleRef(x.Id, x.Name)).ToList() : new List<AdminRoleRef>(),
                u.LastLoginDate?.LocalDateTime,
                online.OnlineUsers.Values.Any(x => x.UserId == u.Id),
                u.CreatedAt?.LocalDateTime,
                week, total, u.IsDisabled, u.DisabledAt?.LocalDateTime, admin.IsAdmin(u.Id),
                groups.Concat(channels).Concat(bots).ToList(),
                await AdminPanelKit.LogForAsync(uow, u.Id, 30, ct),
                u.LastName ?? string.Empty, u.FirsName ?? string.Empty));
        }
    }

    // ================================================================== کارها

    /// <summary>شماره‌ی کامل؛ هر بار نمایش در رویدادها ثبت می‌شود.</summary>
    public sealed record RevealUserPhoneCommand(Guid Id) : ICommands<string>;

    internal sealed class RevealUserPhoneCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<RevealUserPhoneCommand, string>
    {
        public async Task<OperationResult<string>> Handle(RevealUserPhoneCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<string>(AdminPanelKit.Denied());

            var u = await uow.Users.AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new { x.PhoneNumber, x.FirsName, x.LastName })
                .FirstOrDefaultAsync(ct);
            if (u is null)
                return OperationResult.Failure<string>(new OperationResult().NotFound("کاربر یافت نشد."));

            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "user.phone", "user", request.Id, AdminPanelKit.Name(u.LastName, u.FirsName));
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return OperationResult.Failure<string>(new OperationResult().Failed("ثبت رویداد انجام نشد: " + saved.Message));

            return OperationResult.Success(u.PhoneNumber ?? string.Empty);
        }
    }

    /// <summary>Action: disable | enable | sessions</summary>
    public sealed record SetUserAccessCommand(Guid Id, string Action, string? Reason) : ICommands;

    internal sealed class SetUserAccessCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users,
        IUserAccessGuard guard, HubConnectionRegistry hubs, IHubContext<NaraHub, IChatHubClient> hub)
        : ICommandHandler<SetUserAccessCommand>
    {
        public async Task<OperationResult> Handle(SetUserAccessCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            if (request.Action is not ("disable" or "enable" or "sessions"))
                return new OperationResult().BadRequest("کار نامعتبر است.");

            if (request.Action != "enable" && (request.Id == users.UserId || admin.IsAdmin(request.Id)))
                return new OperationResult().BadRequest("حساب ادمین‌ها را از پنل نمی‌شود غیرفعال یا خارج کرد.");

            var user = await uow.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsBot, ct);
            if (user is null)
                return new OperationResult().NotFound("کاربر یافت نشد.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            var name = AdminPanelKit.Name(user.LastName, user.FirsName);
            switch (request.Action)
            {
                case "disable":
                    if (user.IsDisabled) return new OperationResult().succedded();
                    user.Disable();
                    AdminPanelKit.Audit(uow, actor, "user.disable", "user", user.Id, name, request.Reason);
                    break;
                case "enable":
                    if (!user.IsDisabled) return new OperationResult().succedded();
                    user.Enable();
                    AdminPanelKit.Audit(uow, actor, "user.enable", "user", user.Id, name, request.Reason);
                    break;
                default:
                    user.RevokeSessions();
                    AdminPanelKit.Audit(uow, actor, "user.sessions", "user", user.Id, name, request.Reason);
                    break;
            }

            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return new OperationResult().Failed("ذخیره نشد: " + saved.Message);

            // بعد از ذخیره: توکن‌ها از همین لحظه رد می‌شوند و اتصال زنده بسته می‌شود.
            switch (request.Action)
            {
                case "disable":
                    guard.MarkDisabled(user.Id, user.DisabledAt ?? DateTimeOffset.Now);
                    await KickAsync(user.Id, "حساب شما غیرفعال شد. برای پیگیری با پشتیبانی تماس بگیرید.");
                    break;
                case "enable":
                    guard.MarkEnabled(user.Id);
                    break;
                default:
                    guard.MarkSessionsRevoked(user.Id, user.SessionsRevokedAt ?? DateTimeOffset.Now);
                    await KickAsync(user.Id, "از همه‌ی دستگاه‌ها خارج شدید؛ لطفا دوباره وارد شوید.");
                    break;
            }

            return new OperationResult().succedded();
        }

        /// <summary>به برنامه‌ی کاربر خبر می‌دهد تا خودش خارج شود، و کمی بعد اتصال زنده‌اش را می‌بندد.
        /// توکن‌هایش از همین الان رد می‌شوند، پس این فاصله‌ی کوتاه دسترسی تازه نمی‌دهد.</summary>
        private async Task KickAsync(Guid userId, string message)
        {
            try
            {
                await hub.Clients.User(userId.ToString()).ForceLogout(message);
            }
            catch (Exception)
            {
                // اتصالی نبود.
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                hubs.Abort(userId);
            });
        }
    }

    public sealed record SetUserRolesCommand(Guid Id, List<Guid> RoleIds) : ICommands;

    internal sealed class SetUserRolesCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, IAdminIdentityService identity)
        : ICommandHandler<SetUserRolesCommand>
    {
        public async Task<OperationResult> Handle(SetUserRolesCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var result = await identity.SetUserRolesAsync(request.Id, request.RoleIds ?? new List<Guid>(), ct);
            if (!result.IsSucceded)
                return result;

            var (added, removed) = result.result;
            if (added.Count + removed.Count == 0)
                return new OperationResult().succedded();

            var target = await uow.Users.AsNoTracking().Where(u => u.Id == request.Id)
                .Select(u => new { u.FirsName, u.LastName }).FirstOrDefaultAsync(ct);
            var details = string.Join("، ", added.Select(x => "+" + x).Concat(removed.Select(x => "−" + x)));
            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "user.roles", "user", request.Id,
                AdminPanelKit.Name(target?.LastName, target?.FirsName), details);
            await uow.SaveChangesAsync(ct);
            return new OperationResult().succedded();
        }
    }

    // ================================================================== تغییر نام

    /// <summary>FirstName = نامی که اول نمایش داده می‌شود (ستون LastName)، LastName = دومی (ستون FirsName).</summary>
    public sealed record RenameUserCommand(Guid Id, string FirstName, string LastName) : ICommands;

    internal sealed class RenameUserCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<RenameUserCommand>
    {
        public async Task<OperationResult> Handle(RenameUserCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var first = (request.FirstName ?? string.Empty).Trim();
            var last = (request.LastName ?? string.Empty).Trim();
            if (first.Length is < 1 or > 50 || last.Length > 50)
                return new OperationResult().BadRequest("نام باید ۱ تا ۵۰ حرف باشد.");

            var user = await uow.Users.FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsBot, ct);
            if (user is null)
                return new OperationResult().NotFound("کاربر یافت نشد.");

            var before = AdminPanelKit.Name(user.LastName, user.FirsName);
            // نمایش برنامه «LastName FirsName» است؛ پس نام اول در LastName می‌نشیند.
            user.RenameByAdmin(first, last);
            var after = AdminPanelKit.Name(user.LastName, user.FirsName);
            if (before == after)
                return new OperationResult().succedded();

            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "user.rename", "user", user.Id, after, "نام قبلی: " + before);
            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess ? new OperationResult().succedded() : new OperationResult().Failed("ذخیره نشد: " + saved.Message);
        }
    }

    // ================================================================== نقش‌ها

    public sealed record GetAdminRolesQuery : IQuery<List<AdminRoleInfo>>;

    internal sealed class GetAdminRolesQueryHandler(IAdminAccess admin, IAdminIdentityService identity)
        : IQueryHandler<GetAdminRolesQuery, List<AdminRoleInfo>>
    {
        public async Task<OperationResult<List<AdminRoleInfo>>> Handle(GetAdminRolesQuery request, CancellationToken ct)
            => admin.CurrentUserIsAdmin
                ? OperationResult.Success(await identity.RolesAsync(ct))
                : OperationResult.Failure<List<AdminRoleInfo>>(AdminPanelKit.Denied());
    }

    /// <summary>Action: create | rename | delete (برای create، Id خالی).</summary>
    public sealed record ManageRoleCommand(string Action, Guid? Id, string? Name) : ICommands;

    internal sealed class ManageRoleCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, IAdminIdentityService identity)
        : ICommandHandler<ManageRoleCommand>
    {
        public async Task<OperationResult> Handle(ManageRoleCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var before = request.Id is Guid id0
                ? (await identity.RolesAsync(ct)).FirstOrDefault(r => r.Id == id0)?.Name
                : null;

            OperationResult result;
            Guid? targetId = request.Id;
            switch (request.Action)
            {
                case "create":
                    var created = await identity.CreateRoleAsync(request.Name ?? string.Empty, ct);
                    result = created;
                    if (created.IsSucceded) targetId = created.result;
                    break;
                case "rename" when request.Id is Guid id1:
                    result = await identity.RenameRoleAsync(id1, request.Name ?? string.Empty, ct);
                    break;
                case "delete" when request.Id is Guid id2:
                    result = await identity.DeleteRoleAsync(id2, ct);
                    break;
                default:
                    return new OperationResult().BadRequest("کار نامعتبر است.");
            }

            if (!result.IsSucceded)
                return result;

            var name = request.Action == "delete" ? before ?? string.Empty : (request.Name ?? string.Empty).Trim();
            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "role." + request.Action, "role", targetId, name,
                request.Action == "rename" && before is not null ? "نام قبلی: " + before : null);
            await uow.SaveChangesAsync(ct);
            return new OperationResult().succedded();
        }
    }
}
