using System.Threading.Channels;

namespace CleanArchitecture.Application.Bots
{
    public enum BotUpdateKind
    {
        /// <summary>کاربر «شروع» را زد.</summary>
        Start = 0,
        /// <summary>کاربر به ربات پیام متنی داد (یا دکمه‌ی کیبورد را زد).</summary>
        Text = 1,
        /// <summary>کاربر دکمه‌ی شیشه‌ای زیر یکی از پیام‌های ربات را زد.</summary>
        Callback = 2,
        /// <summary>کاربر فایل فرستاد.</summary>
        File = 3,
    }

    /// <summary>
    /// اتفاقی که موتور ربات باید به آن جواب بدهد. همه‌ی بررسی‌های دسترسی قبل از
    /// قرار گرفتن در صف انجام شده‌اند؛ موتور دوباره هم چک می‌کند.
    /// </summary>
    /// <param name="MessageId">پیام کاربر (Text/File/Start) یا پیام رباتی که دکمه‌اش زده شد (Callback).</param>
    /// <param name="Data">برای Callback: داده‌ی دکمه.</param>
    public sealed record BotUpdate(
        BotUpdateKind Kind,
        Guid BotUserId,
        Guid UserId,
        Guid ConversationId,
        Guid? MessageId,
        string? Text = null,
        string? Data = null);

    public interface IBotUpdateQueue
    {
        int PartitionCount { get; }
        void Enqueue(BotUpdate update);
        IAsyncEnumerable<BotUpdate> ReadAllAsync(int partition, CancellationToken cancellationToken);
    }

    /// <summary>
    /// صف در حافظه. هر کاربر همیشه به یک بخش می‌رود تا پیام‌هایش به ترتیب
    /// پردازش شوند؛ بخش‌های مختلف هم‌زمان کار می‌کنند تا جریان کندِ یک نفر
    /// بقیه را معطل نکند. با ری‌استارت سرور، کارهای صف از بین می‌روند؛ کاربر
    /// دوباره پیام می‌دهد و جریانش از دیتابیس ادامه پیدا می‌کند.
    /// </summary>
    public sealed class BotUpdateQueue : IBotUpdateQueue
    {
        private readonly Channel<BotUpdate>[] _partitions;

        public BotUpdateQueue(int partitions = 4)
        {
            _partitions = Enumerable.Range(0, Math.Max(1, partitions))
                .Select(_ => Channel.CreateBounded<BotUpdate>(new BoundedChannelOptions(1000)
                {
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.DropOldest,
                }))
                .ToArray();
        }

        public int PartitionCount => _partitions.Length;

        public void Enqueue(BotUpdate update)
        {
            var index = (int)((uint)update.UserId.GetHashCode() % (uint)_partitions.Length);
            _partitions[index].Writer.TryWrite(update);
        }

        public IAsyncEnumerable<BotUpdate> ReadAllAsync(int partition, CancellationToken cancellationToken)
            => _partitions[partition].Reader.ReadAllAsync(cancellationToken);
    }
}
