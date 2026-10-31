using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CleanArchitecture.Infrastructure.Persistence.Encryption
{
    /// <summary>
    /// مبدل EF برای ستون‌های متنی رمزشده. null دست نمی‌خورد (EF مبدل را برای
    /// null صدا نمی‌زند). چون هر بار nonce تازه ساخته می‌شود، روی این ستون‌ها
    /// نمی‌شود در SQL جستجو یا مقایسه کرد؛ جستجو در حافظه انجام می‌شود.
    /// </summary>
    public sealed class EncryptedStringConverter : ValueConverter<string, string>
    {
        public static readonly EncryptedStringConverter Instance = new();

        public EncryptedStringConverter()
            : base(v => MessageCipher.Encrypt(v), v => MessageCipher.Decrypt(v))
        {
        }
    }
}
