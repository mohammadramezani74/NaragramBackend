using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Api
{
    // ================================================================== قرارداد بیرونی
    //
    // POST /api/v1/botapi/send      سربرگ X-Bot-Key: <کلید ربات>
    // {
    //   "phone": "09121234567",
    //   "text": "فیش حقوقی مهر آماده است.",
    //   "buttons": [[{ "text": "مشاهده در سامانه", "url": "https://hr.example/payslip" }]],
    //   "externalId": "payslip-1405-07-12345",          // اختیاری؛ جلوی ارسال تکراری را می‌گیرد
    //   "file": { "name": "fish.pdf", "base64": "..." }   // اختیاری
    // }
    //
    // دکمه‌ی «عمل» (action + data) برای ربات کارتابل رزرو شده و فعلاً رد می‌شود.

    public sealed class BotApiSendRequest
    {
        public string? Phone { get; set; }
        public string? Text { get; set; }
        public List<List<BotApiButton>>? Buttons { get; set; }
        public string? ExternalId { get; set; }
        public BotApiFile? File { get; set; }
    }

    public sealed class BotApiButton
    {
        public string? Text { get; set; }
        public string? Url { get; set; }

        /// <summary>رزرو برای دکمه‌های عمل (تأیید/رد کارتابل)؛ هنوز پشتیبانی نمی‌شود.</summary>
        public string? Action { get; set; }

        public string? Data { get; set; }
    }

    public sealed class BotApiFile
    {
        public string? Name { get; set; }
        public string? Base64 { get; set; }
    }

    public sealed record BotApiBotInfo(string Handle, string Name, bool IsEnabled, bool IsOfficial);

    public sealed class BotApiResponse
    {
        public bool Ok { get; init; }

        /// <summary>کد خطا برای برنامه‌ی بیرونی (انگلیسی و ثابت)، مثل user_not_found.</summary>
        public string? Error { get; init; }

        /// <summary>توضیح فارسی برای آدم.</summary>
        public string? Message { get; init; }

        public Guid? MessageId { get; init; }

        /// <summary>true یعنی این externalId قبلاً فرستاده شده بود و دوباره فرستاده نشد.</summary>
        public bool? Duplicate { get; init; }

        public BotApiBotInfo? Bot { get; init; }
    }

    public sealed record BotApiOutcome(int StatusCode, BotApiResponse Body);

    public interface IBotApiService
    {
        /// <summary>کلید درست است؟ کدام ربات؟ برای آزمایش اتصال از طرف سیستم بیرونی.</summary>
        Task<BotApiOutcome> MeAsync(string? key, CancellationToken ct);

        Task<BotApiOutcome> SendAsync(string? key, string? clientIp, BotApiSendRequest? request, CancellationToken ct);
    }

    // ================================================================== کمک‌ها

    public static class BotApiKeys
    {
        public const string HeaderName = "X-Bot-Key";
        private const string Lead = "nrk_";

        /// <summary>کلید تازه: ۳۲ بایت تصادفی. فقط هش آن ذخیره می‌شود.</summary>
        public static (string Plain, string Prefix, string Hash) Generate()
        {
            var body = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var plain = Lead + body;
            return (plain, plain[..BotApiKey.PrefixLength], Hash(plain));
        }

        public static string Hash(string plain)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain.Trim())));
    }

    /// <summary>سقف درخواست برای هر کلید (در حافظه‌ی همین سرور).</summary>
    public sealed class BotApiRateLimiter
    {
        public const int PerMinute = 600;

        private readonly ConcurrentDictionary<Guid, Window> _windows = new();

        private sealed class Window
        {
            public long Minute;
            public int Count;
        }

        public bool TryAcquire(Guid keyId)
        {
            var minute = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
            var window = _windows.GetOrAdd(keyId, _ => new Window());
            lock (window)
            {
                if (window.Minute != minute)
                {
                    window.Minute = minute;
                    window.Count = 0;
                }
                if (window.Count >= PerMinute)
                    return false;
                window.Count++;
                return true;
            }
        }
    }

    internal static class BotApiPhone
    {
        /// <summary>هر شکلی از شماره‌ی ایران (۰۹۱۲…، 912…، ‎+98912…، ارقام فارسی) → 09121234567؛ نامعتبر → null.</summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var digits = new string(AdminPanelKit.LatinDigits(raw).Where(char.IsAsciiDigit).ToArray());
            if (digits.StartsWith("0098", StringComparison.Ordinal))
                digits = digits[4..];
            else if (digits.StartsWith("98", StringComparison.Ordinal) && digits.Length == 12)
                digits = digits[2..];
            if (digits.StartsWith('0'))
                digits = digits[1..];
            return digits.Length == 10 && digits[0] == '9' ? "0" + digits : null;
        }

        /// <summary>شکل‌هایی که ممکن است شماره در جدول کاربرها ذخیره شده باشد.</summary>
        public static List<string> Variants(string normalized)
        {
            var core = normalized[1..];
            return new List<string> { normalized, core, "+98" + core, "98" + core, "0098" + core };
        }
    }

    public static class BotApiErrors
    {
        public static string Describe(string? code) => code switch
        {
            null or "" => string.Empty,
            "invalid_key" => "کلید API نامعتبر یا باطل‌شده",
            "rate_limited" => "درخواست بیش از حد مجاز",
            "invalid_request" => "درخواست نامعتبر",
            "invalid_phone" => "شماره‌ی موبایل نامعتبر",
            "empty_text" => "متن خالی",
            "text_too_long" => "متن خیلی بلند",
            "invalid_buttons" => "دکمه‌ها نامعتبر",
            "action_not_supported" => "دکمه‌ی عمل هنوز پشتیبانی نمی‌شود",
            "invalid_file" => "فایل نامعتبر",
            "file_too_large" => "فایل خیلی بزرگ",
            "bot_disabled" => "ربات خاموش است",
            "user_not_found" => "کاربری با این شماره نیست",
            "user_disabled" => "حساب کاربر غیرفعال است",
            "phone_ambiguous" => "شماره برای چند حساب ثبت شده",
            "not_allowed" => "کاربر به ربات دسترسی ندارد",
            "not_started" => "کاربر ربات را شروع نکرده",
            "in_progress" => "همین درخواست در حال ارسال است",
            "server_error" => "خطای سرور",
            _ => code,
        };
    }

    // ================================================================== ارسال

    internal sealed class BotApiService(
        IApplicationUnitOfWork uow,
        IApplicationRoleManager roles,
        BotMessenger messenger,
        BotApiRateLimiter limiter,
        ILogger<BotApiService> logger) : IBotApiService
    {
        private const int MaxTextLength = 4000;
        private const int MaxRows = 5;
        private const int MaxPerRow = 3;
        private const int MaxButtonText = 40;
        private const int MaxUrl = 500;
        private const int MaxFileBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> FileTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".docx", ".doc", ".txt", ".csv", ".zip",
        };

        public async Task<BotApiOutcome> MeAsync(string? key, CancellationToken ct)
        {
            var (bot, _, failure) = await AuthenticateAsync(key, ct);
            if (failure is not null)
                return failure;

            return new BotApiOutcome(200, new BotApiResponse { Ok = true, Bot = Info(bot!) });
        }

        public async Task<BotApiOutcome> SendAsync(string? key, string? clientIp, BotApiSendRequest? request, CancellationToken ct)
        {
            var (bot, apiKey, failure) = await AuthenticateAsync(key, ct);
            if (failure is not null)
                return failure;

            if (!limiter.TryAcquire(apiKey!.Id))
                return Fail(429, "rate_limited", $"بیش از {BotApiRateLimiter.PerMinute} درخواست در دقیقه؛ کمی بعد دوباره بفرستید.");

            request ??= new BotApiSendRequest();
            var externalId = string.IsNullOrWhiteSpace(request.ExternalId) ? null : request.ExternalId.Trim();
            if (externalId is { Length: > BotApiSend.MaxExternalIdLength })
                return Fail(400, "invalid_request", $"externalId حداکثر {BotApiSend.MaxExternalIdLength} نویسه است.");

            if (externalId is not null && await PreviousAsync(bot!.Id, externalId, ct) is { } earlier)
                return earlier;

            // ردیف گزارش قبل از ارسال ثبت می‌شود؛ ایندکس یکتای externalId دو درخواست هم‌زمان را جدا می‌کند.
            var phone = BotApiPhone.Normalize(request.Phone);
            var text = (request.Text ?? string.Empty).Trim();
            var preview = text.Length > 0 ? text.Split('\n')[0] : request.File?.Name;
            var log = BotApiSend.Create(bot!.Id, apiKey.Id, externalId, AdminPanelKit.MaskPhone(phone ?? request.Phone), preview, clientIp);
            uow.BotApiSends.Add(log);
            var reserved = await uow.SaveChangesAsync(ct);
            if (reserved.IsFailure)
            {
                uow.BotApiSends.Entry(log).State = EntityState.Detached;
                if (externalId is not null && await PreviousAsync(bot.Id, externalId, ct) is { } concurrent)
                    return concurrent;
                logger.LogError("Bot API log could not be saved: {Message}", reserved.Message);
                return Fail(500, "server_error", "خطای سرور؛ دوباره تلاش کنید.");
            }

            BotApiOutcome outcome;
            try
            {
                outcome = await DeliverAsync(bot, log.Id, request, phone, text, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await MarkFailedAsync(log.Id, "server_error");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot API send failed for bot {Bot}", bot.Handle);
                outcome = Fail(500, "server_error", "ارسال انجام نشد؛ دوباره تلاش کنید.");
            }

            if (!outcome.Body.Ok)
                await MarkFailedAsync(log.Id, outcome.Body.Error ?? "server_error");
            return outcome;
        }

        private async Task<BotApiOutcome> DeliverAsync(Bot bot, Guid logId, BotApiSendRequest request, string? phone, string text, CancellationToken ct)
        {
            if (!bot.IsEnabled)
                return Fail(403, "bot_disabled", "ربات خاموش است؛ در پنل ادمین روشنش کنید.");

            // ---------- محتوا
            var (markup, buttonError) = BuildMarkup(request.Buttons);
            if (buttonError is not null)
                return buttonError;

            byte[]? fileData = null;
            string? fileName = null;
            if (request.File is not null)
            {
                var (data, name, fileError) = ReadFile(request.File);
                if (fileError is not null)
                    return fileError;
                fileData = data;
                fileName = name;
            }

            if (fileData is null && text.Length == 0)
                return Fail(400, "empty_text", "متن پیام خالی است.");
            if (text.Length > MaxTextLength)
                return Fail(400, "text_too_long", $"متن پیام حداکثر {MaxTextLength} نویسه است.");

            // ---------- گیرنده
            if (phone is null)
                return Fail(400, "invalid_phone", "شماره‌ی موبایل معتبر نیست؛ مثلاً 09121234567.");

            var variants = BotApiPhone.Variants(phone);
            var matches = await uow.Users
                .Where(u => !u.IsBot && u.PhoneNumber != null && variants.Contains(u.PhoneNumber))
                .ToListAsync(ct);
            var active = matches.Where(u => !u.IsDisabled).ToList();
            if (matches.Count == 0)
                return Fail(404, "user_not_found", "کاربری با این شماره در ناراگرام نیست.");
            if (active.Count == 0)
                return Fail(403, "user_disabled", "حساب این کاربر غیرفعال است.");
            if (active.Count > 1)
                return Fail(409, "phone_ambiguous", "این شماره برای بیش از یک حساب ثبت شده؛ پیام فرستاده نشد.");
            var human = active[0];

            if (!await BotVisibility.CanUseAsync(bot, human.Id, isAdmin: false, roles, ct))
                return Fail(403, "not_allowed", "این کاربر به این ربات دسترسی ندارد (تنظیم «چه کسانی ربات را می‌بینند» در پنل).");

            var session = await uow.BotSessions.FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == human.Id, ct);
            if (!bot.IsOfficial && session?.IsStarted != true)
                return Fail(403, "not_started", "این کاربر هنوز ربات را «شروع» نکرده است. فقط ربات رسمی بدون شروع پیام می‌دهد.");

            // ---------- ارسال: همان مسیر پیام‌های خود ربات (لیست چت، شمارش نخوانده، هاب، Firebase)
            var conversation = await BotConversations.GetOrCreateAsync(uow, human, bot.User!, ct);
            var membership = conversation?.Users.FirstOrDefault(u => u.UserId == human.Id);
            if (conversation is null || membership is null)
                return Fail(500, "server_error", "گفتگوی ربات ساخته نشد؛ دوباره تلاش کنید.");

            var turn = new BotTurn(bot, bot.User!, human, conversation, membership,
                session ?? BotSession.Create(bot.Id, human.Id), new BotMenu(Array.Empty<BotMenuNode>()), ct);

            var message = fileData is null
                ? await messenger.SendAsync(turn, text, markup)
                : await messenger.SendDocumentAsync(turn, fileData, fileName!, text.Length == 0 ? null : text, markup);

            await uow.BotApiSends.Where(s => s.Id == logId).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, BotApiSend.StatusSent)
                .SetProperty(x => x.UserId, (Guid?)human.Id)
                .SetProperty(x => x.MessageId, (Guid?)message.Id), CancellationToken.None);

            return new BotApiOutcome(200, new BotApiResponse { Ok = true, MessageId = message.Id });
        }

        // ------------------------------------------------------------ کلید

        private async Task<(Bot? Bot, BotApiKey? Key, BotApiOutcome? Failure)> AuthenticateAsync(string? plain, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(plain) || plain.Length > 200)
                return (null, null, Fail(401, "invalid_key", $"کلید API فرستاده نشده؛ کلید در سربرگ {BotApiKeys.HeaderName} می‌آید."));

            var hash = BotApiKeys.Hash(plain);
            var key = await uow.BotApiKeys.FirstOrDefaultAsync(k => k.KeyHash == hash && k.RevokedAt == null, ct);
            var bot = key is null ? null : await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == key.BotId, ct);
            if (key is null || bot?.User is null)
                return (null, null, Fail(401, "invalid_key", "کلید API نامعتبر یا باطل‌شده است."));

            if (key.MarkUsed(DateTime.Now))
                await uow.SaveChangesAsync(ct); // فقط زمان آخرین استفاده؛ اگر نشد مهم نیست

            return (bot, key, null);
        }

        // ------------------------------------------------------------ تکراری

        private async Task<BotApiOutcome?> PreviousAsync(Guid botId, string externalId, CancellationToken ct)
        {
            var previous = await uow.BotApiSends.AsNoTracking()
                .Where(s => s.BotId == botId && s.ExternalId == externalId)
                .Select(s => new { s.Status, s.MessageId })
                .FirstOrDefaultAsync(ct);

            if (previous is null)
                return null;
            if (previous.Status == BotApiSend.StatusSent)
                return new BotApiOutcome(200, new BotApiResponse
                {
                    Ok = true,
                    Duplicate = true,
                    MessageId = previous.MessageId,
                    Message = "این externalId قبلاً فرستاده شده بود؛ دوباره فرستاده نشد.",
                });
            return Fail(409, "in_progress", "درخواستی با همین externalId در حال ارسال است.");
        }

        /// <summary>بیرون از Change Tracker، تا اگر ذخیره‌ی پیام نیمه‌کاره ماند، این یکی گیر نکند.</summary>
        private async Task MarkFailedAsync(Guid logId, string code)
        {
            try
            {
                await uow.BotApiSends.Where(s => s.Id == logId).ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, BotApiSend.StatusFailed)
                    .SetProperty(x => x.Error, code)
                    .SetProperty(x => x.ExternalId, (string?)null), CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot API log {Id} could not be marked failed", logId);
            }
        }

        // ------------------------------------------------------------ دکمه و فایل

        private static (BotReplyMarkup? Markup, BotApiOutcome? Error) BuildMarkup(List<List<BotApiButton>>? rows)
        {
            if (rows is null || rows.Count == 0)
                return (null, null);
            if (rows.Count > MaxRows || rows.Any(r => r is null || r.Count == 0 || r.Count > MaxPerRow))
                return (null, Fail(400, "invalid_buttons", $"حداکثر {MaxRows} ردیف دکمه و در هر ردیف ۱ تا {MaxPerRow} دکمه."));

            var markup = new BotReplyMarkup();
            foreach (var row in rows)
            {
                var list = new List<BotButton>();
                foreach (var button in row)
                {
                    if (!string.IsNullOrWhiteSpace(button?.Action))
                        return (null, Fail(400, "action_not_supported", "دکمه‌ی عمل (action) هنوز پشتیبانی نمی‌شود؛ فعلاً فقط دکمه‌ی لینک (url)."));

                    var label = (button?.Text ?? string.Empty).Trim();
                    var url = (button?.Url ?? string.Empty).Trim();
                    if (label.Length is 0 or > MaxButtonText)
                        return (null, Fail(400, "invalid_buttons", $"متن هر دکمه ۱ تا {MaxButtonText} نویسه است."));
                    if (url.Length > MaxUrl || !BroadcastKit.IsValidUrl(url))
                        return (null, Fail(400, "invalid_buttons", $"لینک دکمه‌ی «{label}» باید با http:// یا https:// شروع شود."));

                    list.Add(new BotButton(label, "link", null, url));
                }
                markup.Rows.Add(list);
            }
            return (markup, null);
        }

        private static (byte[]? Data, string? Name, BotApiOutcome? Error) ReadFile(BotApiFile file)
        {
            var name = Path.GetFileName((file.Name ?? string.Empty).Trim());
            if (name.Length is 0 or > 150 || !FileTypes.Contains(Path.GetExtension(name)))
                return (null, null, Fail(400, "invalid_file", "نام فایل لازم است و پسوندش یکی از این‌ها: " + string.Join(" ", FileTypes)));

            var base64 = (file.Base64 ?? string.Empty).Trim();
            var comma = base64.IndexOf(',');
            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                base64 = base64[(comma + 1)..];

            if (base64.Length == 0 || base64.Length > (MaxFileBytes / 3 + 1) * 4 + 8)
                return (null, null, Fail(400, "file_too_large", $"فایل خالی است یا از {MaxFileBytes / 1024 / 1024} مگابایت بزرگ‌تر است."));

            var buffer = new byte[base64.Length * 3 / 4 + 3];
            if (!Convert.TryFromBase64String(base64, buffer, out var length) || length == 0)
                return (null, null, Fail(400, "invalid_file", "محتوای فایل base64 معتبر نیست."));
            if (length > MaxFileBytes)
                return (null, null, Fail(400, "file_too_large", $"فایل از {MaxFileBytes / 1024 / 1024} مگابایت بزرگ‌تر است."));

            return (buffer.AsSpan(0, length).ToArray(), name, null);
        }

        private static BotApiBotInfo Info(Bot bot)
            => new(bot.Handle, (bot.User?.FirsName ?? bot.Handle).Trim(), bot.IsEnabled, bot.IsOfficial);

        private static BotApiOutcome Fail(int status, string code, string message)
            => new(status, new BotApiResponse { Ok = false, Error = code, Message = message });
    }
}
