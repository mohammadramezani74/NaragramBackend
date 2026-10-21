namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record PrayerTimesResult(
        string City, string Imsak, string Fajr, string Sunrise, string Dhuhr, string Sunset, string Maghrib, string Midnight);

    /// <summary>
    /// اوقات شرعی بدون اینترنت، با روش مؤسسه‌ی ژئوفیزیک دانشگاه تهران (فجر ۱۷٫۷°، مغرب ۴٫۵°،
    /// نیمه‌شب شرعی بین غروب و فجر) بر پایه‌ی الگوریتم PrayTimes.org. ممکن است یکی‌دو دقیقه
    /// با تقویم رسمی فرق کند.
    /// </summary>
    public static class PrayerTimes
    {
        private const double FajrAngle = 17.7;
        private const double MaghribAngle = 4.5;
        private const double RiseSetAngle = 0.833;

        public static PrayerTimesResult Compute(AssistantCity city, DateOnly date)
        {
            var tz = IranTime.OffsetHours(date);
            var jDate = Julian(date.Year, date.Month, date.Day) - city.Lng / (15 * 24.0);

            double fajr = 5, sunrise = 6, dhuhr = 12, sunset = 18, maghrib = 18;
            for (var i = 0; i < 2; i++)
            {
                fajr = SunAngleTime(jDate, city.Lat, FajrAngle, fajr / 24, ccw: true);
                sunrise = SunAngleTime(jDate, city.Lat, RiseSetAngle, sunrise / 24, ccw: true);
                dhuhr = MidDay(jDate, dhuhr / 24);
                sunset = SunAngleTime(jDate, city.Lat, RiseSetAngle, sunset / 24, ccw: false);
                maghrib = SunAngleTime(jDate, city.Lat, MaghribAngle, maghrib / 24, ccw: false);
            }

            var shift = tz - city.Lng / 15;
            fajr += shift; sunrise += shift; dhuhr += shift; sunset += shift; maghrib += shift;
            var midnight = sunset + Fix(fajr - sunset, 24) / 2;
            var imsak = fajr - 10 / 60.0;

            return new PrayerTimesResult(
                city.Name,
                AssistantFormat.Clock(imsak), AssistantFormat.Clock(fajr), AssistantFormat.Clock(sunrise),
                AssistantFormat.Clock(dhuhr), AssistantFormat.Clock(sunset), AssistantFormat.Clock(maghrib),
                AssistantFormat.Clock(midnight));
        }

        private static double MidDay(double jDate, double t)
        {
            var (_, eqt) = SunPosition(jDate + t);
            return Fix(12 - eqt, 24);
        }

        private static double SunAngleTime(double jDate, double lat, double angle, double t, bool ccw)
        {
            var (decl, _) = SunPosition(jDate + t);
            var noon = MidDay(jDate, t);
            var cos = (-Sin(angle) - Sin(decl) * Sin(lat)) / (Cos(decl) * Cos(lat));
            var hours = Deg(Math.Acos(Math.Clamp(cos, -1, 1))) / 15;
            return noon + (ccw ? -hours : hours);
        }

        private static (double Declination, double Equation) SunPosition(double jd)
        {
            var d = jd - 2451545.0;
            var g = Fix(357.529 + 0.98560028 * d, 360);
            var q = Fix(280.459 + 0.98564736 * d, 360);
            var l = Fix(q + 1.915 * Sin(g) + 0.020 * Sin(2 * g), 360);
            var e = 23.439 - 0.00000036 * d;
            var ra = Fix(Deg(Math.Atan2(Cos(e) * Sin(l), Cos(l))) / 15, 24);
            return (Deg(Math.Asin(Sin(e) * Sin(l))), q / 15 - ra);
        }

        private static double Julian(int year, int month, int day)
        {
            if (month <= 2) { year -= 1; month += 12; }
            var a = Math.Floor(year / 100.0);
            var b = 2 - a + Math.Floor(a / 4);
            return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + b - 1524.5;
        }

        private static double Sin(double deg) => Math.Sin(deg * Math.PI / 180);
        private static double Cos(double deg) => Math.Cos(deg * Math.PI / 180);
        private static double Deg(double rad) => rad * 180 / Math.PI;

        private static double Fix(double a, double b)
        {
            a -= b * Math.Floor(a / b);
            return a < 0 ? a + b : a;
        }
    }
}
