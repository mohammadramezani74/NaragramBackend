using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Persistence.Encryption
{
    /// <summary>
    /// برگرداندن یک‌باره‌ی همه‌ی متن‌های رمزشده به متن ساده، برای وقتی که بخواهیم
    /// رمزنگاری را برداریم. فقط وقتی اجرا می‌شود که در تنظیمات:
    ///   MessageEncryption:Key            = همان کلید
    ///   MessageEncryption:EncryptNew     = false
    ///   MessageEncryption:DecryptExisting = true
    ///
    /// در پس‌زمینه دسته‌دسته کار می‌کند، با SQL مستقیم (بدون مبدل EF) تا دقیقاً
    /// همان چیزی نوشته شود که باز شده. ردیفی که با کلید فعلی باز نشود دست نمی‌خورد
    /// و در لاگ شمرده می‌شود. اجرای دوباره بی‌خطر است؛ ردیف‌های ساده را رد می‌کند.
    /// وقتی لاگ «تمام شد» داد، کلید و این تنظیم را بردار.
    /// </summary>
    internal sealed class MessageDecryptService(
        IServiceScopeFactory scopes,
        ILogger<MessageDecryptService> logger) : BackgroundService
    {
        private const int BatchSize = 500;

        private static readonly (string Table, string Column)[] Columns =
        [
            ("Messages", "Content"),
            ("Messages", "ReplyMarkup"),
            ("Conversation", "LastMessageText"),
            ("Channel", "LastMessageText"),
            ("ContactTasks", "Text"),
            ("ContactTasks", "SourcePreview"),
        ];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!MessageCipher.HasKey || MessageCipher.IsEnabled)
            {
                logger.LogWarning(
                    "MessageEncryption: DecryptExisting needs the Key and EncryptNew=false. Skipped.");
                return;
            }

            try
            {
                // بگذار برنامه اول کامل بالا بیاید
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

                long total = 0, failed = 0;
                foreach (var (table, column) in Columns)
                {
                    var (done, bad) = await DecryptColumnAsync(table, column, stoppingToken);
                    total += done;
                    failed += bad;
                    logger.LogInformation(
                        "MessageEncryption: {Table}.{Column} decrypted {Done}, could not decrypt {Bad}",
                        table, column, done, bad);
                }

                logger.LogWarning(
                    "MessageEncryption: decrypt finished. {Total} values restored, {Failed} left encrypted. " +
                    "If Failed is 0 you can now remove the key and DecryptExisting.",
                    total, failed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // دفعه‌ی بعد که برنامه بالا بیاید از همان‌جا ادامه می‌دهد
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MessageEncryption: decrypt job failed; it will resume on next start.");
            }
        }

        private async Task<(long Done, long Bad)> DecryptColumnAsync(
            string table, string column, CancellationToken ct)
        {
            long done = 0, bad = 0;
            var last = Guid.Empty;

            // نام جدول و ستون از فهرست ثابت بالا می‌آید، نه از ورودی کاربر
            var select =
                $"SELECT TOP ({BatchSize}) [Id], [{column}] AS [Value] FROM [{table}] " +
                $"WHERE [{column}] LIKE N'{MessageCipher.Prefix}%' AND [Id] > {{0}} ORDER BY [Id]";

            while (!ct.IsCancellationRequested)
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var rows = await db.Database
                    .SqlQueryRaw<EncryptedRow>(select, last)
                    .ToListAsync(ct);
                if (rows.Count == 0) break;

                last = rows[^1].Id;

                var sql = new System.Text.StringBuilder();
                var args = new List<object>();
                foreach (var row in rows)
                {
                    if (!MessageCipher.TryDecrypt(row.Value, out var plain))
                    {
                        bad++;
                        continue;
                    }

                    sql.Append($"UPDATE [{table}] SET [{column}] = {{{args.Count}}} WHERE [Id] = {{{args.Count + 1}}};\n");
                    args.Add(plain);
                    args.Add(row.Id);
                    done++;
                }

                if (args.Count > 0)
                    await db.Database.ExecuteSqlRawAsync(sql.ToString(), args, ct);

                if (rows.Count < BatchSize) break;
            }

            return (done, bad);
        }

        private sealed class EncryptedRow
        {
            public Guid Id { get; set; }
            public string Value { get; set; } = string.Empty;
        }
    }
}
