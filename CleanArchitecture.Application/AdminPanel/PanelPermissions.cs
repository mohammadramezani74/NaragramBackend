namespace CleanArchitecture.Application.AdminPanel
{
    /// <summary>یک دسترسی پنل مدیریت، برای نمایش در صفحه‌ی «مدیران».</summary>
    public sealed record AdminPermissionInfo(string Key, string Title, string Description);

    /// <summary>
    /// دسترسی‌های پنل مدیریت. مالک‌ها (فهرست Admin:UserIds در تنظیمات سرور) همه را
    /// دارند و از داخل پنل قابل تغییر نیستند؛ بقیه فقط آنچه مالک برایشان تیک زده.
    /// </summary>
    public static class PanelPermissions
    {
        /// <summary>فقط مالک؛ در فهرست قابل‌اعطا نیست.</summary>
        public const string Owner = "owner";

        public const string Dashboard = "dashboard";
        public const string Users = "users";
        public const string Roles = "roles";
        public const string Rooms = "rooms";
        public const string Bots = "bots";
        public const string Broadcast = "broadcast";
        public const string Reports = "reports";
        public const string Audit = "audit";
        public const string Settings = "settings";

        public static readonly IReadOnlyList<AdminPermissionInfo> All =
        [
            new(Dashboard, "داشبورد", "آمار کلی برنامه"),
            new(Users, "کاربران", "دیدن، ویرایش، غیرفعال کردن کاربران و دادن نقش به آن‌ها"),
            new(Roles, "نقش‌ها", "ساختن، تغییر و حذف نقش‌ها و دسترسی‌های برنامه"),
            new(Rooms, "گروه‌ها و کانال‌ها", "دیدن، ویرایش و بایگانی گروه‌ها و کانال‌ها"),
            new(Bots, "ربات‌ها", "ساختن و تنظیم ربات‌ها و کلیدهای API"),
            new(Broadcast, "پیام همگانی", "فرستادن پیام به همه یا گروهی از کاربران"),
            new(Reports, "گزارش‌ها", "گزارش استفاده و کاربران غیرفعال"),
            new(Audit, "گزارش فعالیت مدیران", "دیدن کارهایی که مدیران در پنل انجام داده‌اند"),
            new(Settings, "تنظیمات", "اعلان بالای برنامه و اجازه‌ی ساخت گروه و کانال"),
        ];

        private static readonly HashSet<string> Known = All.Select(p => p.Key).ToHashSet();

        public static bool IsKnown(string key) => Known.Contains(key);

        /// <summary>همه‌ی دسترسی‌ها به‌علاوه‌ی owner، برای مالک.</summary>
        public static readonly IReadOnlySet<string> OwnerSet = All.Select(p => p.Key).Append(Owner).ToHashSet();
    }

    /// <summary>دسترسی‌های یک مدیر غیرمالک. در جدول AppSettings با کلید admin.grants ذخیره می‌شود.</summary>
    public sealed record AdminGrant(Guid UserId, List<string> Permissions, DateTime GrantedAt, string GrantedBy);

    /// <summary>
    /// آخرین فهرست مدیران در حافظه، برای پرسش‌های همگام (IsAdmin). هر بار که فهرست
    /// از دیتابیس خوانده یا ذخیره شود تازه می‌شود.
    /// </summary>
    public static class AdminGrantsSnapshot
    {
        private static volatile IReadOnlyDictionary<Guid, IReadOnlySet<string>>? _current;

        public static IReadOnlyDictionary<Guid, IReadOnlySet<string>>? Current => _current;

        public static void Set(IEnumerable<AdminGrant> grants)
            => _current = grants
                .GroupBy(g => g.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlySet<string>)g.SelectMany(x => x.Permissions ?? []).Where(PanelPermissions.IsKnown).ToHashSet());
    }
}
