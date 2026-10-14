using CleanArchitecture.Application.Common.unitOfWork;

namespace CleanArchitecture.Application.Bots
{
    /// <summary>
    /// ذخیره‌ای که نشد. SaveChangesAsync واحد کار خطای دیتابیس را می‌گیرد و فقط
    /// Result.Failure برمی‌گرداند؛ اگر کد ربات آن را نگاه نکند، ذخیره‌ی ناموفق بی‌صدا
    /// رد می‌شود. پس در کد ربات همیشه از SaveOrThrowAsync استفاده می‌شود.
    /// </summary>
    public sealed class BotSaveException(string message) : Exception(message);

    internal static class BotSave
    {
        public static async Task SaveOrThrowAsync(this IApplicationUnitOfWork uow, CancellationToken ct)
        {
            var result = await uow.SaveChangesAsync(ct);
            if (result.IsFailure)
                throw new BotSaveException("ذخیره در دیتابیس انجام نشد: " + result.Message);
        }
    }
}
