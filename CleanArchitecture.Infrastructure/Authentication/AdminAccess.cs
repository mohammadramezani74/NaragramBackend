using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Authentication;

internal sealed class AdminAccess(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<AdminOptions> options,
    IServiceScopeFactory scopes)
    : IAdminAccess
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>();

    public bool IsOwner(Guid? userId)
        => userId is Guid id
           && id != Guid.Empty
           && options.CurrentValue.UserIds.Contains(id);

    public bool IsAdmin(Guid? userId)
        => IsOwner(userId) || (userId is Guid id && Grants().ContainsKey(id));

    public IReadOnlySet<string> PermissionsOf(Guid? userId)
    {
        if (IsOwner(userId))
            return PanelPermissions.OwnerSet;
        return userId is Guid id && Grants().TryGetValue(id, out var perms) ? perms : None;
    }

    public bool Has(Guid? userId, string permission)
        => PermissionsOf(userId).Contains(permission);

    public bool CurrentUserIsAdmin
        => IsAdmin(httpContextAccessor.HttpContext?.User.GetUserId());

    /// <summary>
    /// فهرست مدیران در حافظه. فیلتر پنل قبل از هر درخواست آن را تازه می‌کند؛ اینجا
    /// فقط اولین بار بعد از بالا آمدن سرور، اگر هنوز خوانده نشده، یک بار می‌خوانیم.
    /// </summary>
    private IReadOnlyDictionary<Guid, IReadOnlySet<string>> Grants()
    {
        var current = AdminGrantsSnapshot.Current;
        if (current is not null)
            return current;

        try
        {
            using var scope = scopes.CreateScope();
            scope.ServiceProvider.GetRequiredService<IAppSettingsStore>()
                .AdminGrantsAsync().GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // دیتابیس در دسترس نیست: فقط مالک‌ها
            return new Dictionary<Guid, IReadOnlySet<string>>();
        }

        return AdminGrantsSnapshot.Current ?? new Dictionary<Guid, IReadOnlySet<string>>();
    }
}
