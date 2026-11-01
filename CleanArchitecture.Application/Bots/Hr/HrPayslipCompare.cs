using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>یک فیش با سال و ماه شمسی‌اش.</summary>
    public sealed record DatedPayslip(int Year, int Month, Payslip Slip)
    {
        private static readonly string[] Months =
            { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

        public string MonthName => Months[Month - 1];

        public string Label => $"{MonthName} {BotText.ToPersianDigits(Year.ToString(CultureInfo.InvariantCulture))}";
    }

    [BotFlow("hr.payslip.compare", "مقایسه فیش با ماه قبل",
        Description = "آخرین فیش حقوقی در کنار ماه قبلش: خالص دریافتی، قلم‌هایی که عوض شده و دلیل اصلی اختلاف.")]
    public sealed class HrPayslipCompareFlow(IIntsConnector ints) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var r = await c.CallAsync("payslips2", ct => HrApi.GetTwoPayslipsAsync(ints, c.User.PhoneNumber, ct));
            if (!r.Ok || r.Value is null)
            {
                await c.SayAsync("⚠️ " + r.Message, menuButton: true);
                return;
            }

            var slips = r.Value.Where(s => s.Slip.Lines.Count > 0).ToList();
            if (slips.Count == 0)
            {
                await c.SayAsync("فیش حقوقی ماه‌های اخیر شما هنوز صادر نشده است.", menuButton: true);
                return;
            }

            if (slips.Count == 1)
            {
                // فقط یک ماه هست؛ همان فیش را نشان بده
                await c.SayAsync($"🧾 فیش حقوقی {slips[0].Label}\nفیش ماه قبلش در سامانه نیست، پس مقایسه‌ای نمی‌شود.",
                    menuButton: true, card: HrPayslipView.Build(slips[0].Slip));
                return;
            }

            var (current, previous) = (slips[0], slips[1]);
            await c.SayAsync($"📊 فیش {current.Label} در مقایسه با {previous.MonthName}",
                menuButton: true, card: HrPayslipCompareView.Build(current, previous));
        }
    }

    /// <summary>
    /// کارت مقایسه‌ی دو فیش. همه‌ی مبلغ‌ها از INTS ریال‌اند و تومان نمایش داده می‌شوند.
    /// «اثر» هر قلم روی خالص: پرداخت بیشتر مثبت است، کسر بیشتر منفی.
    /// </summary>
    public static class HrPayslipCompareView
    {
        private sealed record Change(string Title, int Group, long Before, long After)
        {
            public long Delta => After - Before;
            public long Impact => Group == 2 ? -Delta : Delta;
            public bool IsNew => Before == 0 && After != 0;
            public bool IsGone => After == 0 && Before != 0;
        }

        public static List<BotCardRow> Build(DatedPayslip current, DatedPayslip previous)
        {
            var cur = current.Slip;
            var prev = previous.Slip;
            var pm = previous.MonthName;

            var netCur = Net(cur);
            var netPrev = Net(prev);
            var grossCur = Total(cur, 1, 1);
            var grossPrev = Total(prev, 1, 1);
            var dedCur = Total(cur, 2, 1);
            var dedPrev = Total(prev, 2, 1);

            var rows = new List<BotCardRow>
            {
                new($"خالص دریافتی {current.MonthName}", Toman(netCur), "hero"),
                new(DeltaSentence(netCur, netPrev, pm), "", "note"),
            };

            // خلاصه: هر عدد با اسم ماهش و نوار برای مقایسه‌ی چشمی
            AddPair(rows, "خالص دریافتی", netCur, netPrev, current.MonthName, pm);
            AddPair(rows, "جمع پرداخت", grossCur, grossPrev, current.MonthName, pm);
            AddPair(rows, "جمع کسور", dedCur, dedPrev, current.MonthName, pm);

            // قلم‌هایی که عوض شده‌اند، به ترتیب اثر روی خالص
            var changes = Changes(cur, prev)
                .Where(x => x.Delta != 0)
                .OrderByDescending(x => Math.Abs(x.Impact))
                .ToList();

            if (changes.Count > 0)
            {
                rows.Add(new("چه چیزی عوض شد", "", "section"));
                rows.Add(new("🟢 خالص را بیشتر کرده · 🔴 کمتر کرده", "", "note"));
                foreach (var x in changes.Take(8))
                {
                    var dot = x.Impact > 0 ? "🟢" : "🔴";
                    var kind = x.Group == 2 ? "کسر" : "پرداخت";
                    rows.Add(new($"{dot} {x.Title} ({kind})",
                        $"{current.MonthName} {Plain(x.After)} · {pm} {Plain(x.Before)}"));
                }
                if (changes.Count > 8)
                    rows.Add(new($"و {Fa(changes.Count - 8)} قلم کوچک‌تر دیگر", "", "note"));
            }

            // کارکرد
            var work = WorkRows(cur, prev, current.MonthName, pm);
            if (work.Count > 0)
            {
                rows.Add(new("کارکرد", "", "section"));
                rows.AddRange(work);
            }

            // دلیل اصلی
            var reasons = Reasons(changes, cur, prev, pm, netCur - netPrev);
            if (reasons.Count > 0)
            {
                rows.Add(new("چرا؟", "", "section"));
                rows.AddRange(reasons.Select(t => new BotCardRow(t, "", "note")));
            }

            return rows;
        }

        // ------------------------------------------------------------ تحلیل

        private static List<Change> Changes(Payslip cur, Payslip prev)
        {
            static Dictionary<string, (int Group, long Value)> Map(Payslip s)
            {
                var map = new Dictionary<string, (int Group, long Value)>();
                foreach (var l in s.Lines.Where(l => l.Sub == 0 && l.Group is 1 or 2))
                {
                    var key = Key(l.Title);
                    var v = Signed(l.Amount) ?? 0;
                    map[key] = map.TryGetValue(key, out var old) ? (l.Group, old.Value + v) : (l.Group, v);
                }
                return map;
            }

            var a = Map(cur);
            var b = Map(prev);
            var titles = cur.Lines.Concat(prev.Lines)
                .Where(l => l.Sub == 0 && l.Group is 1 or 2)
                .GroupBy(l => Key(l.Title))
                .Select(g => (Key: g.Key, Title: Fa(g.First().Title)));

            return titles.Select(t =>
            {
                a.TryGetValue(t.Key, out var x);
                b.TryGetValue(t.Key, out var y);
                var group = x.Group != 0 ? x.Group : y.Group;
                return new Change(t.Title, group, y.Value, x.Value);
            }).ToList();
        }

        private static List<string> Reasons(List<Change> changes, Payslip cur, Payslip prev, string pm, long netDelta)
        {
            var list = new List<string>();
            if (netDelta == 0 || changes.Count == 0) return list;

            // بزرگ‌ترین اثر هم‌جهت با تغییر خالص
            var main = changes.FirstOrDefault(x => Math.Sign(x.Impact) == Math.Sign(netDelta));
            if (main is not null)
            {
                var share = Math.Abs(main.Impact) * 100.0 / Math.Abs(netDelta);
                var text = main switch
                {
                    { Group: 2, IsGone: true } => $"«{main.Title}» که {pm} {Toman(main.Before)} کسر شده بود، این ماه نیست.",
                    { Group: 2, IsNew: true } => $"این ماه «{main.Title}» به مبلغ {Toman(main.After)} کسر شده که {pm} نبود.",
                    { Group: 2 } => $"«{main.Title}» {Toman(Math.Abs(main.Delta))} {(main.Delta > 0 ? "بیشتر" : "کمتر")} کسر شده.",
                    { IsNew: true } => $"«{main.Title}» این ماه {Toman(main.After)} پرداخت شده که {pm} نبود.",
                    { IsGone: true } => $"«{main.Title}» که {pm} {Toman(Math.Abs(main.Before))} بود، این ماه نیست.",
                    _ => $"«{main.Title}» {Toman(Math.Abs(main.Delta))} {(main.Delta > 0 ? "بیشتر" : "کمتر")} شده.",
                };
                list.Add("📌 " + text + (share >= 50 && share <= 100 ? $" {Pct(share)} اختلاف از همین است." : ""));
            }

            // حقوق پایه و تعداد روزهای ماه
            var daysCur = Days(cur);
            var daysPrev = Days(prev);
            var baseChange = changes.FirstOrDefault(x => x.Title.Contains("حقوق پایه"));
            if (daysCur is int dc && daysPrev is int dp && dc != dp && baseChange is not null && Math.Sign(baseChange.Delta) == Math.Sign(dc - dp))
                list.Add($"📅 حقوق پایه با کارکرد {Fa(dc)} روز حساب شده ({pm} {Fa(dp)} روز)؛ تفاوتش طبیعی است.");

            // اضافه‌کار
            var overtime = changes.Where(x => x.Group == 1 && x.Title.Contains("اضافه")).Sum(x => x.Delta);
            if (overtime != 0 && (main is null || !main.Title.Contains("اضافه")))
                list.Add($"⏱ اضافه‌کار {Toman(Math.Abs(overtime))} {(overtime > 0 ? "بیشتر" : "کمتر")} از {pm}.");

            return list;
        }

        private static List<BotCardRow> WorkRows(Payslip cur, Payslip prev, string currentName, string pm)
        {
            var rows = new List<BotCardRow>();
            var keys = cur.Lines.Concat(prev.Lines).Where(l => l.Group == 3)
                .Select(l => Key(l.Title)).Distinct().ToList();
            foreach (var key in keys)
            {
                var a = cur.Lines.FirstOrDefault(l => l.Group == 3 && Key(l.Title) == key);
                var b = prev.Lines.FirstOrDefault(l => l.Group == 3 && Key(l.Title) == key);
                var title = Fa((a ?? b)!.Title);
                var now = a is null ? "ندارد" : HrText.PayslipValue(a);
                var before = b is null ? "ندارد" : HrText.PayslipValue(b);
                rows.Add(new(title, now == before ? $"هر دو ماه {now}" : $"{currentName} {now} · {pm} {before}"));
            }
            return rows;
        }

        // ------------------------------------------------------------ کمکی

        private static long Net(Payslip s)
            => Total(s, 1, 2) is var n && n != 0 ? n : Total(s, 1, 1) - Total(s, 2, 1);

        private static long Total(Payslip s, int group, int sub)
        {
            var row = s.Lines.FirstOrDefault(l => l.Group == group && l.Sub == sub);
            if (row is not null && Signed(row.Amount) is long v) return v;
            // جمع نیامده: از اقلام بساز
            return sub == 2 ? 0 : s.Lines.Where(l => l.Group == group && l.Sub == 0).Sum(l => Signed(l.Amount) ?? 0);
        }

        private static int? Days(Payslip s)
        {
            var row = s.Lines.FirstOrDefault(l => l.Group == 3 && l.Title.Contains("کارکرد"));
            var digits = new string(BotText.ToLatinDigits(row?.Amount ?? "").TakeWhile(char.IsAsciiDigit).ToArray());
            return int.TryParse(digits, out var d) ? d : null;
        }

        /// <summary>مبلغ ریالی با علامت: «-2,549,906» ← -2549906.</summary>
        private static long? Signed(string? s)
        {
            var t = BotText.ToLatinDigits(s ?? "").Replace(",", "").Replace("٬", "").Trim();
            var negative = t.StartsWith('-');
            t = t.TrimStart('-', '+').Trim();
            if (t.Length == 0 || !t.All(char.IsAsciiDigit) || !long.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                return null;
            return negative ? -n : n;
        }

        /// <summary>کلید یکسان برای یک قلم در دو ماه: بدون فاصله و نیم‌فاصله.</summary>
        private static string Key(string title)
            => (HrText.Clean(title) ?? "").Replace(" ", "").Replace("‌", "");

        private static string DeltaSentence(long now, long before, string pm)
        {
            var d = now - before;
            if (d == 0) return $"دقیقاً برابر {pm}.";
            var arrow = d > 0 ? "▲" : "▼";
            var pct = before > 0 ? $" ({Pct(Math.Abs(d) * 100.0 / before)})" : "";
            return $"{arrow} {Toman(Math.Abs(d))} {(d > 0 ? "بیشتر" : "کمتر")} از {pm}{pct}";
        }

        /// <summary>
        /// یک سرفصل با تغییر، بعد دو ردیف نواری «مهر» و «شهریور» تا معلوم باشد هر عدد مال کدام ماه است.
        /// </summary>
        private static void AddPair(List<BotCardRow> rows, string title, long now, long before, string nowName, string beforeName)
        {
            var d = now - before;
            var change = d == 0 ? "بدون تغییر" : $"{(d > 0 ? "▲" : "▼")} {Num(Math.Abs(d))} {(d > 0 ? "بیشتر" : "کمتر")}";
            rows.Add(new($"{title} (تومان)", change, "section"));
            var max = Math.Max(Math.Abs(now), Math.Abs(before));
            rows.Add(new(nowName, Num(now), "bar", max > 0 ? Math.Round(Math.Abs(now) * 100.0 / max, 1) : 0));
            rows.Add(new(beforeName, Num(before), "bar", max > 0 ? Math.Round(Math.Abs(before) * 100.0 / max, 1) : 0));
        }

        /// <summary>مبلغ ریالی به تومان، صفر یعنی «نداشت».</summary>
        private static string Plain(long rial) => rial == 0 ? "نداشت" : (rial < 0 ? "−" : "") + Num(Math.Abs(rial));

        private static string Num(long rial) => BotText.Number(HrText.ToToman(rial));

        private static string Toman(long rial) => BotText.Money(HrText.ToToman(rial));

        private static string Pct(double p)
            => Fa(p.ToString(p < 10 ? "0.#" : "0", CultureInfo.InvariantCulture)).Replace(".", "٫") + "٪";

        private static string Fa(int n) => BotText.ToPersianDigits(n.ToString(CultureInfo.InvariantCulture));

        private static string Fa(string s) => BotText.ToPersianDigits(s);
    }
}
