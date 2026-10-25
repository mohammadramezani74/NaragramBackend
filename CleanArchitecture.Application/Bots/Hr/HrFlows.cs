using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Hr
{
    // ======================================================================
    // ربات «منابع انسانی». ادمین در پنل منوی ربات را می‌چیند و هر گزینه را به یکی از این
    // جریان‌ها وصل می‌کند. همه‌ی داده از INTS و «از طرف» همین کاربر خوانده می‌شود: موبایل از
    // حساب او در ناراگرام (BotFlowUser.PhoneNumber)، نه از چیزی که تایپ کند.
    // ======================================================================

    [BotFlow("hr.payslip", "فیش حقوقی", Description = "آخرین فیش حقوقی صادرشده (تا سه ماه اخیر) از INTS.")]
    public sealed class HrPayslipFlow(IIntsConnector ints) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var r = await c.CallAsync("payslip", ct => HrApi.GetPayslipAsync(ints, c.User.PhoneNumber, ct));
            if (!r.Ok || r.Value is null)
            {
                await c.SayAsync("⚠️ " + r.Message, menuButton: true);
                return;
            }

            var slip = r.Value;
            if (slip.Lines.Count == 0)
            {
                await c.SayAsync("فیش حقوقی سه ماه اخیر شما هنوز صادر نشده است.", menuButton: true);
                return;
            }

            var title = string.IsNullOrEmpty(slip.Title) ? "آخرین فیش حقوقی" : "فیش حقوقی " + BotText.ToPersianDigits(slip.Title);
            await c.SayAsync($"🧾 {title}", menuButton: true, card: HrPayslipView.Build(slip));
        }
    }

    [BotFlow("hr.profile", "مشخصات پرسنلی من", Description = "خلاصه‌ی پرونده‌ی پرسنلی از INTS: تحصیلات، قرارداد، بیمه، نظام وظیفه و اطلاعات تماس.")]
    public sealed class HrProfileFlow(IIntsConnector ints) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var r = await c.CallAsync("profile", ct => HrApi.GetProfileAsync(ints, c.User.PhoneNumber, ct));
            if (!r.Ok || r.Value is null)
            {
                await c.SayAsync("⚠️ " + r.Message, menuButton: true);
                return;
            }

            var p = r.Value;
            var today = await c.CallAsync("today", _ => Task.FromResult(PersianToday()));

            // عمداً کد ملی، دین، آدرس منزل و شماره‌ی بیمه نشان داده نمی‌شود: پیام ربات در
            // دیتابیس ناراگرام می‌ماند و این‌ها برای این کارت لازم نیستند.
            var rows = new List<BotCardRow> { new("نام", c.User.FullName) };
            Add(rows, "تحصیلات", p.Education);
            Add(rows, "شروع قرارداد", p.ContractStart);
            Add(rows, "بیمه", p.Insurance);
            if (p.Military is not null)
            {
                var military = p.Military;
                if (p.MilitaryExpire is not null)
                {
                    military += " تا " + p.MilitaryExpire;
                    if (string.CompareOrdinal(BotText.ToLatinDigits(p.MilitaryExpire), today) < 0)
                        military += " ⚠️ تمام شده";
                }
                Add(rows, "نظام وظیفه", military);
            }
            Add(rows, "وضعیت تأهل", p.MaritalStatus);
            Add(rows, "موبایل در پرونده", p.Mobile);
            if (p.Email is not null) rows.Add(new BotCardRow("ایمیل در پرونده", p.Email));

            await c.SayAsync(
                "👤 مشخصات پرسنلی شما در سامانه\n\nاگر چیزی اشتباه است، به واحد منابع انسانی خبر دهید.",
                menuButton: true,
                card: rows);
        }

        private static void Add(List<BotCardRow> rows, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) rows.Add(new BotCardRow(label, BotText.ToPersianDigits(value)));
        }

        private static string PersianToday()
        {
            var pc = new PersianCalendar();
            var now = DateTime.Now;
            return $"{pc.GetYear(now):0000}/{pc.GetMonth(now):00}/{pc.GetDayOfMonth(now):00}";
        }
    }

    [BotFlow("hr.requests", "درخواست‌های من", Description = "درخواست‌های پرسنلی کاربر و وضعیتشان؛ حذف درخواستی که هنوز بررسی نشده.")]
    public sealed class HrRequestsFlow(IIntsConnector ints) : BotFlow
    {
        private const int Shown = 10;

        public override async Task RunAsync(BotFlowContext c)
        {
            var r = await c.CallAsync("requests", ct => HrApi.GetRequestsAsync(ints, c.User.PhoneNumber, ct));
            if (!r.Ok || r.Value is null)
            {
                await c.SayAsync("⚠️ " + r.Message, menuButton: true);
                return;
            }

            var requests = r.Value;
            if (requests.Count == 0)
            {
                await c.SayAsync("هنوز درخواست پرسنلی ثبت نکرده‌اید.", menuButton: true);
                return;
            }

            var shown = requests.Take(Shown).ToList();
            var options = shown
                .Select(q => new BotOption(q.Id.ToString(), $"{StatusIcon(q)} {q.Type} · {BotText.ToPersianDigits(q.Date ?? "")} · #{BotText.ToPersianDigits(q.DocNo)}"))
                .Append(new BotOption("close", "بستن"))
                .ToList();

            var intro = requests.Count > Shown
                ? $"📂 {BotText.Number(requests.Count)} درخواست دارید؛ {BotText.Number(Shown)} مورد آخر:"
                : $"📂 {BotText.Number(requests.Count)} درخواست دارید:";
            intro += "\n✅ تأیید شده  ❌ رد شده  ⏳ در جریان\n\nبرای دیدن جزئیات روی هر کدام بزنید.";

            var picked = await c.ChooseAsync(intro, options);
            if (picked == "close")
            {
                await c.SayAsync("بسته شد.", menuButton: true);
                return;
            }

            var q = shown.First(x => x.Id.ToString() == picked);
            var card = Details(q);
            var title = $"{StatusIcon(q)} {q.Type}، شماره‌ی {BotText.ToPersianDigits(q.DocNo)}";

            if (!q.CanDelete)
            {
                await c.SayAsync(title, menuButton: true, card: card);
                return;
            }

            var delete = await c.ConfirmAsync(
                title + "\n\nاین درخواست هنوز بررسی نشده و می‌توانید حذفش کنید.",
                card: card,
                yes: "🗑 حذف درخواست",
                no: "نگه دار");
            if (!delete)
            {
                await c.SayAsync("درخواست سر جایش ماند.", menuButton: true);
                return;
            }

            var sure = await c.ConfirmAsync("مطمئنید؟ حذف برگشت ندارد.", yes: "بله، حذف شود", no: "نه");
            if (!sure)
            {
                await c.SayAsync("درخواست سر جایش ماند.", menuButton: true);
                return;
            }

            var d = await c.CallAsync("delete", ct => HrApi.DeleteRequestAsync(ints, c.User.PhoneNumber, q.Id, ct));
            await c.SayAsync(d.Ok ? "🗑 درخواست حذف شد." : "⚠️ " + d.Message, menuButton: true);
        }

        private static string StatusIcon(HrRequest q)
        {
            var s = q.Status ?? "";
            if (s.Contains("عدم") || s.Contains("رد شد") || s.Contains("مردود")) return "❌";
            if (s.Contains("تایید درخواست") || s.Contains("تأیید درخواست")) return "✅";
            return "⏳";
        }

        private static List<BotCardRow> Details(HrRequest q)
        {
            var rows = new List<BotCardRow>
            {
                new("نوع", q.Type),
                new("وضعیت", q.Status ?? (q.Events.Count > 0 ? "در جریان بررسی" : "ثبت شده")),
            };
            if (q.Date is not null) rows.Add(new("تاریخ درخواست", BotText.ToPersianDigits(q.Date)));
            if (q.Price is decimal price && price > 0) rows.Add(new("مبلغ", HrText.Money(price)));
            if (q.Organization is not null) rows.Add(new("ارگان درخواست‌کننده", BotText.ToPersianDigits(q.Organization)));
            if (q.Beneficiary is not null) rows.Add(new("ذی‌نفع", q.Beneficiary));
            if (q.HasCheckbook is bool cb) rows.Add(new("دسته‌چک", cb ? "دارد" : "ندارد"));
            for (var i = 0; i < q.Events.Count; i++)
                rows.Add(new($"مرحله‌ی {BotText.Number(i + 1)}", BotText.ToPersianDigits(q.Events[i])));
            return rows;
        }
    }
}
