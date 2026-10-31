using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== ورود به پنل (تأیید دوم)

    public sealed record AdminMeDto(Guid UserId, string Name, bool IsOwner, List<string> Permissions);

    public sealed record SendAdminCodeCommand : ICommands<AdminCodeResult>;

    internal sealed class SendAdminCodeCommandHandler(IAdminAccess admin, IApplicationUserManager users, IAdminSessionService sessions)
        : ICommandHandler<SendAdminCodeCommand, AdminCodeResult>
    {
        public async Task<OperationResult<AdminCodeResult>> Handle(SendAdminCodeCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin || users.UserId is not Guid me)
                return OperationResult.Failure<AdminCodeResult>(AdminPanelKit.Denied());

            // نتیجه همیشه برمی‌گردد تا کلاینت پیام و زمان انتظار را نشان دهد
            return OperationResult.Success(await sessions.SendCodeAsync(me, ct));
        }
    }

    public sealed record VerifyAdminCodeCommand(string Code) : ICommands<AdminVerifyResult>;

    internal sealed class VerifyAdminCodeCommandHandler(
        IAdminAccess admin, IApplicationUserManager users, IAdminSessionService sessions,
        IApplicationUnitOfWork uow, IHttpContextAccessor http)
        : ICommandHandler<VerifyAdminCodeCommand, AdminVerifyResult>
    {
        public async Task<OperationResult<AdminVerifyResult>> Handle(VerifyAdminCodeCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin || users.UserId is not Guid me)
                return OperationResult.Failure<AdminVerifyResult>(AdminPanelKit.Denied());

            var result = await sessions.VerifyAsync(me, request.Code ?? string.Empty, ct);
            if (result.Ok)
            {
                var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
                var ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString();
                AdminPanelKit.Audit(uow, actor, "admin.login", "admin", me, actor.Name, ip is null ? null : "IP: " + ip);
                await uow.SaveChangesAsync(ct);
            }

            return OperationResult.Success(result);
        }
    }

    public sealed record GetAdminMeQuery : IQuery<AdminMeDto>;

    internal sealed class GetAdminMeQueryHandler(IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow)
        : IQueryHandler<GetAdminMeQuery, AdminMeDto>
    {
        public async Task<OperationResult<AdminMeDto>> Handle(GetAdminMeQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin || users.UserId is not Guid me)
                return OperationResult.Failure<AdminMeDto>(AdminPanelKit.Denied());

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            return OperationResult.Success(new AdminMeDto(
                me, actor.Name, admin.IsOwner(me),
                admin.PermissionsOf(me).Where(p => p != PanelPermissions.Owner).OrderBy(p => p).ToList()));
        }
    }

    public sealed record AdminLogoutCommand(string? Token) : ICommands;

    internal sealed class AdminLogoutCommandHandler(IAdminSessionService sessions) : ICommandHandler<AdminLogoutCommand>
    {
        public Task<OperationResult> Handle(AdminLogoutCommand request, CancellationToken ct)
        {
            sessions.Revoke(request.Token);
            return Task.FromResult(new OperationResult().succedded());
        }
    }

    // ================================================================== صفحه‌ی مدیران (فقط مالک)

    public sealed record AdminManagerItem(
        Guid UserId, string Name, string Phone, bool IsOwner, List<string> Permissions,
        DateTime? GrantedAt, string? GrantedBy, int ActiveSessions);

    public sealed record AdminManagersDto(List<AdminManagerItem> Owners, List<AdminManagerItem> Managers, IReadOnlyList<AdminPermissionInfo> Catalog);

    public sealed record AdminCandidate(Guid Id, string Name, string Phone, bool IsManager, bool IsOwner);

    internal static class AdminManagersKit
    {
        public static bool IsCurrentOwner(IAdminAccess admin, IApplicationUserManager users)
            => admin.IsOwner(users.UserId);
    }

    public sealed record GetAdminManagersQuery : IQuery<AdminManagersDto>;

    internal sealed class GetAdminManagersQueryHandler(
        IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow,
        IAppSettingsStore settings, IOptionsMonitor<AdminOptions> options, IAdminSessionService sessions)
        : IQueryHandler<GetAdminManagersQuery, AdminManagersDto>
    {
        public async Task<OperationResult<AdminManagersDto>> Handle(GetAdminManagersQuery request, CancellationToken ct)
        {
            if (!AdminManagersKit.IsCurrentOwner(admin, users))
                return OperationResult.Failure<AdminManagersDto>(AdminPanelKit.Denied());

            var owners = options.CurrentValue.UserIds.Distinct().ToList();
            var grants = (await settings.AdminGrantsAsync(ct)).Where(g => !owners.Contains(g.UserId)).ToList();
            var ids = owners.Concat(grants.Select(g => g.UserId)).ToList();

            var people = await uow.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FirsName, u.LastName, u.PhoneNumber })
                .ToDictionaryAsync(u => u.Id, ct);

            string NameOf(Guid id) => people.TryGetValue(id, out var p) ? AdminPanelKit.Name(p.LastName, p.FirsName) : "کاربر حذف‌شده";
            string PhoneOf(Guid id) => people.TryGetValue(id, out var p) ? AdminPanelKit.MaskPhone(p.PhoneNumber) : "—";

            var ownerItems = owners
                .Select(id => new AdminManagerItem(id, NameOf(id), PhoneOf(id), true,
                    PanelPermissions.All.Select(p => p.Key).ToList(), null, null, sessions.ActiveCount(id)))
                .ToList();

            var managerItems = grants
                .OrderBy(g => NameOf(g.UserId))
                .Select(g => new AdminManagerItem(g.UserId, NameOf(g.UserId), PhoneOf(g.UserId), false,
                    g.Permissions.Where(PanelPermissions.IsKnown).ToList(), g.GrantedAt, g.GrantedBy, sessions.ActiveCount(g.UserId)))
                .ToList();

            return OperationResult.Success(new AdminManagersDto(ownerItems, managerItems, PanelPermissions.All));
        }
    }

    public sealed record SearchAdminCandidatesQuery(string? Q) : IQuery<List<AdminCandidate>>;

    internal sealed class SearchAdminCandidatesQueryHandler(
        IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow, IAppSettingsStore settings)
        : IQueryHandler<SearchAdminCandidatesQuery, List<AdminCandidate>>
    {
        public async Task<OperationResult<List<AdminCandidate>>> Handle(SearchAdminCandidatesQuery request, CancellationToken ct)
        {
            if (!AdminManagersKit.IsCurrentOwner(admin, users))
                return OperationResult.Failure<List<AdminCandidate>>(AdminPanelKit.Denied());

            var q = AdminPanelKit.LatinDigits((request.Q ?? string.Empty).Trim());
            if (q.Length < 2)
                return OperationResult.Success(new List<AdminCandidate>());

            var rows = await uow.Users.AsNoTracking()
                .Where(u => !u.IsBot && !u.IsDisabled)
                .Where(u => (u.FirsName != null && u.FirsName.Contains(q))
                         || (u.LastName != null && u.LastName.Contains(q))
                         || (u.UserName != null && u.UserName.Contains(q))
                         || (u.PhoneNumber != null && u.PhoneNumber.Contains(q)))
                .OrderBy(u => u.LastName).ThenBy(u => u.FirsName)
                .Take(12)
                .Select(u => new { u.Id, u.FirsName, u.LastName, u.PhoneNumber })
                .ToListAsync(ct);

            var managers = (await settings.AdminGrantsAsync(ct)).Select(g => g.UserId).ToHashSet();
            return OperationResult.Success(rows
                .Select(u => new AdminCandidate(u.Id, AdminPanelKit.Name(u.LastName, u.FirsName),
                    AdminPanelKit.MaskPhone(u.PhoneNumber), managers.Contains(u.Id), admin.IsOwner(u.Id)))
                .ToList());
        }
    }

    public sealed record SaveAdminGrantCommand(Guid UserId, List<string> Permissions) : ICommands;

    internal sealed class SaveAdminGrantCommandHandler(
        IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow, IAppSettingsStore settings)
        : ICommandHandler<SaveAdminGrantCommand>
    {
        public async Task<OperationResult> Handle(SaveAdminGrantCommand request, CancellationToken ct)
        {
            if (!AdminManagersKit.IsCurrentOwner(admin, users))
                return AdminPanelKit.Denied();

            if (admin.IsOwner(request.UserId))
                return new OperationResult().BadRequest("دسترسی مالک‌ها از تنظیمات سرور می‌آید و از اینجا تغییر نمی‌کند.");

            var perms = (request.Permissions ?? new List<string>())
                .Where(PanelPermissions.IsKnown).Distinct().ToList();
            if (perms.Count == 0)
                return new OperationResult().BadRequest("حداقل یک دسترسی را انتخاب کنید؛ برای حذف از مدیران از دکمه‌ی حذف استفاده کنید.");

            var user = await uow.Users.AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => new { u.FirsName, u.LastName, u.IsBot, u.IsDisabled, u.PhoneNumber })
                .FirstOrDefaultAsync(ct);
            if (user is null || user.IsBot)
                return new OperationResult().NotFound("کاربر یافت نشد.");
            if (user.IsDisabled)
                return new OperationResult().BadRequest("این حساب غیرفعال است.");
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                return new OperationResult().BadRequest("این کاربر شماره‌ی موبایل ندارد و نمی‌تواند کد ورود پنل را بگیرد.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            var grants = (await settings.AdminGrantsAsync(ct)).Where(g => g.UserId != request.UserId).ToList();
            grants.Add(new AdminGrant(request.UserId, perms, DateTime.Now, actor.Name));

            var titles = PanelPermissions.All.Where(p => perms.Contains(p.Key)).Select(p => p.Title);
            AdminPanelKit.Audit(uow, actor, "admin.grant", "user", request.UserId,
                AdminPanelKit.Name(user.LastName, user.FirsName), string.Join("، ", titles));
            await settings.SaveAsync(AppSettingsStore.AdminGrantsKey, grants, actor.Name, ct);
            return new OperationResult().succedded();
        }
    }

    public sealed record RemoveAdminGrantCommand(Guid UserId) : ICommands;

    internal sealed class RemoveAdminGrantCommandHandler(
        IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow,
        IAppSettingsStore settings, IAdminSessionService sessions)
        : ICommandHandler<RemoveAdminGrantCommand>
    {
        public async Task<OperationResult> Handle(RemoveAdminGrantCommand request, CancellationToken ct)
        {
            if (!AdminManagersKit.IsCurrentOwner(admin, users))
                return AdminPanelKit.Denied();

            var all = await settings.AdminGrantsAsync(ct);
            var grants = all.Where(g => g.UserId != request.UserId).ToList();
            if (grants.Count == all.Count)
                return new OperationResult().NotFound("این کاربر در فهرست مدیران نیست.");

            var name = await uow.Users.AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => AdminPanelKit.Name(u.LastName, u.FirsName))
                .FirstOrDefaultAsync(ct) ?? "کاربر";

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            AdminPanelKit.Audit(uow, actor, "admin.revoke", "user", request.UserId, name);
            await settings.SaveAsync(AppSettingsStore.AdminGrantsKey, grants, actor.Name, ct);
            sessions.RevokeAll(request.UserId);
            return new OperationResult().succedded();
        }
    }

    public sealed record SignOutAdminCommand(Guid UserId) : ICommands;

    internal sealed class SignOutAdminCommandHandler(
        IAdminAccess admin, IApplicationUserManager users, IApplicationUnitOfWork uow, IAdminSessionService sessions)
        : ICommandHandler<SignOutAdminCommand>
    {
        public async Task<OperationResult> Handle(SignOutAdminCommand request, CancellationToken ct)
        {
            if (!AdminManagersKit.IsCurrentOwner(admin, users))
                return AdminPanelKit.Denied();

            var count = sessions.RevokeAll(request.UserId);
            var name = await uow.Users.AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => AdminPanelKit.Name(u.LastName, u.FirsName))
                .FirstOrDefaultAsync(ct) ?? "کاربر";

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            AdminPanelKit.Audit(uow, actor, "admin.signout", "user", request.UserId, name, $"{count} نشست بسته شد");
            await uow.SaveChangesAsync(ct);
            return new OperationResult().succedded($"{count} نشست بسته شد.");
        }
    }
}
