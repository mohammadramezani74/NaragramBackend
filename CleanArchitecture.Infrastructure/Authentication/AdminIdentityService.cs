using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Authentication;

internal sealed class AdminIdentityService(
    ApplicationDbContext db,
    RoleManager<Role> roleManager,
    UserManager<User> userManager) : IAdminIdentityService
{
    public async Task<List<AdminRoleInfo>> RolesAsync(CancellationToken ct = default)
    {
        var counts = await db.UserRoles.AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var roles = await db.Roles.AsNoTracking()
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);

        return roles
            .Select(r => new AdminRoleInfo(r.Id, r.Name ?? string.Empty, counts.FirstOrDefault(c => c.RoleId == r.Id)?.Count ?? 0))
            .OrderBy(r => r.Name)
            .ToList();
    }

    public async Task<Dictionary<Guid, List<AdminRoleInfo>>> RolesOfUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, List<AdminRoleInfo>>();

        var ids = userIds.ToList();
        var rows = await (
                from ur in db.UserRoles.AsNoTracking()
                join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ids.Contains(ur.UserId)
                select new { ur.UserId, r.Id, r.Name })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => new AdminRoleInfo(x.Id, x.Name ?? string.Empty, 0)).OrderBy(x => x.Name).ToList());
    }

    public Task<List<Guid>> UserIdsInRoleAsync(Guid roleId, CancellationToken ct = default)
        => db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync(ct);

    public async Task<OperationResult<(List<string> Added, List<string> Removed)>> SetUserRolesAsync(
        Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return OperationResult.Failure<(List<string>, List<string>)>(new OperationResult().NotFound("کاربر یافت نشد."));

        var wanted = await db.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync(ct);
        if (wanted.Count != roleIds.Distinct().Count())
            return OperationResult.Failure<(List<string>, List<string>)>(new OperationResult().BadRequest("یکی از نقش‌ها پیدا نشد."));

        var current = (await userManager.GetRolesAsync(user)).ToList();
        var add = wanted.Except(current).ToList();
        var remove = current.Except(wanted).ToList();

        if (remove.Count > 0)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, remove);
            if (!removed.Succeeded)
                return OperationResult.Failure<(List<string>, List<string>)>(new OperationResult().Failed(Errors(removed)));
        }

        if (add.Count > 0)
        {
            var added = await userManager.AddToRolesAsync(user, add);
            if (!added.Succeeded)
                return OperationResult.Failure<(List<string>, List<string>)>(new OperationResult().Failed(Errors(added)));
        }

        return OperationResult.Success((add, remove));
    }

    public async Task<OperationResult<Guid>> CreateRoleAsync(string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (name.Length is < 2 or > 64)
            return OperationResult.Failure<Guid>(new OperationResult().BadRequest("نام نقش باید ۲ تا ۶۴ حرف باشد."));

        if (await roleManager.RoleExistsAsync(name))
            return OperationResult.Failure<Guid>(new OperationResult().BadRequest("نقشی با این نام هست."));

        var role = new Role { Id = Guid.NewGuid(), Name = name };
        var result = await roleManager.CreateAsync(role);
        return result.Succeeded
            ? OperationResult.Success(role.Id)
            : OperationResult.Failure<Guid>(new OperationResult().Failed(Errors(result)));
    }

    public async Task<OperationResult> RenameRoleAsync(Guid roleId, string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (name.Length is < 2 or > 64)
            return new OperationResult().BadRequest("نام نقش باید ۲ تا ۶۴ حرف باشد.");

        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return new OperationResult().NotFound("نقش یافت نشد.");

        if (!string.Equals(role.Name, name, StringComparison.Ordinal) && await roleManager.RoleExistsAsync(name))
            return new OperationResult().BadRequest("نقشی با این نام هست.");

        role.Name = name;
        var result = await roleManager.UpdateAsync(role);
        return result.Succeeded ? new OperationResult().succedded() : new OperationResult().Failed(Errors(result));
    }

    public async Task<OperationResult> DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return new OperationResult().NotFound("نقش یافت نشد.");

        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == roleId, ct))
            return new OperationResult().BadRequest("این نقش عضو دارد؛ اول نقش را از اعضایش بگیرید.");

        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded ? new OperationResult().succedded() : new OperationResult().Failed(Errors(result));
    }

    private static string Errors(IdentityResult result)
        => string.Join(" ", result.Errors.Select(e => e.Description));
}
