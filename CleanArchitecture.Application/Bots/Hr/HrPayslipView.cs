using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>
    /// فیش حقوقی در یک کارت: خالص دریافتی بزرگ بالا، بعد پرداخت‌ها و کسور با نوار سهم
    /// هر قلم از جمع پرداخت، کارکرد، سایر اطلاعات و چند نکته‌ی تحلیلی.
    ///
    /// کدهای فیش INTS: ۱ پرداخت (۱٫۱ جمع، ۱٫۲ قابل پرداخت)، ۲ کسور (۲٫۱ جمع)، ۳ کارکرد، ۴ سایر.
    /// مبلغ‌ها از INTS ریال می‌آیند و همه به تومان نمایش داده می‌شوند.
    /// </summary>
    public static class HrPayslipView
    {
        public static List<BotCardRow> Build(Payslip slip)
        {
            var earnings = Items(slip, 1);
            var deductions = Items(slip, 2);
            var gross = Amount(slip, 1, 1) ?? earnings.Sum(e => e.Value);
            var deductTotal = Amount(slip, 2, 1) ?? deductions.Sum(d => d.Value);
            var net = Amount(slip, 1, 2) ?? gross - deductTotal;

            var rows = new List<BotCardRow>
            {
                new("خالص دریافتی", Toman(net), "hero"),
            };
            if (gross > 0)
                rows.Add(new($"{Pct(net, gross)} از جمع پرداخت به حسابتان می‌رسد", "", "note"));

            // پرداخت‌ها
            if (earnings.Count > 0)
            {
                rows.Add(new("پرداخت‌ها (تومان)", Num(gross), "section"));
                foreach (var e in earnings.OrderByDescending(e => e.Value))
                    rows.Add(Bar(e.Title, e.Value, gross));
            }

            // کسور
            if (deductions.Count > 0)
            {
                rows.Add(new("کسور (تومان)", Num(deductTotal), "section"));
                foreach (var d in deductions.OrderByDescending(d => d.Value))
                    rows.Add(Bar(d.Title, d.Value, gross));
            }

            // کارکرد
            var work = slip.Lines.Where(l => l.Group == 3).ToList();
            if (work.Count > 0)
            {
                rows.Add(new("کارکرد", "", "section"));
                foreach (var w in work)
                    rows.Add(new(Fa(w.Title), HrText.PayslipValue(w)));
                var overtimeMinutes = work.Where(w => w.Title.Contains("اضافه")).Sum(w => Minutes(w.Amount));
                if (work.Count(w => w.Title.Contains("اضافه")) > 1 && overtimeMinutes > 0)
                    rows.Add(new("جمع اضافه‌کار", Hours(overtimeMinutes), "total"));
            }

            // سایر اطلاعات؛ بانک و شماره‌ی حساب در یک ردیف
            var info = slip.Lines.Where(l => l.Group == 4).ToList();
            if (info.Count > 0)
            {
                rows.Add(new("سایر اطلاعات", "", "section"));
                var bank = info.FirstOrDefault(l => l.Title.Contains("بانک"));
                var account = info.FirstOrDefault(l => l.Title.Contains("حساب"));
                foreach (var l in info)
                {
                    if (l == bank || l == account) continue;
                    rows.Add(new(Fa(l.Title), HrText.PayslipValue(l)));
                }
                if (bank is not null || account is not null)
                {
                    var value = string.Join(" · ", new[] { bank?.Amount, account is null ? null : HrText.PayslipValue(account) }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    rows.Add(new("حساب واریز", value));
                }
            }

            // نکته‌ها
            var insights = Insights(slip, earnings, deductions, gross, deductTotal, work);
            if (insights.Count > 0)
            {
                rows.Add(new("نکته‌ها", "", "section"));
                rows.AddRange(insights.Select(i => new BotCardRow(i, "", "note")));
            }

            return rows;
        }

        private static List<string> Insights(
            Payslip slip, List<(string Title, long Value)> earnings, List<(string Title, long Value)> deductions,
            long gross, long deductTotal, List<PayslipLine> work)
        {
            var list = new List<string>();
            if (gross <= 0) return list;

            // از هر ۱۰۰ تومان چقدر کم می‌شود
            var tax = deductions.Where(d => d.Title.Contains("مالیات")).Sum(d => d.Value);
            var insurance = deductions.Where(d => d.Title.Contains("بیمه")).Sum(d => d.Value);
            var parts = new List<string>();
            if (insurance > 0) parts.Add($"بیمه {Pct(insurance, gross)}");
            if (tax > 0) parts.Add($"مالیات {Pct(tax, gross)}");
            list.Add($"📉 از هر ۱۰۰ تومان پرداخت، {Fa(Math.Round(deductTotal * 100.0 / gross).ToString(CultureInfo.InvariantCulture))} تومان کسر شده"
                     + (parts.Count > 0 ? $" ({string.Join("، ", parts)})." : "."));

            // بزرگ‌ترین قلم پرداخت
            var top = earnings.OrderByDescending(e => e.Value).FirstOrDefault();
            if (top.Value > 0 && earnings.Count > 1)
                list.Add($"💼 بیشترین سهم پرداخت: {top.Title} با {Pct(top.Value, gross)}.");

            // اضافه‌کار: ساعت و مبلغ
            var overtime = earnings.Where(e => e.Title.Contains("اضافه")).Sum(e => e.Value);
            var minutes = work.Where(w => w.Title.Contains("اضافه")).Sum(w => Minutes(w.Amount));
            if (overtime > 0)
            {
                var text = $"⏱ اضافه‌کار {MillionToman(overtime)} میلیون تومان ({Pct(overtime, gross)} پرداخت)";
                if (minutes > 0)
                {
                    text += $" برای {Hours(minutes)}";
                    var perHour = overtime / 10.0 / (minutes / 60.0);
                    text += $"؛ هر ساعت حدود {BotText.Number((long)Math.Round(perHour / 1000) * 1000)} تومان";
                }
                list.Add(text + ".");
            }

            // مانده‌ی وام
            var loan = slip.Lines.FirstOrDefault(l => l.Group == 4 && l.Title.Contains("وام"));
            if (loan is not null && ParseLong(loan.Amount) is long remaining && remaining > 0)
                list.Add($"🏦 مانده‌ی وام شما {Toman(remaining)} است.");

            return list;
        }

        // ------------------------------------------------------------ کمکی

        private static List<(string Title, long Value)> Items(Payslip slip, int group)
            => slip.Lines
                .Where(l => l.Group == group && l.Sub == 0)
                .Select(l => (Title: Fa(l.Title), Value: ParseLong(l.Amount) ?? 0))
                .Where(x => x.Value > 0)
                .ToList();

        private static long? Amount(Payslip slip, int group, int sub)
            => slip.Lines.FirstOrDefault(l => l.Group == group && l.Sub == sub) is { } l ? ParseLong(l.Amount) : null;

        private static BotCardRow Bar(string title, long value, long of)
            => new($"{title} · {Pct(value, of)}", Num(value), "bar", of > 0 ? Math.Round(Math.Min(100, value * 100.0 / of), 1) : 0);

        private static long? ParseLong(string? s)
        {
            var d = BotText.ToLatinDigits(s ?? "").Replace(",", "").Replace("٬", "").Trim();
            return d.Length > 0 && d.All(char.IsAsciiDigit) && long.TryParse(d, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : null;
        }

        /// <summary>«005:50 ساعت» ← ۳۵۰ دقیقه.</summary>
        private static int Minutes(string? s)
        {
            var t = BotText.ToLatinDigits(s ?? "").Split(' ')[0].Split(':');
            return t.Length == 2 && int.TryParse(t[0], out var h) && int.TryParse(t[1], out var m) ? h * 60 + m : 0;
        }

        private static string Hours(int minutes) => Fa($"{minutes / 60}:{minutes % 60:00}") + " ساعت";

        // همه‌ی مبلغ‌های INTS ریال‌اند و به کاربر تومان نشان داده می‌شوند
        private static string Num(long rial) => BotText.Number(HrText.ToToman(rial));

        private static string Toman(long rial) => BotText.Money(HrText.ToToman(rial));

        private static string MillionToman(long rial)
        {
            var m = rial / 10.0 / 1_000_000;
            return Fa(m.ToString(m >= 100 ? "0" : "0.#", CultureInfo.InvariantCulture)).Replace(".", "٫");
        }

        private static string Pct(long part, long whole)
        {
            if (whole <= 0) return "۰٪";
            var p = part * 100.0 / whole;
            return Fa(p.ToString(p < 10 ? "0.#" : "0", CultureInfo.InvariantCulture)).Replace(".", "٫") + "٪";
        }

        private static string Fa(string s) => BotText.ToPersianDigits(s);
    }
}
