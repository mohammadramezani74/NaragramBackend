using System.Globalization;
using System.Text;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Assistant
{
    /// <summary>متن‌های ربات دستیار؛ هم جریان‌ها و هم خلاصه‌ی صبحگاهی از این‌ها استفاده می‌کنند.</summary>
    public static class AssistantTexts
    {
        public const string Unavailable = "این بخش الان در دسترس نیست؛ کمی بعد دوباره امتحان کنید.";

        public static string Weather(WeatherReport r)
        {
            if (!r.Ok)
                return $"آب‌وهوای {r.City} الان در دسترس نیست (ممکن است اینترنت بین‌الملل قطع باشد). کمی بعد دوباره امتحان کنید.";

            var sb = new StringBuilder();
            sb.AppendLine($"{r.Emoji} هوای {r.City}: {AssistantFormat.Degrees(r.Temp)}، {r.Text}");
            sb.AppendLine($"رطوبت {AssistantFormat.Persian(r.Humidity.ToString(CultureInfo.InvariantCulture))}٪ · باد {AssistantFormat.Persian(r.WindKmh.ToString(CultureInfo.InvariantCulture))} کیلومتر بر ساعت");
            sb.AppendLine();
            foreach (var d in r.Days)
                sb.AppendLine($"{d.Emoji} {d.Label}: {d.Text}، {AssistantFormat.Degrees(d.Min)} تا {AssistantFormat.Degrees(d.Max)}");
            sb.AppendLine();
            sb.Append(r.Stale ? $"⚠️ به‌روز نیست (آخرین دریافت {r.UpdatedAt})." : $"به‌روز ساعت {r.UpdatedAt}.");
            sb.Append(" منبع: MET Norway");
            return sb.ToString();
        }

        public static string PricesHeader(PriceSnapshot p)
            => !p.Ok
                ? "قیمت‌ها الان در دسترس نیست؛ کمی بعد دوباره امتحان کنید."
                : p.Stale
                    ? $"💵 قیمت‌ها (⚠️ به‌روز نیست، آخرین دریافت ساعت {p.UpdatedAt})"
                    : $"💵 قیمت‌های بازار، ساعت {p.UpdatedAt}\nمنبع: {p.Sources}";

        public static List<BotCardRow> PriceRows(PriceSnapshot p)
            => p.Items.Select(i => new BotCardRow(i.Title, Price(i))).ToList();

        public static string Price(PriceItem i)
        {
            var text = BotText.Money(i.Toman);
            if (i.ChangePercent is double c && Math.Abs(c) >= 0.01)
                text += $"  {(c > 0 ? "▲" : "▼")}{AssistantFormat.Persian(Math.Abs(c).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', '٫'))}٪";
            return text;
        }

        public static List<BotCardRow> PrayerRows(PrayerTimesResult t) => new()
        {
            new("اذان صبح", t.Fajr), new("طلوع آفتاب", t.Sunrise), new("اذان ظهر", t.Dhuhr),
            new("غروب آفتاب", t.Sunset), new("اذان مغرب", t.Maghrib), new("نیمه‌شب شرعی", t.Midnight),
        };

        public static string Today(PersianDayInfo d)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 امروز {d.Weekday} {d.Persian}");
            sb.AppendLine($"{d.Hijri} · {d.Gregorian}");
            if (d.Occasions.Count > 0)
            {
                sb.AppendLine();
                foreach (var o in d.Occasions)
                    sb.AppendLine((o.Holiday ? "🎉 " : "• ") + o.Title + (o.Holiday ? " (تعطیل رسمی)" : string.Empty));
            }
            else if (d.Holiday)
            {
                sb.AppendLine("تعطیل");
            }
            if (d.NextHoliday is not null)
            {
                sb.AppendLine();
                sb.Append("تعطیلی بعدی: " + d.NextHoliday);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
