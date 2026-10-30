using CleanArchitecture.Application.Bots.Flows;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>متن «کارنامه‌ی هفته»؛ هم فلوی «کارنامه‌ی من» و هم پیام خودکار چهارشنبه همین را می‌فرستند.</summary>
    public static class KartableReport
    {
        /// <returns>null اگر نه این هفته چیزی بررسی شده و نه چیزی منتظر است (چیزی برای گفتن نیست).</returns>
        public static string? Build(string? firstName, KartableWeekly w, List<KartableItem> pending, DateTime now)
        {
            if (w.Total == 0 && pending.Count == 0) return null;

            var lines = new List<string>
            {
                string.IsNullOrWhiteSpace(firstName) ? "📊 کارنامه‌ی هفت روز اخیر شما در کارتابل" : $"📊 {firstName.Trim()}، کارنامه‌ی هفت روز اخیر شما در کارتابل",
                "",
            };

            if (w.Total > 0)
            {
                var line = $"✅ {BotText.Number(w.Total)} مورد بررسی کردید";
                if (w.Rejected > 0) line += $" ({BotText.Number(w.Approved)} تایید، {BotText.Number(w.Rejected)} رد)";
                if (w.PrevTotal > 0 && w.PrevTotal != w.Total)
                    line += w.Total > w.PrevTotal
                        ? $"؛ {BotText.Number(w.Total - w.PrevTotal)} مورد بیشتر از هفته‌ی قبل"
                        : $"؛ هفته‌ی قبل {BotText.Number(w.PrevTotal)} مورد بود";
                lines.Add(line + ".");
            }
            else
            {
                lines.Add("این هفته موردی بررسی نکردید.");
            }

            if (w.MedianHours is double median)
            {
                var line = $"⏱ معمولاً {Duration(median)} بعد از رسیدن هر مورد، تصمیم گرفتید";
                if (w.PrevMedianHours is double prev && prev > 0)
                {
                    if (median <= prev * 0.8) line += $"؛ سریع‌تر از هفته‌ی قبل ({Duration(prev)}) 👏";
                    else if (median >= prev * 1.25) line += $"؛ هفته‌ی قبل {Duration(prev)} بود";
                }
                lines.Add(line.EndsWith("👏", StringComparison.Ordinal) ? line : line + ".");
            }

            if (w.ByType.Count > 1)
            {
                var top = w.ByType.OrderByDescending(t => t.Count).First();
                var info = KartableTypes.Of(top.Type);
                lines.Add($"{info.Icon} بیشترین کارتان: {info.Plural} ({BotText.Number(top.Count)} مورد).");
            }

            lines.Add("");
            if (pending.Count > 0)
            {
                var oldest = pending.OrderBy(i => i.SendTime).First();
                lines.Add($"📥 {BotText.Number(pending.Count)} مورد هنوز در کارتابل‌تان است؛ قدیمی‌ترینش {HrKartableFlow.Age(oldest.SendTime, now)} رسیده.");
            }
            else
            {
                lines.Add("✨ کارتابل‌تان خالی است.");
            }

            return string.Join("\n", lines);
        }

        private static string Duration(double hours)
        {
            if (hours < 1) return "کمتر از یک ساعت";
            if (hours < 24) return $"{BotText.Number((long)Math.Round(hours))} ساعت";
            var days = hours / 24;
            var text = days < 10 ? days.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : Math.Round(days).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return BotText.ToPersianDigits(text).Replace(".", "٫") + " روز";
        }
    }

    [BotFlow("kartable.report", "کارنامه‌ی من",
        Description = "کارنامه‌ی هفت روز اخیر در کارتابل INTS: چند مورد بررسی شد، چقدر سریع، در مقایسه با هفته‌ی قبل.")]
    public sealed class KartableReportFlow(IIntsConnector ints) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var phone = c.User.PhoneNumber;
            var weekly = await c.CallAsync("weekly", ct => KartableApi.GetWeeklyAsync(ints, phone, ct));
            if (!weekly.Ok || weekly.Value is null)
            {
                await c.SayAsync("⚠️ " + weekly.Message, menuButton: true);
                return;
            }
            var pending = await c.CallAsync("kartable", ct => KartableApi.GetAsync(ints, phone, ct));
            var now = await c.CallAsync("now", _ => Task.FromResult(DateTime.Now));

            var text = KartableReport.Build(c.User.FirstName, weekly.Value, pending.Value ?? new List<KartableItem>(), now)
                       ?? "این هفته هنوز موردی بررسی نکرده‌اید و کارتابل‌تان هم خالی است ✨";
            await c.SayAsync(text, menuButton: true);
        }
    }
}
