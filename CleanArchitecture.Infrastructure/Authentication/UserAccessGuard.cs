using System.Collections.Concurrent;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Authentication;

/// <summary>
/// سینگلتون. اولین بار از دیتابیس پر می‌شود (فقط کاربرهای غیرفعال یا خروج‌خورده) و بعد با
/// Mark* به‌روز می‌ماند. با ری‌استارت سرور دوباره از دیتابیس خوانده می‌شود، پس چیزی گم نمی‌شود.
/// </summary>
internal sealed class UserAccessGuard(IServiceScopeFactory scopes) : IUserAccessGuard
{
    private sealed record Entry(bool Disabled, DateTimeOffset? RevokedAt);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _loaded;

    public async Task<bool> IsAllowedAsync(Guid userId, DateTime issuedAtUtc, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        if (!_entries.TryGetValue(userId, out var entry))
            return true;

        if (entry.Disabled)
            return false;

        // iat دقت ثانیه دارد؛ توکنی که در همان ثانیه‌ی خروج ساخته شده هم رد می‌شود.
        return entry.RevokedAt is not { } revokedAt || issuedAtUtc > revokedAt.UtcDateTime;
    }

    public void MarkDisabled(Guid userId, DateTimeOffset at)
        => _entries[userId] = new Entry(true, at);

    public void MarkEnabled(Guid userId)
        => _entries.AddOrUpdate(userId, _ => new Entry(false, null), (_, e) => e with { Disabled = false });

    public void MarkSessionsRevoked(Guid userId, DateTimeOffset at)
        => _entries.AddOrUpdate(userId, _ => new Entry(false, at), (_, e) => e with { RevokedAt = at });

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded)
            return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_loaded)
                return;

            using var scope = scopes.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
            var rows = await uow.Users.AsNoTracking()
                .Where(u => u.IsDisabled || u.SessionsRevokedAt != null)
                .Select(u => new { u.Id, u.IsDisabled, u.SessionsRevokedAt })
                .ToListAsync(ct);

            foreach (var row in rows)
                _entries.TryAdd(row.Id, new Entry(row.IsDisabled, row.SessionsRevokedAt));

            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
