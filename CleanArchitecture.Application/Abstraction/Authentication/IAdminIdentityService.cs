using CleanArchitecture.Application.Common.Models;

namespace CleanArchitecture.Application.Abstraction.Authentication
{
    public sealed record AdminRoleInfo(Guid Id, string Name, int Members);

    /// <summary>نقش‌ها برای پنل ادمین (جزئیات Identity در Infrastructure می‌ماند).</summary>
    public interface IAdminIdentityService
    {
        Task<List<AdminRoleInfo>> RolesAsync(CancellationToken ct = default);

        /// <summary>نام نقش‌های هر کاربر؛ کاربر بدون نقش در دیکشنری نیست.</summary>
        Task<Dictionary<Guid, List<AdminRoleInfo>>> RolesOfUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

        Task<List<Guid>> UserIdsInRoleAsync(Guid roleId, CancellationToken ct = default);

        /// <summary>نقش‌های کاربر را دقیقاً همین فهرست می‌کند. نام نقش‌های اضافه‌شده/برداشته‌شده را برمی‌گرداند.</summary>
        Task<OperationResult<(List<string> Added, List<string> Removed)>> SetUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct = default);

        Task<OperationResult<Guid>> CreateRoleAsync(string name, CancellationToken ct = default);

        Task<OperationResult> RenameRoleAsync(Guid roleId, string name, CancellationToken ct = default);

        /// <summary>فقط نقش بدون عضو حذف می‌شود.</summary>
        Task<OperationResult> DeleteRoleAsync(Guid roleId, CancellationToken ct = default);
    }
}
