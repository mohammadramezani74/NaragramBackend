using System.Globalization;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Bots.Feedback
{
    // ======================================================================
    // ربات «پشتیبانی ناراگرام»: کاربر نظر، پیشنهاد یا مشکلش درباره‌ی خود اپ را می‌فرستد و
    // بعداً وضعیت و جواب را پیگیری می‌کند. ادمین در پنل («بازخوردها») می‌بیند و جواب می‌دهد؛
    // جواب از همین ربات به کاربر می‌رسد.
    // ======================================================================

    public static class FeedbackText
    {
        public static string Kind(string kind) => kind switch
        {
            AppFeedback.KindIdea => "💡 پیشنهاد",
            AppFeedback.KindBug => "🐞 گزارش مشکل",
            _ => "💬 نظر",
        };

        public static string Status(string status) => status switch
        {
            AppFeedback.StatusNew => "📨 ثبت شد، هنوز بررسی نشده",
            AppFeedback.StatusSeen => "👀 دیده شد",
            AppFeedback.StatusDoing => "🔧 در حال انجام",
            AppFeedback.StatusDone => "✅ انجام شد",
            AppFeedback.StatusRejected => "🗂 فعلاً در برنامه نیست",
            _ => status,
        };

        public static string Number(int n) => "#" + BotText.ToPersianDigits(n.ToString(CultureInfo.InvariantCulture));

        public static string Preview(string? text, int max)
            => string.IsNullOrEmpty(text) ? string.Empty : text.Length <= max ? text : text[..max] + "…";
    }

    [BotFlow("feedback.send", "ارسال نظر و پیشنهاد",
        Description = "پیشنهاد، گزارش مشکل یا نظر درباره‌ی اپ ناراگرام؛ در پنل ادمین بخش «بازخوردها» دیده و جواب داده می‌شود.")]
    public sealed class FeedbackSendFlow(IApplicationUnitOfWork uow) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var kind = await c.ChooseAsync("درباره‌ی چه می‌خواهید بنویسید؟", new[]
            {
                new BotOption(AppFeedback.KindIdea, "💡 پیشنهاد"),
                new BotOption(AppFeedback.KindBug, "🐞 گزارش مشکل"),
                new BotOption(AppFeedback.KindOpinion, "💬 نظر"),
            });

            var question = kind switch
            {
                AppFeedback.KindIdea => "پیشنهادتان را بنویسید: چه چیزی اضافه یا عوض شود تا ناراگرام برایتان بهتر شود؟",
                AppFeedback.KindBug => "مشکل را بنویسید: کجای برنامه بودید، چه کردید و چه شد؟ اگر روی گوشی است، مدل گوشی را هم بنویسید.",
                _ => "نظرتان را بنویسید؛ هر چیزی که در ناراگرام دوست دارید یا ندارید.",
            };
            var text = (await c.AskTextAsync(question, AppFeedback.MaxTextLength,
                validate: t => t.Trim().Length < 10 ? "لطفاً کمی بیشتر توضیح دهید (دست‌کم ۱۰ حرف)." : null,
                placeholder: "بنویسید…")).Trim();

            var ok = await c.ConfirmAsync("ارسال شود؟", new List<BotCardRow>
            {
                new("نوع", FeedbackText.Kind(kind)),
                new(FeedbackText.Preview(text, 300), "", "note"),
            }, yes: "ارسال", no: "انصراف");
            if (!ok)
            {
                await c.SayAsync("ارسال نشد. هر وقت خواستید از منو دوباره شروع کنید.", menuButton: true);
                return;
            }

            var number = await c.CallAsync("save", async ct =>
            {
                var feedback = AppFeedback.Create(c.User.Id, c.BotId == Guid.Empty ? null : c.BotId, kind, text);
                uow.AppFeedbacks.Add(feedback);
                await uow.SaveOrThrowAsync(ct);
                return feedback.Number;
            });

            await c.SayAsync(
                $"✅ ممنون! با شماره‌ی پیگیری {FeedbackText.Number(number)} ثبت شد.\n" +
                "تیم ناراگرام می‌خواندش و هر خبری شد همین‌جا به شما می‌گوییم.",
                menuButton: true);
        }
    }

    [BotFlow("feedback.mine", "پیگیری بازخوردهای من",
        Description = "ده نظر و پیشنهاد آخر کاربر، با وضعیت و جواب تیم ناراگرام.")]
    public sealed class FeedbackMineFlow(IApplicationUnitOfWork uow) : BotFlow
    {
        public sealed record Item(int Number, string Kind, string Text, string Status, DateTime CreatedAt, string? Reply);

        public override async Task RunAsync(BotFlowContext c)
        {
            var items = await c.CallAsync("mine", ct => uow.AppFeedbacks.AsNoTracking()
                .Where(f => f.UserId == c.User.Id)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .Select(f => new Item(f.Number, f.Kind, f.Text, f.Status, f.CreatedAt, f.Reply))
                .ToListAsync(ct));

            if (items.Count == 0)
            {
                await c.SayAsync("هنوز نظر یا پیشنهادی نفرستاده‌اید. از منو «ارسال نظر و پیشنهاد» را بزنید.", menuButton: true);
                return;
            }

            var rows = new List<BotCardRow>();
            foreach (var f in items)
            {
                rows.Add(new($"{FeedbackText.Number(f.Number)} · {FeedbackText.Kind(f.Kind)}", BotText.Date(DateOnly.FromDateTime(f.CreatedAt)), "section"));
                rows.Add(new("وضعیت", FeedbackText.Status(f.Status)));
                rows.Add(new(FeedbackText.Preview(f.Text, 140), "", "note"));
                if (!string.IsNullOrWhiteSpace(f.Reply))
                    rows.Add(new("💬 جواب تیم: " + FeedbackText.Preview(f.Reply, 400), "", "note"));
            }

            await c.SayAsync("📋 نظر و پیشنهادهای شما", menuButton: true, card: rows);
        }
    }
}
