using System.Globalization;
using System.Text;

namespace CleanArchitecture.Application.Bots.Assistant
{
    /// <summary>ساعت ایران، مستقل از منطقه‌ی زمانی سرور.</summary>
    public static class IranTime
    {
        private static readonly TimeZoneInfo Zone = Find();

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        public static DateOnly Today => DateOnly.FromDateTime(Now);

        /// <summary>اختلاف ساعت ایران با UTC برای یک روز (برای محاسبه‌ی اوقات شرعی).</summary>
        public static double OffsetHours(DateOnly date)
            => Zone.GetUtcOffset(date.ToDateTime(new TimeOnly(12, 0))).TotalHours;

        public static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        private static TimeZoneInfo Find()
        {
            foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (Exception) { /* شناسه‌ی دیگر */ }
            }
            return TimeZoneInfo.CreateCustomTimeZone("Iran", TimeSpan.FromHours(3.5), "Iran", "Iran");
        }
    }

    public sealed record AssistantCity(string Name, double Lat, double Lng, int Elevation = 0);

    /// <summary>
    /// شهرهای شناخته‌شده با مختصات، برای آب‌وهوا و اوقات شرعی. شهر کاربر از پروفایلش
    /// (Address.City) خوانده می‌شود؛ اگر در این فهرست نبود، از خودش پرسیده می‌شود.
    /// </summary>
    public static class AssistantCities
    {
        public static readonly IReadOnlyList<AssistantCity> All = new List<AssistantCity>
        {
            new("تهران", 35.6892, 51.3890), new("کرج", 35.8400, 50.9391), new("مشهد", 36.2605, 59.6168),
            new("اصفهان", 32.6539, 51.6660), new("شیراز", 29.5918, 52.5837), new("تبریز", 38.0962, 46.2738),
            new("اهواز", 31.3183, 48.6706), new("قم", 34.6399, 50.8759), new("کرمانشاه", 34.3142, 47.0650),
            new("ارومیه", 37.5527, 45.0761), new("رشت", 37.2808, 49.5832), new("زاهدان", 29.4963, 60.8629),
            new("همدان", 34.7983, 48.5148), new("کرمان", 30.2839, 57.0834), new("یزد", 31.8974, 54.3569),
            new("اردبیل", 38.2498, 48.2933), new("بندرعباس", 27.1832, 56.2666), new("اراک", 34.0917, 49.6892),
            new("زنجان", 36.6736, 48.4787), new("سنندج", 35.3219, 46.9862), new("قزوین", 36.2797, 50.0049),
            new("خرم‌آباد", 33.4878, 48.3558), new("گرگان", 36.8456, 54.4393), new("ساری", 36.5633, 53.0601),
            new("بوشهر", 28.9234, 50.8203), new("بیرجند", 32.8663, 59.2211), new("ایلام", 33.6374, 46.4227),
            new("شهرکرد", 32.3256, 50.8644), new("یاسوج", 30.6682, 51.5880), new("سمنان", 35.5729, 53.3971),
            new("بجنورد", 37.4747, 57.3290), new("کیش", 26.5578, 54.0194), new("قشم", 26.9581, 56.2719),
            new("آبادان", 30.3392, 48.3043), new("کاشان", 33.9850, 51.4100), new("دزفول", 32.3830, 48.4010),
            new("نیشابور", 36.2133, 58.7958), new("سبزوار", 36.2126, 57.6819), new("بابل", 36.5513, 52.6790),
            new("آمل", 36.4696, 52.3507), new("قائمشهر", 36.4631, 52.8600), new("ورامین", 35.3242, 51.6457),
            new("اسلامشهر", 35.5522, 51.2350), new("شهریار", 35.6597, 51.0592), new("پاکدشت", 35.4669, 51.6861),
            new("دماوند", 35.7178, 52.0650), new("چابهار", 25.2919, 60.6430), new("بم", 29.1060, 58.3570),
            new("سیرجان", 29.4520, 55.6810), new("رفسنجان", 30.4067, 55.9939), new("ماهشهر", 30.5589, 49.1981),
            new("بندر انزلی", 37.4727, 49.4622), new("لاهیجان", 37.2072, 50.0039), new("مراغه", 37.3917, 46.2397),
            new("خوی", 38.5503, 44.9520), new("مهاباد", 36.7631, 45.7222), new("بروجرد", 33.8973, 48.7516),
            new("نجف‌آباد", 32.6342, 51.3680), new("شاهرود", 36.4182, 54.9763), new("گنبد کاووس", 37.2500, 55.1672),
            new("تربت حیدریه", 35.2740, 59.2195), new("عسلویه", 27.4761, 52.6074), new("ملایر", 34.2969, 48.8235),
            new("ساوه", 35.0213, 50.3566),
        };

        /// <summary>چند شهر بزرگ برای دکمه‌های پیشنهادی.</summary>
        public static readonly IReadOnlyList<string> Popular = new[] { "تهران", "کرج", "مشهد", "اصفهان", "شیراز", "تبریز", "اهواز", "قم" };

        private static readonly Dictionary<string, AssistantCity> ByKey =
            All.GroupBy(c => Key(c.Name)).ToDictionary(g => g.Key, g => g.First());

        public static AssistantCity? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var key = Key(name);
            if (ByKey.TryGetValue(key, out var city)) return city;
            foreach (var prefix in new[] { "شهر", "استان", "بندر" })
                if (key.StartsWith(prefix, StringComparison.Ordinal) && ByKey.TryGetValue(key[prefix.Length..], out city))
                    return city;
            return null;
        }

        /// <summary>«ي»→«ی»، «ك»→«ک»، بدون فاصله و نیم‌فاصله.</summary>
        private static string Key(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
            {
                var c = ch switch { 'ي' => 'ی', 'ى' => 'ی', 'ك' => 'ک', 'ة' => 'ه', 'أ' => 'ا', 'إ' => 'ا', 'آ' => 'ا', _ => ch };
                if (char.IsWhiteSpace(c) || c == '‌' || c == '-') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }
    }

    public static class AssistantFormat
    {
        public static string Persian(string? s) => Flows.BotText.ToPersianDigits(s);

        public static string Clock(double hours)
        {
            var total = (int)Math.Round(((hours % 24) + 24) % 24 * 60, MidpointRounding.AwayFromZero) % (24 * 60);
            return Persian($"{total / 60:00}:{total % 60:00}");
        }

        public static string Clock(DateTime t) => Persian(t.ToString("HH:mm", CultureInfo.InvariantCulture));

        public static string Degrees(double c) => Persian(((int)Math.Round(c)).ToString(CultureInfo.InvariantCulture)) + "°";
    }
}
