using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// کلید API یک ربات برای ارسال پیام از سیستم‌های بیرونی (مثلاً منابع انسانی).
    /// خود کلید هیچ‌جا ذخیره نمی‌شود، فقط هش SHA-256 آن؛ کلید فقط یک بار، موقع ساخت،
    /// در پنل نشان داده می‌شود. Prefix (چند حرف اول) برای شناختن کلید در فهرست است.
    /// </summary>
    public sealed class BotApiKey : BaseEntity
    {
        public const int MaxNameLength = 100;
        public const int PrefixLength = 10;
        public const int HashLength = 64;

        private BotApiKey() { }

        public Guid BotId { get; private set; }

        /// <summary>برای چه سیستمی است، مثل «سامانه‌ی منابع انسانی».</summary>
        public string Name { get; private set; } = null!;

        public string Prefix { get; private set; } = null!;
        public string KeyHash { get; private set; } = null!;
        public DateTime? LastUsedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        public bool IsActive => RevokedAt is null;

        public static BotApiKey Create(Guid botId, string name, string prefix, string keyHash, Guid createdBy) => new()
        {
            BotId = botId,
            Name = name.Length > MaxNameLength ? name[..MaxNameLength] : name,
            Prefix = prefix,
            KeyHash = keyHash,
            CreatedByUserId = createdBy,
        };

        public void Revoke()
        {
            RevokedAt ??= DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>حداکثر دقیقه‌ای یک بار به‌روز می‌شود تا هر درخواست یک نوشتن اضافه نداشته باشد.</summary>
        public bool MarkUsed(DateTime now)
        {
            if (LastUsedAt is DateTime last && now - last < TimeSpan.FromMinutes(1))
                return false;
            LastUsedAt = now;
            return true;
        }
    }

    /// <summary>
    /// گزارش هر درخواست ارسال از بیرون (موفق یا ناموفق) و جلوگیری از ارسال تکراری.
    /// ExternalId شناسه‌ای است که سیستم بیرونی می‌فرستد؛ اگر همان را دوباره بفرستد
    /// (مثلاً بعد از قطع شبکه)، پیام دوباره فرستاده نمی‌شود. ردیف قبل از ارسال ثبت می‌شود
    /// و ایندکس یکتای (BotId, ExternalId) دو درخواست هم‌زمان را هم جلوگیری می‌کند.
    /// اگر ارسال ناموفق شد ExternalId خالی می‌شود تا تلاش دوباره ممکن باشد؛ Reference
    /// همیشه همان مقدار را برای نمایش نگه می‌دارد.
    /// </summary>
    public sealed class BotApiSend
    {
        public const int MaxExternalIdLength = 100;
        public const string StatusSending = "sending";
        public const string StatusSent = "sent";
        public const string StatusFailed = "failed";

        private BotApiSend() { }

        public Guid Id { get; private set; }
        public Guid BotId { get; private set; }
        public Guid ApiKeyId { get; private set; }
        public string? ExternalId { get; private set; }
        public string? Reference { get; private set; }

        /// <summary>شماره‌ی گیرنده، نیمه‌پنهان (۰۹۱۲•••۴۵۶۷).</summary>
        public string Phone { get; private set; } = string.Empty;

        public Guid? UserId { get; private set; }
        public Guid? MessageId { get; private set; }
        public string Status { get; private set; } = StatusSending;

        /// <summary>کد خطا، مثل user_not_found.</summary>
        public string? Error { get; private set; }

        public string? Preview { get; private set; }
        public string? ClientIp { get; private set; }
        public DateTime At { get; private set; }

        public static BotApiSend Create(Guid botId, Guid apiKeyId, string? externalId, string maskedPhone, string? preview, string? clientIp) => new()
        {
            Id = Guid.NewGuid(),
            BotId = botId,
            ApiKeyId = apiKeyId,
            ExternalId = externalId,
            Reference = externalId,
            Phone = maskedPhone.Length > 20 ? maskedPhone[..20] : maskedPhone,
            Preview = preview is { Length: > 100 } ? preview[..100] : preview,
            ClientIp = clientIp is { Length: > 64 } ? clientIp[..64] : clientIp,
            At = DateTime.Now,
        };
    }
}
