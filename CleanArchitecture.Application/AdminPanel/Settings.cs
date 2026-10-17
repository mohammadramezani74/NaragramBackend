using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Caching;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== مدل‌ها

    /// <summary>اعلان بالای برنامه. Tone: info | warn | success</summary>
    public sealed record AnnouncementSetting(bool Enabled, string Text, string Tone, string? LinkText, string? LinkUrl, DateTime? Until, DateTime? UpdatedAt = null);

    /// <summary>چه کسی گروه/کانال می‌سازد. Groups/Channels: all | roles (ادمین‌های پنل همیشه می‌توانند).</summary>
    public sealed record CreationSetting(string Groups, List<Guid> GroupRoleIds, string Channels, List<Guid> ChannelRoleIds);

    public sealed record AdminSettingsDto(AnnouncementSetting Announcement, CreationSetting Creation, List<string> AdminNames, DateTime? AnnouncementUpdatedAt, string? AnnouncementUpdatedBy);

    /// <summary>برای همه‌ی کاربران واردشده: اعلان فعال (اگر هست) و اجازه‌ی ساخت.</summary>
    public sealed record AppInfoDto(AnnouncementSetting? Announcement, bool CanCreateGroup, bool CanCreateChannel);

    // ================================================================== ذخیره

    public interface IAppSettingsStore
    {
        Task<AnnouncementSetting> AnnouncementAsync(CancellationToken ct = default);
        Task<CreationSetting> CreationAsync(CancellationToken ct = default);
        Task SaveAsync<T>(string key, T value, string byName, CancellationToken ct = default);
    }

    internal sealed class AppSettingsStore(IApplicationUnitOfWork uow, ICacheService cache) : IAppSettingsStore
    {
        public const string AnnouncementKey = "announcement";
        public const string CreationKey = "creation";

        private static readonly AnnouncementSetting DefaultAnnouncement = new(false, string.Empty, "info", null, null, null);
        private static readonly CreationSetting DefaultCreation = new("all", new List<Guid>(), "all", new List<Guid>());

        public async Task<AnnouncementSetting> AnnouncementAsync(CancellationToken ct = default)
        {
            var (value, updatedAt) = await ReadAsync(AnnouncementKey, DefaultAnnouncement, ct);
            return value with { UpdatedAt = updatedAt };
        }

        public async Task<CreationSetting> CreationAsync(CancellationToken ct = default)
            => (await ReadAsync(CreationKey, DefaultCreation, ct)).Value;

        public async Task SaveAsync<T>(string key, T value, string byName, CancellationToken ct = default)
        {
            var json = BotJson.Serialize(value);
            var row = await uow.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
            if (row is null) uow.AppSettings.Add(AppSetting.Create(key, json, byName));
            else row.Update(json, byName);
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                throw new InvalidOperationException("ذخیره‌ی تنظیمات انجام نشد: " + saved.Message);
            // کش همین سرور فوراً تازه می‌شود
            cache.Set<(T, DateTime?)>(CacheKey(key), (value, DateTime.Now), TimeSpan.FromSeconds(30));
        }

        // تنظیمات روی هر درخواست ساخت گروه و هر بار باز شدن برنامه خوانده می‌شود؛ ۳۰ ثانیه کش.
        private async Task<(T Value, DateTime? UpdatedAt)> ReadAsync<T>(string key, T fallback, CancellationToken ct)
        {
            if (cache.TryGetValue<(T, DateTime?)>(CacheKey(key), out var hit))
                return hit;

            var row = await uow.AppSettings.AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => new { s.ValueJson, s.UpdatedAt })
                .FirstOrDefaultAsync(ct);
            var value = (row is null ? default : BotJson.Deserialize<T>(row.ValueJson)) ?? fallback;
            (T, DateTime?) result = (value, row?.UpdatedAt);
            cache.Set<(T, DateTime?)>(CacheKey(key), result, TimeSpan.FromSeconds(30));
            return result;
        }

        private static string CacheKey(string key) => "app-setting:" + key;
    }

    // ================================================================== اجازه‌ی ساخت

    public interface ICreationPolicy
    {
        /// <summary>kind: group | channel</summary>
        Task<bool> CanCreateAsync(Guid userId, string kind, CancellationToken ct = default);
    }

    internal sealed class CreationPolicy(IAppSettingsStore settings, IAdminAccess admin, IApplicationRoleManager roles) : ICreationPolicy
    {
        public async Task<bool> CanCreateAsync(Guid userId, string kind, CancellationToken ct = default)
        {
            if (admin.IsAdmin(userId))
                return true;

            var rule = await settings.CreationAsync(ct);
            var (mode, allowed) = kind == "channel" ? (rule.Channels, rule.ChannelRoleIds) : (rule.Groups, rule.GroupRoleIds);
            if (mode != "roles")
                return true;
            if (allowed is null || allowed.Count == 0)
                return false;

            var mine = await roles.GetRolesByUserId(userId, ct);
            return mine.Any(r => allowed.Contains(r.RoleId));
        }
    }

    // ================================================================== پنل

    public sealed record GetAdminSettingsQuery : IQuery<AdminSettingsDto>;

    internal sealed class GetAdminSettingsQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IAppSettingsStore settings, IOptionsMonitor<AdminOptions> adminOptions)
        : IQueryHandler<GetAdminSettingsQuery, AdminSettingsDto>
    {
        public async Task<OperationResult<AdminSettingsDto>> Handle(GetAdminSettingsQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminSettingsDto>(AdminPanelKit.Denied());

            var ids = adminOptions.CurrentValue.UserIds.ToList();
            var names = await uow.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.LastName, u.FirsName })
                .ToListAsync(ct);
            var row = await uow.AppSettings.AsNoTracking()
                .Where(s => s.Key == AppSettingsStore.AnnouncementKey)
                .Select(s => new { s.UpdatedAt, s.UpdatedByName })
                .FirstOrDefaultAsync(ct);

            return OperationResult.Success(new AdminSettingsDto(
                await settings.AnnouncementAsync(ct),
                await settings.CreationAsync(ct),
                names.Select(n => AdminPanelKit.Name(n.LastName, n.FirsName)).ToList(),
                row?.UpdatedAt, row?.UpdatedByName));
        }
    }

    public sealed record SaveAnnouncementCommand(AnnouncementSetting Value) : ICommands;

    internal sealed class SaveAnnouncementCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, IAppSettingsStore settings)
        : ICommandHandler<SaveAnnouncementCommand>
    {
        public async Task<OperationResult> Handle(SaveAnnouncementCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var v = request.Value;
            var text = (v.Text ?? string.Empty).Trim();
            if (v.Enabled && text.Length is < 2 or > 300)
                return new OperationResult().BadRequest("متن اعلان باید ۲ تا ۳۰۰ حرف باشد.");
            if (v.Tone is not ("info" or "warn" or "success"))
                return new OperationResult().BadRequest("رنگ اعلان نامعتبر است.");
            var linkText = string.IsNullOrWhiteSpace(v.LinkText) ? null : v.LinkText.Trim();
            var linkUrl = string.IsNullOrWhiteSpace(v.LinkUrl) ? null : v.LinkUrl.Trim();
            if ((linkText is null) != (linkUrl is null) || (linkUrl is not null && !BroadcastKit.IsValidUrl(linkUrl)))
                return new OperationResult().BadRequest("برای لینک، متن و آدرسی که با https:// شروع شود لازم است.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            var value = new AnnouncementSetting(v.Enabled, text, v.Tone, linkText, linkUrl, v.Until);
            AdminPanelKit.Audit(uow, actor, "settings.announcement", "setting", null, "اعلان بالای برنامه",
                v.Enabled ? text : "خاموش شد");
            await settings.SaveAsync(AppSettingsStore.AnnouncementKey, value, actor.Name, ct);
            return new OperationResult().succedded();
        }
    }

    public sealed record SaveCreationCommand(CreationSetting Value) : ICommands;

    internal sealed class SaveCreationCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, IAppSettingsStore settings, IAdminIdentityService identity)
        : ICommandHandler<SaveCreationCommand>
    {
        public async Task<OperationResult> Handle(SaveCreationCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var v = request.Value;
            if (v.Groups is not ("all" or "roles") || v.Channels is not ("all" or "roles"))
                return new OperationResult().BadRequest("تنظیم نامعتبر است.");

            var roles = await identity.RolesAsync(ct);
            string Describe(string mode, List<Guid> ids) => mode == "all"
                ? "همه"
                : (ids.Count == 0 ? "فقط ادمین‌ها" : string.Join("، ", roles.Where(r => ids.Contains(r.Id)).Select(r => r.Name)));

            var value = new CreationSetting(v.Groups, v.GroupRoleIds ?? new List<Guid>(), v.Channels, v.ChannelRoleIds ?? new List<Guid>());
            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            AdminPanelKit.Audit(uow, actor, "settings.creation", "setting", null, "ساخت گروه و کانال",
                $"گروه: {Describe(value.Groups, value.GroupRoleIds)} · کانال: {Describe(value.Channels, value.ChannelRoleIds)}");
            await settings.SaveAsync(AppSettingsStore.CreationKey, value, actor.Name, ct);
            return new OperationResult().succedded();
        }
    }

    // ================================================================== برای برنامه

    public sealed record GetAppInfoQuery : IQuery<AppInfoDto>;

    internal sealed class GetAppInfoQueryHandler(IApplicationUserManager users, IAppSettingsStore settings, ICreationPolicy policy)
        : IQueryHandler<GetAppInfoQuery, AppInfoDto>
    {
        public async Task<OperationResult<AppInfoDto>> Handle(GetAppInfoQuery request, CancellationToken ct)
        {
            var me = users.UserId ?? Guid.Empty;
            var a = await settings.AnnouncementAsync(ct);
            var active = a.Enabled && !string.IsNullOrWhiteSpace(a.Text) && (a.Until is null || a.Until > DateTime.Now);
            return OperationResult.Success(new AppInfoDto(
                active ? a : null,
                await policy.CanCreateAsync(me, "group", ct),
                await policy.CanCreateAsync(me, "channel", ct)));
        }
    }
}
