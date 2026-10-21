using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record DayOccasion(string Title, bool Holiday);

    public sealed record PersianDayInfo(
        string Weekday,
        string Persian,
        string Hijri,
        string Gregorian,
        bool Holiday,
        List<DayOccasion> Occasions,
        string? NextHoliday);

    /// <summary>
    /// تقویم امروز بدون اینترنت: شمسی، قمری، میلادی و مناسبت‌ها و تعطیلی‌های رسمی.
    /// تاریخ قمری با تقویم هجری دات‌نت حساب می‌شود و ممکن است با تقویم رسمی ایران یک روز
    /// فرق کند؛ با تنظیم Assistant:HijriAdjust (مثلاً -1 یا 1) روی سرور درست می‌شود.
    /// </summary>
    public sealed class PersianCalendarService(IConfiguration configuration)
    {
        private static readonly PersianCalendar Pc = new();
        private static readonly HijriCalendar Hc = new();

        private static readonly string[] PersianMonths =
            { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

        private static readonly string[] HijriMonths =
            { "محرم", "صفر", "ربیع‌الاول", "ربیع‌الثانی", "جمادی‌الاول", "جمادی‌الثانی", "رجب", "شعبان", "رمضان", "شوال", "ذی‌القعده", "ذی‌الحجه" };

        private static readonly string[] GregorianMonths =
            { "ژانویه", "فوریه", "مارس", "آوریل", "مه", "ژوئن", "ژوئیه", "اوت", "سپتامبر", "اکتبر", "نوامبر", "دسامبر" };

        private static readonly Dictionary<DayOfWeek, string> Weekdays = new()
        {
            [DayOfWeek.Saturday] = "شنبه", [DayOfWeek.Sunday] = "یکشنبه", [DayOfWeek.Monday] = "دوشنبه",
            [DayOfWeek.Tuesday] = "سه‌شنبه", [DayOfWeek.Wednesday] = "چهارشنبه", [DayOfWeek.Thursday] = "پنجشنبه",
            [DayOfWeek.Friday] = "جمعه",
        };

        // (ماه، روز) شمسی
        private static readonly Dictionary<(int, int), DayOccasion> SolarDays = new()
        {
            [(1, 1)] = new("عید نوروز", true), [(1, 2)] = new("عید نوروز", true), [(1, 3)] = new("عید نوروز", true),
            [(1, 4)] = new("عید نوروز", true), [(1, 12)] = new("روز جمهوری اسلامی", true), [(1, 13)] = new("روز طبیعت", true),
            [(3, 14)] = new("رحلت امام خمینی", true), [(3, 15)] = new("قیام ۱۵ خرداد", true),
            [(11, 22)] = new("پیروزی انقلاب اسلامی", true), [(12, 29)] = new("ملی شدن صنعت نفت", true),
            [(2, 12)] = new("روز معلم", false), [(5, 14)] = new("روز مشروطه", false), [(7, 8)] = new("روز بزرگداشت مولوی", false),
            [(7, 20)] = new("روز بزرگداشت حافظ", false), [(8, 13)] = new("روز دانش‌آموز", false), [(9, 16)] = new("روز دانشجو", false),
            [(9, 30)] = new("شب یلدا", false), [(12, 15)] = new("روز درختکاری", false),
        };

        // (ماه، روز) قمری
        private static readonly Dictionary<(int, int), DayOccasion> LunarDays = new()
        {
            [(1, 9)] = new("تاسوعای حسینی", true), [(1, 10)] = new("عاشورای حسینی", true),
            [(2, 20)] = new("اربعین حسینی", true), [(2, 28)] = new("رحلت پیامبر و شهادت امام حسن مجتبی", true),
            [(2, 30)] = new("شهادت امام رضا", true), [(3, 8)] = new("شهادت امام حسن عسکری", true),
            [(3, 17)] = new("میلاد پیامبر و امام صادق", true), [(6, 3)] = new("شهادت حضرت فاطمه", true),
            [(7, 13)] = new("ولادت امام علی، روز پدر", true), [(7, 27)] = new("مبعث پیامبر", true),
            [(8, 15)] = new("ولادت حضرت قائم", true), [(9, 21)] = new("شهادت امام علی", true),
            [(10, 1)] = new("عید فطر", true), [(10, 2)] = new("تعطیل به مناسبت عید فطر", true),
            [(10, 25)] = new("شهادت امام صادق", true), [(12, 10)] = new("عید قربان", true), [(12, 18)] = new("عید غدیر", true),
            [(5, 1)] = new("ولادت حضرت زینب، روز پرستار", false), [(6, 20)] = new("ولادت حضرت فاطمه، روز مادر", false),
            [(9, 19)] = new("شب قدر", false), [(9, 23)] = new("شب قدر", false),
        };

        private int HijriAdjust
            => int.TryParse(configuration["Assistant:HijriAdjust"], out var n) && n is >= -2 and <= 2 ? n : 0;

        public PersianDayInfo Get(DateOnly date)
        {
            var dt = date.ToDateTime(new TimeOnly(12, 0));
            var occasions = OccasionsOf(date);
            var holiday = dt.DayOfWeek == DayOfWeek.Friday || occasions.Any(o => o.Holiday);

            var hdt = dt.AddDays(HijriAdjust);
            var (hm, hd) = LunarOf(date);

            return new PersianDayInfo(
                Weekdays[dt.DayOfWeek],
                AssistantFormat.Persian($"{Pc.GetDayOfMonth(dt)} {PersianMonths[Pc.GetMonth(dt) - 1]} {Pc.GetYear(dt)}"),
                AssistantFormat.Persian($"{hd} {HijriMonths[hm - 1]} {Hc.GetYear(hdt)}"),
                AssistantFormat.Persian($"{dt.Day} {GregorianMonths[dt.Month - 1]} {dt.Year}"),
                holiday,
                occasions,
                NextHoliday(date));
        }

        public List<DayOccasion> OccasionsOf(DateOnly date)
        {
            var dt = date.ToDateTime(new TimeOnly(12, 0));
            var list = new List<DayOccasion>();
            if (SolarDays.TryGetValue((Pc.GetMonth(dt), Pc.GetDayOfMonth(dt)), out var solar))
                list.Add(solar);

            var (hm, hd) = LunarOf(date);
            if (LunarDays.TryGetValue((hm, hd), out var lunar))
                list.Add(lunar);
            // صفر گاهی ۲۹ روزه است؛ شهادت امام رضا آخرین روز صفر است.
            if (hm == 2 && hd == 29 && LunarOf(date.AddDays(1)).Month == 3)
                list.Add(LunarDays[(2, 30)]);
            return list;
        }

        private (int Month, int Day) LunarOf(DateOnly date)
        {
            var hdt = date.ToDateTime(new TimeOnly(12, 0)).AddDays(HijriAdjust);
            return (Hc.GetMonth(hdt), Hc.GetDayOfMonth(hdt));
        }

        private string? NextHoliday(DateOnly today)
        {
            for (var i = 1; i <= 90; i++)
            {
                var d = today.AddDays(i);
                var holiday = OccasionsOf(d).FirstOrDefault(o => o.Holiday);
                if (holiday is null) continue;
                var dt = d.ToDateTime(new TimeOnly(12, 0));
                var when = i == 1 ? "فردا" : AssistantFormat.Persian($"{i} روز دیگر");
                return $"{holiday.Title}، {Weekdays[dt.DayOfWeek]} {AssistantFormat.Persian($"{Pc.GetDayOfMonth(dt)} {PersianMonths[Pc.GetMonth(dt) - 1]}")} ({when})";
            }
            return null;
        }
    }
}
