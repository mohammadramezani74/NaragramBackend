using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Sms;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Authentication;

/// <summary>
/// وضعیت کدها و نشست‌های پنل در حافظه‌ی سرور (singleton). با ری‌استارت پاک می‌شود
/// و همه دوباره کد می‌خواهند؛ این برای پنل مدیریت عمداً سخت‌گیرانه است.
/// </summary>
internal sealed class AdminSessionStore
{
    public sealed class CodeState
    {
        public string Hash = string.Empty;
        public DateTime ExpiresAt;
        public int Attempts;
        public DateTime LastSentAt;
        public List<DateTime> Sends = new();
        public List<DateTime> Failures = new();
        public DateTime? LockedUntil;
    }

    public sealed record Session(Guid UserId, DateTime CreatedAt, string? Ip, string? UserAgent)
    {
        public DateTime LastSeen { get; set; } = CreatedAt;
    }

    public ConcurrentDictionary<Guid, CodeState> Codes { get; } = new();

    // کلید: هش توکن؛ خود توکن هیچ‌جا نگه داشته نمی‌شود
    public ConcurrentDictionary<string, Session> Sessions { get; } = new();
}

internal sealed class AdminSessionService(
    AdminSessionStore store,
    IApplicationUnitOfWork uow,
    ISmsService sms,
    IHttpContextAccessor http,
    IConfiguration configuration,
    ILogger<AdminSessionService> logger) : IAdminSessionService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxSendsPerHour = 5;
    private const int MaxAttemptsPerCode = 5;
    private const int MaxFailuresPerHour = 10;
    private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(30);

    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromHours(12);

    public async Task<AdminCodeResult> SendCodeAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var state = store.Codes.GetOrAdd(userId, _ => new AdminSessionStore.CodeState());

        string code;
        lock (state)
        {
            if (state.LockedUntil > now)
                return Wait(state.LockedUntil.Value - now, "به‌خاطر تلاش‌های ناموفق زیاد، ورود به پنل موقتاً بسته است.");

            var sinceLast = now - state.LastSentAt;
            if (sinceLast < ResendCooldown)
                return Wait(ResendCooldown - sinceLast, "کد قبلی تازه فرستاده شده.");

            state.Sends.RemoveAll(t => now - t > TimeSpan.FromHours(1));
            if (state.Sends.Count >= MaxSendsPerHour)
                return Wait(TimeSpan.FromHours(1) - (now - state.Sends.Min()), "در یک ساعت گذشته کد زیادی خواسته شده.");

            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            state.Hash = Hash(userId + ":" + code);
            state.ExpiresAt = now + CodeLifetime;
            state.Attempts = 0;
            state.LastSentAt = now;
            state.Sends.Add(now);
        }

        var phone = await uow.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PhoneNumber)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(phone))
        {
            lock (state) { state.Hash = string.Empty; }
            return new AdminCodeResult(false, "برای حساب شما شماره‌ی موبایل ثبت نشده؛ کد ورود پنل فرستاده نمی‌شود.", null, 0);
        }

        try
        {
            await SendSmsAsync(phone, code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin panel code SMS failed for {UserId}", userId);
            lock (state) { state.Hash = string.Empty; state.LastSentAt = DateTime.MinValue; }
            return new AdminCodeResult(false, "پیامک فرستاده نشد. چند لحظه بعد دوباره امتحان کنید.", null, 0);
        }

        return new AdminCodeResult(true, "کد ورود پیامک شد.", Mask(phone), (int)ResendCooldown.TotalSeconds);
    }

    public async Task<AdminVerifyResult> VerifyAsync(Guid userId, string code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        code = new string((code ?? string.Empty)
            .Select(c => c is >= '۰' and <= '۹' ? (char)('0' + (c - '۰')) : c is >= '٠' and <= '٩' ? (char)('0' + (c - '٠')) : c)
            .Where(char.IsDigit).ToArray());

        if (!store.Codes.TryGetValue(userId, out var state))
            return Fail("اول کد را بخواهید.");

        lock (state)
        {
            if (state.LockedUntil > now)
                return Fail("به‌خاطر تلاش‌های ناموفق زیاد، ورود به پنل موقتاً بسته است.");
            if (state.Hash.Length == 0 || state.ExpiresAt < now)
                return Fail("کد منقضی شده؛ کد تازه بخواهید.");

            var ok = code.Length == 6 && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(state.Hash), Encoding.ASCII.GetBytes(Hash(userId + ":" + code)));

            if (!ok)
            {
                state.Attempts++;
                state.Failures.RemoveAll(t => now - t > TimeSpan.FromHours(1));
                state.Failures.Add(now);
                if (state.Failures.Count >= MaxFailuresPerHour)
                {
                    state.LockedUntil = now + LockTime;
                    state.Hash = string.Empty;
                    logger.LogWarning("Admin panel locked for {UserId} after repeated wrong codes", userId);
                    return Fail("تلاش‌های ناموفق زیاد بود؛ ورود به پنل ۳۰ دقیقه بسته شد.");
                }
                if (state.Attempts >= MaxAttemptsPerCode)
                {
                    state.Hash = string.Empty;
                    return Fail("کد چند بار اشتباه وارد شد؛ کد تازه بخواهید.");
                }
                return Fail("کد درست نیست.");
            }

            state.Hash = string.Empty;
            state.Failures.Clear();
        }

        var ctx = http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress?.ToString();
        var agent = ctx?.Request.Headers.UserAgent.ToString();

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        store.Sessions[Hash(token)] = new AdminSessionStore.Session(userId, now, ip, agent);
        Prune(now);

        await AlertAsync(userId, ip, ct);
        return new AdminVerifyResult(true, "وارد شدید.", token, (int)IdleTimeout.TotalMinutes);
    }

    public bool Validate(string? token, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
            return false;

        var key = Hash(token);
        if (!store.Sessions.TryGetValue(key, out var s))
            return false;

        var now = DateTime.UtcNow;
        if (now - s.LastSeen > IdleTimeout || now - s.CreatedAt > AbsoluteTimeout)
        {
            store.Sessions.TryRemove(key, out _);
            return false;
        }

        if (s.UserId != userId)
            return false;

        s.LastSeen = now;
        return true;
    }

    public void Revoke(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            store.Sessions.TryRemove(Hash(token), out _);
    }

    public int RevokeAll(Guid userId)
    {
        var count = 0;
        foreach (var (key, s) in store.Sessions)
            if (s.UserId == userId && store.Sessions.TryRemove(key, out _))
                count++;
        return count;
    }

    public int ActiveCount(Guid userId)
    {
        var now = DateTime.UtcNow;
        return store.Sessions.Values.Count(s => s.UserId == userId
            && now - s.LastSeen <= IdleTimeout && now - s.CreatedAt <= AbsoluteTimeout);
    }

    // ---------------------------------------------------------------- کمکی

    private async Task SendSmsAsync(string phone, string code)
    {
        // اگر قالب sms.ir تعریف شده باشد از خط خدماتی و قالب استفاده می‌شود (مطمئن‌تر،
        // به لیست سیاه نمی‌خورد)؛ وگرنه پیامک عادی.
        if (int.TryParse(configuration["AdminAuth:SmsTemplateId"], out var templateId) && templateId > 0)
        {
            var param = configuration["AdminAuth:SmsTemplateParam"];
            await sms.SendTemplateAsync(phone, templateId, new Dictionary<string, string>
            {
                [string.IsNullOrWhiteSpace(param) ? "Code" : param] = code,
            });
            return;
        }

        await sms.SendMessageToUser(phone,
            $"کد ورود به پنل مدیریت ناراگرام: {code}\nاین کد را به هیچ‌کس ندهید.");
    }

    /// <summary>پیامک هشدار ورود به خود مدیر؛ بیرون از ناراگرام، تا اگر حساب دست کس دیگری است باز هم برسد.</summary>
    private async Task AlertAsync(Guid userId, string? ip, CancellationToken ct)
    {
        try
        {
            var phone = await uow.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.PhoneNumber)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(phone))
                return;

            var at = DateTime.Now.ToString("HH:mm");
            await sms.SendMessageToUser(phone,
                $"ورود به پنل مدیریت ناراگرام ساعت {at}" + (ip is null ? "" : $" از IP {ip}") +
                ".\nاگر کار شما نبود، فوراً به مدیر سامانه خبر دهید.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Admin login alert SMS failed for {UserId}", userId);
        }
    }

    private void Prune(DateTime now)
    {
        foreach (var (key, s) in store.Sessions)
            if (now - s.LastSeen > IdleTimeout || now - s.CreatedAt > AbsoluteTimeout)
                store.Sessions.TryRemove(key, out _);
    }

    private static AdminCodeResult Wait(TimeSpan wait, string message)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
        var text = seconds >= 120 ? $"{(seconds + 59) / 60} دقیقه" : $"{seconds} ثانیه";
        return new AdminCodeResult(false, $"{message} {text} دیگر دوباره امتحان کنید.", null, seconds);
    }

    private static AdminVerifyResult Fail(string message) => new(false, message, null, 0);

    private static string Mask(string phone)
    {
        phone = phone.Trim();
        return phone.Length < 8 ? new string('•', phone.Length) : phone[..4] + "•••" + phone[^3..];
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
