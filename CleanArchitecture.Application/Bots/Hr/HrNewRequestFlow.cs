using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>
    /// ثبت درخواست پرسنلی در INTS، با سؤال‌های همان نوع درخواست.
    ///
    /// «هوشمند» یعنی قبل از اینکه کاربر وقت بگذارد، چیزی را که INTS بعداً رد می‌کند همین اول
    /// بگوید (درخواست باز هم‌نوع، سقف مساعده‌ی ماه و سال)، از سابقه‌ی خود کاربر پیشنهاد بدهد
    /// (ارگان‌های قبلی) و از آخرین فیش سقف معقول مساعده را حساب کند. تصمیم نهایی همیشه با INTS
    /// است؛ اگر رد کرد، پیام خودش به کاربر نشان داده می‌شود.
    ///
    /// ادمین می‌تواند در منو نوع را ثابت کند (مثلاً دکمه‌ی «درخواست مساعده»)؛ وگرنه ربات می‌پرسد.
    /// </summary>
    [BotFlow("hr.request.new", "ثبت درخواست پرسنلی",
        Description = "مساعده، گواهی اشتغال، کسر از حقوق، فیش رسمی و حکم کارگزینی؛ ثبت مستقیم در کارتابل منابع انسانی INTS.")]
    [FlowParam("type", "نوع درخواست", Options = new[] { AnyType, "مساعده", "گواهی اشتغال به کار", "کسر از حقوق", "فیش حقوقی رسمی", "حکم کارگزینی" }, Required = false)]
    public sealed class HrNewRequestFlow(IIntsConnector ints) : BotFlow
    {
        public const string AnyType = "کاربر انتخاب کند";
        private const string Skip = "رد شدن";

        private sealed record Kind(int Id, string Title, string Icon, string Hint, string HistoryName);

        // عددها همان PersonnelRequestsTypes در INTS است
        private static readonly Kind[] Kinds =
        {
            new(3, "مساعده", "💵", "بخشی از حقوق، زودتر از موعد", "مساعده"),
            new(2, "گواهی اشتغال به کار", "📄", "برای بانک، سفارت، اجاره‌نامه و …", "اشتغال به کار"),
            new(1, "کسر از حقوق", "🤝", "نامه‌ی ضمانت برای وام یا خرید اقساطی", "کسر از حقوق"),
            new(4, "فیش حقوقی رسمی", "📑", "نسخه‌ی مهرشده برای ارائه به ارگان", "فیش حقوقی"),
            new(5, "حکم کارگزینی", "📜", "نسخه‌ی رسمی آخرین حکم", "حکم کارگزینی"),
        };

        private static readonly string[] Months =
            { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

        public override async Task RunAsync(BotFlowContext c)
        {
            // سابقه برای هوشمندی است؛ اگر نیامد، بدون آن ادامه می‌دهیم و INTS خودش چک می‌کند
            var historyResult = await c.CallAsync("history", ct => HrApi.GetRequestsAsync(ints, c.User.PhoneNumber, ct));
            if (!historyResult.Ok && historyResult.Message is { } fatal && IsAccessProblem(fatal))
            {
                await c.SayAsync("⚠️ " + fatal, menuButton: true);
                return;
            }
            var history = historyResult.Value ?? new List<HrRequest>();
            var now = await c.CallAsync("today", _ => Task.FromResult(PersianNow()));
            var (year, month) = (now.Year, now.Month);

            // ---------------------------------------------------------------- نوع
            var fixedType = c.Param("type", AnyType);
            var kind = Kinds.FirstOrDefault(k => k.Title == fixedType);
            if (kind is null)
            {
                var picked = await c.ChooseAsync(
                    "📝 چه درخواستی دارید؟\n\n" + string.Join("\n", Kinds.Select(k => $"{k.Icon} {k.Title}: {k.Hint}")),
                    Kinds.Select(k => new BotOption(k.Id.ToString(CultureInfo.InvariantCulture), $"{k.Icon} {k.Title}")));
                kind = Kinds.First(k => k.Id.ToString(CultureInfo.InvariantCulture) == picked);
            }

            // درخواست باز هم‌نوع: INTS تا تعیین تکلیف آن، تازه‌اش را قبول نمی‌کند
            var open = history.FirstOrDefault(h => h.Type == kind.HistoryName && IsOpen(h));
            if (open is not null)
            {
                var text = $"⏳ یک درخواست {kind.Title} در جریان دارید (شماره‌ی {Fa(open.DocNo)}"
                           + (open.Date is null ? "" : $"، {Fa(open.Date)}") + ").\n"
                           + "تا وقتی تکلیف آن روشن نشده، درخواست تازه‌ی همین نوع ثبت نمی‌شود.";
                if (open.CanDelete) text += "\n\nاگر دیگر لازمش ندارید، از «درخواست‌های من» حذفش کنید و دوباره بیایید.";
                await c.SayAsync(text, menuButton: true, card: new[]
                {
                    new BotCardRow("وضعیت", open.Status ?? (open.Events.Count > 0 ? "در جریان بررسی" : "ثبت شده")),
                });
                return;
            }

            // ---------------------------------------------------------------- سؤال‌ها
            var summary = new List<BotCardRow> { new("نوع درخواست", $"{kind.Icon} {kind.Title}") };
            var warnings = new List<string>();
            HrNewRequest request;

            switch (kind.Id)
            {
                case 3: // مساعده
                {
                    var thisYear = history.Where(h => h.Type == kind.HistoryName && !IsRejected(h) && InYear(h, year)).ToList();
                    var thisMonth = thisYear.Where(h => InMonth(h, year, month)).ToList();
                    if (thisYear.Count >= 3)
                    {
                        await c.SayAsync($"امسال {Fa("3")} مساعده گرفته‌اید که سقف سالانه است؛ مساعده‌ی تازه از سال بعد ممکن است.", menuButton: true);
                        return;
                    }

                    // ماه مساعده؛ اگر این ماه گرفته، فقط ماه بعد
                    var (nextY, nextM) = month == 12 ? (year + 1, 1) : (year, month + 1);
                    int targetY = year, targetM = month;
                    if (thisMonth.Count > 0)
                    {
                        var ok = await c.ConfirmAsync(
                            $"این ماه ({Months[month - 1]}) یک مساعده گرفته‌اید و هر ماه فقط یکی ممکن است.\nبرای {Months[nextM - 1]} ثبت کنم؟",
                            yes: $"بله، برای {Months[nextM - 1]}", no: "نه");
                        if (!ok) { await c.SayAsync("باشد، ثبت نشد.", menuButton: true); return; }
                        (targetY, targetM) = (nextY, nextM);
                    }
                    else
                    {
                        var which = await c.ChooseAsync("مساعده برای کدام ماه؟", new[]
                        {
                            new BotOption("this", $"{Months[month - 1]} (همین ماه)", "primary"),
                            new BotOption("next", Months[nextM - 1]),
                        });
                        if (which == "next") (targetY, targetM) = (nextY, nextM);
                    }

                    // سقف پیشنهادی از آخرین فیش: INTS بیشتر از ۶۰٪ را با یادداشت به منابع انسانی می‌فرستد
                    var slip = await c.CallAsync("payslip", ct => HrApi.GetPayslipAsync(ints, c.User.PhoneNumber, ct));
                    var netToman = NetToman(slip);
                    long? soft = netToman is long n ? RoundDown(n * 6 / 10, 500_000) : null;

                    var question = "💵 چه مبلغی لازم دارید؟ (تومان)";
                    if (soft is long s && s > 0)
                        question += $"\n\n💡 تا {BotText.Money(s)} (۶۰٪ خالص آخرین فیش‌تان) معمولاً بدون بررسی اضافه تأیید می‌شود.";
                    var suggestions = new List<long> { 5_000_000, 10_000_000 };
                    if (soft is long s2 && s2 > 0) { suggestions = suggestions.Where(x => x < s2).ToList(); suggestions.Add(s2); }

                    var amount = await c.AskMoneyAsync(question, min: 500_000, max: netToman ?? 2_000_000_000, suggestions: suggestions);
                    if (soft is long s3 && amount > s3)
                        warnings.Add("مبلغ از ۶۰٪ حقوق بیشتر است؛ ثبت می‌شود ولی منابع انسانی جداگانه بررسی می‌کند.");

                    summary.Add(new("مبلغ", BotText.Money(amount)));
                    summary.Add(new("برای ماه", $"{Months[targetM - 1]} {Fa(targetY.ToString(CultureInfo.InvariantCulture))}"));
                    summary.Add(new("مساعده‌ی امسال", $"{Ordinal(thisYear.Count + 1)} از {Fa("3")}"));
                    request = new HrNewRequest(kind.Id, PriceRial: amount * 10d, Year: targetY, Month: targetM);
                    break;
                }

                case 2: // گواهی اشتغال
                {
                    var org = await AskOrganizationAsync(c, history, "📄 گواهی را برای کجا می‌خواهید؟",
                        new[] { "بانک ملی ایران", "سفارت", "مشاور املاک" });
                    var checkbook = await c.ConfirmAsync("دسته‌چک دارید؟ (در متن گواهی می‌آید)", yes: "دارم", no: "ندارم");
                    summary.Add(new("برای", org));
                    summary.Add(new("دسته‌چک", checkbook ? "دارد" : "ندارد"));

                    string? branch = null, branchCode = null;
                    if (org.Contains("بانک"))
                    {
                        branch = await AskOptionalAsync(c, $"نام شعبه‌ی {org}؟ (اگر نمی‌دانید «{Skip}» را بزنید)");
                        if (branch is not null)
                        {
                            branchCode = await AskOptionalAsync(c, "کد شعبه؟", digitsOnly: true);
                            summary.Add(new("شعبه", branch + (branchCode is null ? "" : $" (کد {Fa(branchCode)})")));
                        }
                    }
                    request = new HrNewRequest(kind.Id, RecipientOrganization: org, HasCheckbook: checkbook, BranchName: branch, BranchCode: branchCode);
                    break;
                }

                case 1: // کسر از حقوق
                {
                    var beneficiary = await c.AskTextAsync(
                        "🤝 ذی‌نفع (کسی که وام یا اقساط به نام اوست) کیست؟",
                        maxLength: 100,
                        suggestions: new[] { c.User.FullName },
                        placeholder: "نام و نام خانوادگی…");
                    var org = await AskOrganizationAsync(c, history, "نامه برای کدام بانک یا مؤسسه است؟",
                        new[] { "بانک ملی ایران", "بانک ملت", "بانک رفاه" });
                    var branch = await AskOptionalAsync(c, $"نام شعبه؟ (اگر لازم نیست «{Skip}» را بزنید)");
                    string? code = null, phone = null;
                    if (branch is not null)
                    {
                        code = await AskOptionalAsync(c, "کد شعبه؟", digitsOnly: true);
                        phone = await AskOptionalAsync(c, "تلفن شعبه؟", digitsOnly: true);
                    }
                    var amount = await c.AskMoneyAsync("مبلغ وام یا اقساط چقدر است؟ (تومان)", min: 100_000, max: 50_000_000_000,
                        suggestions: new long[] { 50_000_000, 100_000_000, 300_000_000 });

                    summary.Add(new("ذی‌نفع", beneficiary));
                    summary.Add(new("برای", org));
                    if (branch is not null)
                        summary.Add(new("شعبه", branch + (code is null ? "" : $" (کد {Fa(code)})") + (phone is null ? "" : $"، تلفن {Fa(phone)}")));
                    summary.Add(new("مبلغ", BotText.Money(amount)));
                    request = new HrNewRequest(kind.Id, RecipientOrganization: org, Beneficiary: beneficiary,
                        BranchName: branch, BranchCode: code, BranchPhone: phone, PriceRial: amount * 10d);
                    break;
                }

                default: // فیش رسمی، حکم
                {
                    var org = await AskOrganizationAsync(c, history, $"{kind.Icon} {kind.Title} را برای ارائه به کجا می‌خواهید؟",
                        new[] { "بانک", "دادگاه", "سفارت" });
                    summary.Add(new("برای", org));
                    request = new HrNewRequest(kind.Id, RecipientOrganization: org);
                    break;
                }
            }

            // ---------------------------------------------------------------- تأیید و ثبت
            var card = summary.ToList();
            foreach (var w in warnings) card.Add(new BotCardRow("⚠️ " + w, "", "note"));

            var confirmed = await c.ConfirmAsync("همه چیز درست است؟", card: card, yes: "✅ ثبت درخواست", no: "انصراف");
            if (!confirmed)
            {
                await c.SayAsync("درخواست ثبت نشد.", menuButton: true);
                return;
            }

            var result = await c.CallAsync("submit", ct => HrApi.SubmitRequestAsync(ints, c.User.PhoneNumber, request, ct));
            if (!result.Ok)
            {
                await c.SayAsync("⚠️ " + result.Message, menuButton: true);
                return;
            }

            // INTS شماره‌ی سند را برنمی‌گرداند؛ از فهرست تازه پیدایش می‌کنیم
            var after = await c.CallAsync("after", ct => HrApi.GetRequestsAsync(ints, c.User.PhoneNumber, ct));
            var created = after.Value?.FirstOrDefault(h => h.Type == kind.HistoryName && history.All(old => old.Id != h.Id));

            var done = $"✅ درخواست {kind.Title} ثبت شد و به کارتابل منابع انسانی رفت.";
            if (created is not null) done += $"\n\nشماره‌ی پیگیری: {Fa(created.DocNo)}";
            done += "\nنتیجه را با پیامک خبر می‌دهند؛ وضعیتش را هم در «درخواست‌های من» می‌بینید.";
            await c.SayAsync(done, menuButton: true);
        }

        // ---------------------------------------------------------------- کمکی

        /// <summary>ارگان، با پیشنهاد ارگان‌هایی که خود کاربر قبلاً نوشته.</summary>
        private static Task<string> AskOrganizationAsync(BotFlowContext c, List<HrRequest> history, string question, string[] defaults)
        {
            var mine = history
                .Select(h => h.Organization)
                .Where(o => !string.IsNullOrWhiteSpace(o) && o!.Length <= 40)
                .Distinct()
                .Take(3)
                .Cast<string>();
            var suggestions = mine.Concat(defaults).Distinct().Take(5).ToList();
            return c.AskTextAsync(question, maxLength: 150, suggestions: suggestions, placeholder: "نام ارگان…");
        }

        /// <summary>سؤال اختیاری؛ «رد شدن» ← null.</summary>
        private static async Task<string?> AskOptionalAsync(BotFlowContext c, string question, bool digitsOnly = false)
        {
            var answer = await c.AskTextAsync(
                question,
                maxLength: 100,
                validate: digitsOnly
                    ? s => s == Skip || BotText.ToLatinDigits(s).All(ch => char.IsAsciiDigit(ch) || ch is '-' or ' ')
                        ? null
                        : "فقط عدد بنویسید، یا «" + Skip + "» را بزنید."
                    : null,
                suggestions: new[] { Skip });
            if (answer == Skip) return null;
            return digitsOnly ? BotText.ToLatinDigits(answer).Trim() : answer.Trim();
        }

        private static bool IsAccessProblem(string message)
            => message.Contains("ثبت نشده") || message.Contains("در دسترس نیست") || message.Contains("برقرار نشد")
               || message.Contains("جواب نداد") || message.Contains("راه‌اندازی") || message.Contains("معتبری");

        private static bool IsRejected(HrRequest h)
        {
            var s = h.Status ?? "";
            return s.Contains("عدم") || s.Contains("رد شد");
        }

        private static bool IsOpen(HrRequest h)
            => !IsRejected(h) && !(h.Status ?? "").Contains("تایید درخواست");

        private static bool InYear(HrRequest h, int year)
            => BotText.ToLatinDigits(h.Date ?? "").StartsWith(year.ToString(CultureInfo.InvariantCulture) + "/");

        private static bool InMonth(HrRequest h, int year, int month)
            => BotText.ToLatinDigits(h.Date ?? "").StartsWith($"{year}/{month:00}");

        // رکورد، نه tuple: نتیجه‌ی CallAsync به JSON ذخیره می‌شود و فیلدهای tuple ذخیره نمی‌شوند
        public sealed record PersianMonth(int Year, int Month);

        private static PersianMonth PersianNow()
        {
            var pc = new PersianCalendar();
            var now = DateTime.Now;
            return new PersianMonth(pc.GetYear(now), pc.GetMonth(now));
        }

        private static long? NetToman(HrResult<Payslip> slip)
        {
            var line = slip.Value?.Lines.FirstOrDefault(l => l.Group == 1 && l.Sub == 2);
            var digits = BotText.ToLatinDigits(line?.Amount ?? "").Replace(",", "");
            return digits.Length > 0 && long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var rial) && rial > 0
                ? HrText.ToToman(rial)
                : null;
        }

        private static long RoundDown(long value, long step) => value / step * step;

        private static string Ordinal(int n) => n switch
        {
            1 => "اولین",
            2 => "دومین",
            3 => "سومین",
            _ => Fa(n.ToString(CultureInfo.InvariantCulture)) + "مین",
        };

        private static string Fa(string s) => BotText.ToPersianDigits(s);
    }
}
