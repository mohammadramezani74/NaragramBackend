using System.Security.Cryptography;
using System.Text;

namespace CleanArchitecture.Infrastructure.Persistence.Encryption
{
    /// <summary>
    /// رمزنگاری متن پیام‌ها پیش از ذخیره در دیتابیس (AES-256-GCM).
    ///
    /// مقدار رمزشده این شکل را دارد:  enc:v1:base64(nonce[12] + tag[16] + متن)
    /// هر چیزی که این پیشوند را نداشته باشد متن ساده‌ی قدیمی است و دست‌نخورده
    /// برگردانده می‌شود؛ پس پیام‌های قبلی بدون مهاجرت کار می‌کنند و فقط پیام‌های
    /// جدید رمز می‌شوند.
    ///
    /// کلید از تنظیمات «MessageEncryption:Key» خوانده می‌شود (۳۲ بایت به صورت
    /// base64) و فقط باید روی سرور باشد، هیچ‌وقت در گیت. اگر کلید تنظیم نشده
    /// باشد رمزنگاری خاموش است و همه چیز مثل قبل ساده ذخیره می‌شود.
    ///
    /// اگر کلید گم شود پیام‌های رمزشده دیگر قابل بازیابی نیستند.
    ///
    /// برای برداشتن رمزنگاری: «MessageEncryption:EncryptNew» را false کن (پیام‌های
    /// جدید ساده ذخیره می‌شوند ولی قبلی‌ها هنوز با کلید خوانده می‌شوند) و
    /// «MessageEncryption:DecryptExisting» را true کن تا پیام‌های رمزشده در پس‌زمینه
    /// به متن ساده برگردند. وقتی در لاگ نوشت تمام شد، کلید را بردار.
    /// </summary>
    public static class MessageCipher
    {
        public const string Prefix = "enc:v1:";
        public const string Unreadable = "🔒 این پیام رمزنگاری شده و قابل نمایش نیست.";

        private const int NonceSize = 12;
        private const int TagSize = 16;

        private static byte[]? _key;
        private static bool _encryptNew = true;

        /// <summary>کلید هست (پیام‌های رمزشده خوانده می‌شوند).</summary>
        public static bool HasKey => _key is not null;

        /// <summary>پیام‌های جدید رمز می‌شوند.</summary>
        public static bool IsEnabled => _key is not null && _encryptNew;

        /// <summary>
        /// یک بار هنگام بالا آمدن برنامه صدا زده می‌شود. کلید خالی یعنی خاموش؛
        /// کلید خراب باعث خطا می‌شود تا برنامه بی‌صدا متن ساده ذخیره نکند.
        /// </summary>
        public static void Configure(string? base64Key, bool encryptNew = true)
        {
            _encryptNew = encryptNew;

            if (string.IsNullOrWhiteSpace(base64Key))
            {
                _key = null;
                return;
            }

            byte[] key;
            try { key = Convert.FromBase64String(base64Key.Trim()); }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "MessageEncryption:Key must be a base64 string of 32 bytes.");
            }

            if (key.Length != 32)
                throw new InvalidOperationException(
                    "MessageEncryption:Key must be exactly 32 bytes (base64).");

            _key = key;
        }

        public static string Encrypt(string plain)
        {
            var key = _key;
            if (key is null || !_encryptNew || plain.Length == 0)
                return plain;

            var data = Encoding.UTF8.GetBytes(plain);
            var packed = new byte[NonceSize + TagSize + data.Length];
            var nonce = packed.AsSpan(0, NonceSize);
            var tag = packed.AsSpan(NonceSize, TagSize);
            var cipher = packed.AsSpan(NonceSize + TagSize);

            RandomNumberGenerator.Fill(nonce);
            using (var aes = new AesGcm(key, TagSize))
                aes.Encrypt(nonce, data, cipher, tag);

            return Prefix + Convert.ToBase64String(packed);
        }

        public static string Decrypt(string stored)
        {
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
                return stored;

            // متنی که کاربر خودش با «enc:v1:» شروع کرده رمزشده نیست
            if (!TryUnpack(stored, out _))
                return stored;

            return TryDecrypt(stored, out var plain) ? plain : Unreadable;
        }

        /// <summary>
        /// فقط وقتی true است که مقدار واقعاً رمزشده بوده و با کلید فعلی باز شده.
        /// کار برگرداندن پیام‌ها به متن ساده از این استفاده می‌کند تا چیزی را که
        /// باز نشد هرگز با متن «قابل نمایش نیست» جایگزین نکند.
        /// </summary>
        public static bool TryDecrypt(string stored, out string plain)
        {
            plain = stored;
            var key = _key;
            if (key is null
                || !stored.StartsWith(Prefix, StringComparison.Ordinal)
                || !TryUnpack(stored, out var packed))
                return false;

            try
            {
                var bytes = new byte[packed.Length - NonceSize - TagSize];
                using (var aes = new AesGcm(key, TagSize))
                    aes.Decrypt(
                        packed.AsSpan(0, NonceSize),
                        packed.AsSpan(NonceSize + TagSize),
                        packed.AsSpan(NonceSize, TagSize),
                        bytes);
                plain = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static bool TryUnpack(string stored, out byte[] packed)
        {
            packed = [];
            var body = stored.AsSpan(Prefix.Length);
            if (body.Length < 40 || body.Length % 4 != 0)
                return false;

            var buffer = new byte[body.Length / 4 * 3];
            if (!Convert.TryFromBase64Chars(body, buffer, out var written)
                || written < NonceSize + TagSize)
                return false;

            packed = buffer.AsSpan(0, written).ToArray();
            return true;
        }
    }
}
