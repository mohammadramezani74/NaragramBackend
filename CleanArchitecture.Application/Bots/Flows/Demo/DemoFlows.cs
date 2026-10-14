using CleanArchitecture.Application.Bots.Models;
using System.Text;

namespace CleanArchitecture.Application.Bots.Flows.Demo
{
    // ======================================================================
    // نمونه‌ها برای ربات آزمایشی و الگوی نوشتن جریان واقعی.
    // اتصال ساختگی است: هیچ API واقعی صدا زده نمی‌شود.
    // ======================================================================

    /// <summary>
    /// قرارداد اتصال منابع انسانی. جریان‌ها فقط این را می‌شناسند؛ نسخه‌ی واقعی
    /// (INTS) در فاز ۲ همین interface را پیاده می‌کند و جریان‌ها دست نمی‌خورند.
    /// </summary>
    public interface IHrConnector : IBotConnector
    {
        Task<long> GetAdvanceLimitAsync(Guid userId, CancellationToken ct);
        Task<HrRequestResult> SubmitAdvanceAsync(Guid userId, long amountToman, string type, CancellationToken ct);
    }

    public sealed record HrRequestResult(bool Success, string TrackingNumber, string Message);

    public sealed class MockHrConnector : IHrConnector
    {
        public string Key => "mock-hr";
        public string Title => "منابع انسانی (ساختگی)";
        public string? Description => "برای آزمایش؛ سقف مساعده ۱۵ میلیون و شماره‌ی پیگیری تصادفی برمی‌گرداند.";

        public Task<BotConnectorTestResult> TestAsync(CancellationToken cancellationToken)
            => Task.FromResult(new BotConnectorTestResult(true, "اتصال ساختگی همیشه سالم است."));

        public Task<long> GetAdvanceLimitAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(15_000_000L);

        public Task<HrRequestResult> SubmitAdvanceAsync(Guid userId, long amountToman, string type, CancellationToken ct)
            => Task.FromResult(new HrRequestResult(true, Random.Shared.Next(10000, 99999).ToString(), "ثبت شد"));
    }

    [BotFlow("demo.advance", "مساعده (آزمایشی)", Description = "مبلغ، تأیید و ثبت با اتصال ساختگی منابع انسانی.")]
    [FlowParam("type", "نوع مساعده", Options = new[] { "عادی", "اضطراری" })]
    public sealed class DemoAdvanceFlow(IHrConnector hr) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var type = c.Param("type", "عادی");
            var limit = await c.CallAsync("limit", ct => hr.GetAdvanceLimitAsync(c.User.Id, ct));

            var amount = await c.AskMoneyAsync(
                $"مساعده‌ی {type}: مبلغ را انتخاب کنید یا بنویسید.\nسقف مجاز این ماه: {BotText.Money(limit)}",
                min: 1_000_000,
                max: limit,
                suggestions: new[] { 5_000_000L, 10_000_000L, limit });

            var ok = await c.ConfirmAsync(
                "لطفاً درخواست را بررسی و تأیید کنید:",
                card: new[]
                {
                    new BotCardRow("نوع", "مساعده‌ی " + type),
                    new BotCardRow("مبلغ", BotText.Money(amount)),
                    new BotCardRow("متقاضی", c.User.FullName),
                },
                yes: "تأیید و ارسال");

            if (!ok)
            {
                await c.SayAsync("درخواست ثبت نشد.", menuButton: true);
                return;
            }

            var result = await c.CallAsync("submit", ct => hr.SubmitAdvanceAsync(c.User.Id, amount, type, ct));
            await c.SayAsync(
                result.Success
                    ? $"درخواست شما ثبت شد.\nشماره‌ی پیگیری: {BotText.ToPersianDigits(result.TrackingNumber)}"
                    : $"ثبت نشد: {result.Message}",
                menuButton: true);
        }
    }

    [BotFlow("demo.choice", "انتخاب و تاریخ (آزمایشی)", Description = "انتخاب با دکمه‌ی شیشه‌ای، انتخاب با کیبورد و تاریخ شمسی.")]
    public sealed class DemoChoiceFlow : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var kind = await c.ChooseAsync("نوع گواهی را انتخاب کنید:", new[]
            {
                new BotOption("employment", "اشتغال به کار"),
                new BotOption("salary", "کسر از حقوق"),
                new BotOption("other", "سایر"),
            });

            var copies = await c.ChooseAsync("چند نسخه لازم دارید؟", new[]
            {
                new BotOption("1", "۱"), new BotOption("2", "۲"), new BotOption("3", "۳"),
            }, asKeyboard: true);

            var date = await c.AskDateAsync("برای چه تاریخی لازم است؟", min: DateOnly.FromDateTime(DateTime.Now));

            var kindLabel = kind switch { "employment" => "اشتغال به کار", "salary" => "کسر از حقوق", _ => "سایر" };
            await c.SayAsync("درخواست آزمایشی شما:", menuButton: true, card: new[]
            {
                new BotCardRow("نوع گواهی", kindLabel),
                new BotCardRow("تعداد", BotText.ToPersianDigits(copies)),
                new BotCardRow("تاریخ", BotText.Date(date)),
            });
        }
    }

    [BotFlow("demo.echo", "تکرار متن (آزمایشی)", Description = "یک متن می‌پرسد و همان را برمی‌گرداند.")]
    public sealed class DemoEchoFlow : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var text = await c.AskTextAsync("هر چه بنویسید، همان را برمی‌گردانم:", maxLength: 300);
            await c.SayAsync("نوشتید: " + text, menuButton: true);
        }
    }

    [BotFlow("demo.document", "فایل نمونه (آزمایشی)", Description = "یک فایل متنی کوچک می‌فرستد.")]
    public sealed class DemoDocumentFlow : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var body = $"این یک فایل آزمایشی از ربات ناراگرام است.\nبرای: {c.User.FullName}\nتاریخ: {BotText.Date(DateOnly.FromDateTime(DateTime.Now))}\n";
            await c.SendDocumentAsync(Encoding.UTF8.GetBytes(body), "نمونه.txt", "فایل نمونه‌ی شما");
            await c.SayAsync("فایل فرستاده شد.", menuButton: true);
        }
    }
}
