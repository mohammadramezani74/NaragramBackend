using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Flows
{
    /// <summary>
    /// یک «جریان»: گفتگوی چندمرحله‌ای ربات (سؤال، اعتبارسنجی، تأیید، صدا زدن API،
    /// نتیجه). جریان‌ها در کد نوشته می‌شوند و با این برچسب خودشان را معرفی می‌کنند؛
    /// برنامه موقع بالا آمدن پیدایشان می‌کند و در پنل ادمین قابل انتخاب می‌شوند.
    ///
    /// <code>
    /// [BotFlow("hr.advance", "درخواست مساعده")]
    /// [FlowParam("type", "نوع", Options = new[] { "عادی", "اضطراری" })]
    /// public sealed class AdvanceFlow(IHrConnector hr) : BotFlow { ... }
    /// </code>
    ///
    /// کلید (Key) بعد از استفاده در منو نباید عوض شود؛ منو با همین کلید به جریان وصل است.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BotFlowAttribute(string key, string title) : Attribute
    {
        public string Key { get; } = key;
        public string Title { get; } = title;
        public string? Description { get; set; }
    }

    /// <summary>
    /// تنظیمی که پنل ادمین برای این جریان می‌پرسد (مثلاً «نوع مساعده»).
    /// با Options فقط همان مقدارها مجازند و پنل منوی کشویی نشان می‌دهد.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class FlowParamAttribute(string key, string title) : Attribute
    {
        public string Key { get; } = key;
        public string Title { get; } = title;
        public string[]? Options { get; set; }
        public bool Required { get; set; } = true;
    }

    public interface IBotFlow
    {
        Task RunAsync(BotFlowContext context);
    }

    /// <summary>
    /// پایه‌ی جریان‌ها. قواعد مهم (به خاطر ادامه دادن از وسط کار):
    /// - هر کار بیرونی (API، دیتابیس، زمان، عدد تصادفی) را داخل context.CallAsync بنویسید
    ///   تا نتیجه‌اش ذخیره شود و با هر جواب تازه‌ی کاربر دوباره اجرا نشود.
    /// - ترتیب سؤال‌ها باید فقط به جواب‌های قبلی و نتیجه‌ی CallAsync بستگی داشته باشد.
    /// - وسط سؤال‌ها منبعی (فایل، اتصال) باز نگه ندارید؛ finally بعد از سؤال اجرا نمی‌شود.
    /// </summary>
    public abstract class BotFlow : IBotFlow
    {
        public abstract Task RunAsync(BotFlowContext context);
    }

    /// <summary>کاربری که با ربات حرف می‌زند؛ همیشه از سرور، هیچ‌وقت از ورودی کاربر.</summary>
    /// <param name="City">شهر پروفایل (Address.City)، اگر ثبت شده باشد.</param>
    public sealed record BotFlowUser(
        Guid Id, string FullName, string? NationalCode, string? PhoneNumber,
        string? City = null, string? FirstName = null);

    /// <param name="Value">مقداری که به جریان برمی‌گردد.</param>
    /// <param name="Label">متن دکمه.</param>
    /// <param name="Style">null یا primary.</param>
    public sealed record BotOption(string Value, string Label, string? Style = null);

    /// <summary>
    /// یک اتصال به سیستم بیرونی (INTS، کارتابل، قیمت ارز و ...). همه‌ی جزئیات API —
    /// آدرس، توکن، نام فیلدها، تبدیل واحدها — داخل کلاس اتصال می‌ماند تا اگر API
    /// عوض شد فقط همین کلاس عوض شود. پیاده‌سازی‌ها خودکار پیدا و در DI ثبت می‌شوند.
    /// </summary>
    public interface IBotConnector
    {
        string Key { get; }
        string Title { get; }
        string? Description { get; }

        /// <summary>برای دکمه‌ی «آزمایش اتصال» در پنل. نباید چیزی را تغییر دهد.</summary>
        Task<BotConnectorTestResult> TestAsync(CancellationToken cancellationToken);
    }

    public sealed record BotConnectorTestResult(bool Ok, string Message);

    public sealed record BotFlowParamDescriptor(string Key, string Title, bool Required, IReadOnlyList<string>? Options);

    public sealed record BotFlowDescriptor(
        string Key,
        string Title,
        string? Description,
        Type Type,
        IReadOnlyList<BotFlowParamDescriptor> Params);

    /// <summary>متن‌های کمکی فارسی برای جریان‌ها.</summary>
    public static class BotText
    {
        private const string PersianDigits = "۰۱۲۳۴۵۶۷۸۹";
        private const string ArabicDigits = "٠١٢٣٤٥٦٧٨٩";

        public static string ToPersianDigits(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] is >= '0' and <= '9')
                    chars[i] = PersianDigits[chars[i] - '0'];
            return new string(chars);
        }

        public static string ToLatinDigits(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var p = PersianDigits.IndexOf(chars[i]);
                if (p >= 0) { chars[i] = (char)('0' + p); continue; }
                var a = ArabicDigits.IndexOf(chars[i]);
                if (a >= 0) chars[i] = (char)('0' + a);
            }
            return new string(chars);
        }

        /// <summary>۱۲٬۵۰۰٬۰۰۰</summary>
        public static string Number(long value)
            => ToPersianDigits(value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", "٬"));

        /// <summary>۱۲٬۵۰۰٬۰۰۰ تومان</summary>
        public static string Money(long toman) => Number(toman) + " تومان";

        /// <summary>تاریخ شمسی مثل ۱۴۰۵/۰۷/۲۲</summary>
        public static string Date(DateOnly date)
        {
            var pc = new System.Globalization.PersianCalendar();
            var dt = date.ToDateTime(TimeOnly.MinValue);
            return ToPersianDigits($"{pc.GetYear(dt)}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00}");
        }
    }
}
