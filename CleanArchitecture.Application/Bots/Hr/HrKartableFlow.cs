using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>
    /// کارتابل INTS در ربات: ضمانت‌نامه، درخواست پرسنلی، درخواست کالا و نظام پیشنهادات.
    ///
    /// «هوشمند» یعنی ربات قبل از اینکه کاربر بپرسد فکر کرده باشد: خلاصه‌ی کارتابل (چند تا،
    /// چه نوعی، کدام دیر شده)، مرور از قدیمی‌ترین، کنار هر مورد مسیری که تا اینجا آمده و
    /// نکته‌ها (چند روز منتظر مانده، در مرحله‌ای رد شده، فرستنده موارد دیگری هم دارد، مساعده
    /// بیش از ۶۰٪)، و دلیل‌های آماده برای رد.
    ///
    /// ربات هیچ‌وقت خودش تصمیم نمی‌گیرد؛ هر تایید و رد با دکمه‌ی خود کاربر است و همان متد
    /// پنل INTS اجرا می‌شود (نقش‌ها، گردش کار و پیامک‌ها همان است).
    /// </summary>
    [BotFlow("kartable", "کارتابل من",
        Description = "مرور و تایید/رد موارد کارتابل INTS: ضمانت‌نامه، درخواست پرسنلی، درخواست کالا و نظام پیشنهادات.")]
    [FlowParam("type", "فقط این نوع", Options = new[] { AllTypes, "ضمانت‌نامه", "درخواست پرسنلی", "درخواست کالا", "پیشنهاد" }, Required = false)]
    public sealed class HrKartableFlow(IIntsConnector ints) : BotFlow
    {
        public const string AllTypes = "همه";
        private const int MaxPerSession = 25;
        internal const int LateDays = 3;
        private const string NoComment = "بدون توضیح";

        public override async Task RunAsync(BotFlowContext c)
        {
            var phone = c.User.PhoneNumber;
            var r = await c.CallAsync("kartable", ct => KartableApi.GetAsync(ints, phone, ct));
            if (!r.Ok || r.Value is null)
            {
                await c.SayAsync("⚠️ " + r.Message, menuButton: true);
                return;
            }

            var now = await c.CallAsync("now", _ => Task.FromResult(DateTime.Now));
            var all = r.Value;

            var fixedTitle = c.Param("type", AllTypes);
            var fixedType = KartableTypes.All.FirstOrDefault(i => i.Title == fixedTitle);
            if (fixedType is not null) all = all.Where(i => i.Type == fixedType.Type).ToList();

            if (all.Count == 0)
            {
                await c.SayAsync(fixedType is null
                    ? "✨ کارتابل شما خالی است؛ هیچ موردی منتظر شما نیست."
                    : $"✨ هیچ {fixedType.Title}ی منتظر شما نیست.", menuButton: true);
                return;
            }

            // ---------------------------------------------------------------- خلاصه
            var queue = all.OrderBy(i => i.SendTime).ToList();   // قدیمی‌ترین اول: هم منصفانه، هم فوری‌تر
            var options = new List<BotOption> { new("start", queue.Count == 1 ? "▶️ ببینیم" : "▶️ شروع از قدیمی‌ترین", "primary") };
            var groups = all.GroupBy(i => i.Type).ToList();
            if (groups.Count > 1)
                options.AddRange(groups.Select(g =>
                {
                    var info = KartableTypes.Of(g.Key);
                    return new BotOption("t:" + g.Key, $"{info.Icon} فقط {info.Plural} ({BotText.Number(g.Count())})");
                }));
            options.Add(new BotOption("close", "بعداً"));

            var picked = await c.ChooseAsync(Summary(all, now), options);
            if (picked == "close")
            {
                await c.SayAsync("باشه، هر وقت خواستید از منو «کارتابل من» را بزنید.", menuButton: true);
                return;
            }
            if (picked.StartsWith("t:", StringComparison.Ordinal))
                queue = queue.Where(i => i.Type == picked[2..]).ToList();

            var capped = queue.Count > MaxPerSession;
            if (capped) queue = queue.Take(MaxPerSession).ToList();

            // ---------------------------------------------------------------- مرور
            int approved = 0, rejected = 0, referred = 0, skipped = 0;
            var handled = new HashSet<Guid>();
            var prefix = "";
            for (var n = 0; n < queue.Count; n++)
            {
                var item = queue[n];
                if (handled.Contains(item.Id)) continue;
                var info = KartableTypes.Of(item.Type);
                var steps = await StepsAsync(c, item);
                var detail = await DetailAsync(c, item);

                // تایید گروهی: موارد هم‌نوع و هم‌موضوعِ بررسی‌نشده که هیچ هشداری ندارند
                var group = new List<KartableItem>();
                var withWarning = 0;
                if (item.Type != KartableTypes.Suggestion && item.Subject.Length > 0 && !HasWarning(item, steps))
                {
                    foreach (var other in queue.Where(o => !handled.Contains(o.Id) && o.Type == item.Type && o.Subject == item.Subject))
                        if (other.Id == item.Id || !HasWarning(other, await StepsAsync(c, other)))
                            group.Add(other);
                        else
                            withWarning++;
                }

                var title = $"{prefix}{info.Icon} {Name(item)}" + (item.DocNo.Length > 0 ? $" · شماره‌ی {Fa(item.DocNo)}" : "")
                            + $"\n({BotText.Number(handled.Count + 1)} از {BotText.Number(queue.Count)})";
                prefix = "";

                var buttons = new List<BotOption>();
                if (detail?.MustRefer == true)
                {
                    buttons.Add(new BotOption("refer", "↪️ ارجاع به همکار", "primary"));
                }
                else
                {
                    buttons.Add(new BotOption("approve", "✅ تایید", "primary"));
                    buttons.Add(new BotOption("reject", "❌ رد"));
                    if (detail?.CanRefer == true) buttons.Add(new BotOption("refer", "↪️ ارجاع به همکار"));
                }
                if (group.Count >= 2)
                    buttons.Add(new BotOption("bulk", $"☑️ تایید هر {BotText.Number(group.Count)} {Name(item)}"));
                buttons.Add(new BotOption("skip", handled.Count + 1 < queue.Count ? "⏭ بعدی" : "⏭ بعداً"));
                buttons.Add(new BotOption("stop", "⏹ کافیه"));

                var action = await c.ChooseAsync(title, buttons, card: Card(item, steps, detail, all, now));

                if (action == "stop") break;
                if (action == "skip") { handled.Add(item.Id); skipped++; continue; }

                if (action == "bulk")
                {
                    var bulk = await BulkApproveAsync(c, group, withWarning, now);
                    if (bulk is null) { n--; continue; }   // منصرف شد: همین مورد دوباره، تکی

                    foreach (var g in group) handled.Add(g.Id);
                    approved += bulk.Value.Ok;
                    skipped += group.Count - bulk.Value.Ok;
                    prefix = bulk.Value.Text + "\n\n";
                    continue;
                }

                if (action == "refer")
                {
                    var to = await PickColleagueAsync(c, item);
                    if (to is null) { n--; continue; }   // منصرف شد: همین مورد دوباره

                    var note = (await c.AskTextAsync(
                        $"📝 نظر یا توضیح شما برای {to.Value.Name}؟",
                        maxLength: 500,
                        validate: x => x.Trim().Length < 3 ? "چند کلمه توضیح بنویسید." : null,
                        suggestions: new[] { "لطفاً بررسی و اعلام نظر کنید", "از نظر فنی بررسی شود", "از نظر هزینه بررسی شود" })).Trim();

                    handled.Add(item.Id);
                    var rd = await DecideAsync(c, item, approve: false, note, to.Value.Id);
                    if (rd.Ok) { referred++; prefix = $"↪️ قبلی به {to.Value.Name} ارجاع شد.\n\n"; }
                    else { skipped++; prefix = $"⚠️ قبلی ارجاع نشد: {rd.Message}\n\n"; }
                    continue;
                }

                handled.Add(item.Id);
                var approve = action == "approve";
                var comment = approve
                    ? await ApproveCommentAsync(c, "✅ توضیحی برای تایید دارید؟")
                    : (await c.AskTextAsync(
                        "❌ دلیل رد را بنویسید یا یکی از پیشنهادها را بزنید؛ برای درخواست‌کننده فرستاده می‌شود.",
                        maxLength: 500,
                        validate: x => x.Trim().Length < 3 ? "دلیل رد را کمی کامل‌تر بنویسید." : null,
                        suggestions: info.RejectReasons)).Trim();

                var d = await DecideAsync(c, item, approve, comment);
                if (d.Ok)
                {
                    if (approve) approved++; else rejected++;
                    prefix = approve ? "✅ قبلی تایید شد.\n\n" : "❌ قبلی رد شد.\n\n";
                }
                else if (item.Type == KartableTypes.Suggestion && (d.Message ?? "").Contains("ارجاع"))
                {
                    skipped++;
                    prefix = "↪️ آن پیشنهاد باید به کسی ارجاع شود و ارجاع فعلاً فقط از پنل INTS ممکن است؛ در کارتابل ماند.\n\n";
                }
                else
                {
                    skipped++;
                    prefix = $"⚠️ قبلی انجام نشد: {d.Message}\n\n";
                }
            }

            // ---------------------------------------------------------------- جمع‌بندی
            var left = all.Count - approved - rejected - referred;
            var lines = new List<string>();
            if (prefix.Length > 0) lines.Add(prefix.TrimEnd());
            lines.Add(left == 0 ? "🎉 کارتابل شما خالی شد." : $"📥 {BotText.Number(left)} مورد هنوز در کارتابل شماست.");
            if (capped) lines.Add($"(هر بار حداکثر {BotText.Number(MaxPerSession)} مورد مرور می‌شود؛ برای بقیه دوباره «کارتابل من» را بزنید.)");

            var card = new List<BotCardRow>();
            if (approved > 0) card.Add(new BotCardRow("تایید شده", BotText.Number(approved)));
            if (rejected > 0) card.Add(new BotCardRow("رد شده", BotText.Number(rejected)));
            if (referred > 0) card.Add(new BotCardRow("ارجاع شده", BotText.Number(referred)));
            if (skipped > 0) card.Add(new BotCardRow("ماند برای بعد", BotText.Number(skipped)));

            await c.SayAsync(string.Join("\n", lines), menuButton: true, card: card.Count > 0 ? card : null);
        }

        // ------------------------------------------------------------ تایید گروهی

        /// <summary>
        /// چند مورد هم‌نوع و هم‌موضوع را با یک تایید. فقط مواردی که هیچ هشداری ندارند به اینجا
        /// می‌رسند (رد شده در مرحله‌ی قبل، مساعده‌ی بیش از ۶۰٪) و رد گروهی عمداً وجود ندارد:
        /// رد کردن دلیل مخصوص هر نفر می‌خواهد. null یعنی کاربر منصرف شد.
        /// </summary>
        private async Task<(int Ok, string Text)?> BulkApproveAsync(BotFlowContext c, List<KartableItem> group, int withWarning, DateTime now)
        {
            var card = group
                .Select(g => new BotCardRow(g.Sender.Length > 0 ? g.Sender : "—",
                    (g.DocNo.Length > 0 ? $"#{Fa(g.DocNo)} · " : "") + Age(g.SendTime, now)))
                .ToList();
            if (withWarning > 0)
                card.Add(new BotCardRow($"⚠️ {BotText.Number(withWarning)} مورد مشابه دیگر هشدار دارد و جدا، تکی نشانتان می‌دهم.", "", "note"));

            var sure = await c.ConfirmAsync(
                $"☑️ هر {BotText.Number(group.Count)} مورد «{Name(group[0])}» تایید شود؟",
                card: card, yes: "بله، همه تایید", no: "نه، تکی می‌بینم");
            if (!sure) return null;

            var comment = await ApproveCommentAsync(c, "توضیحی برای همه‌شان دارید؟");

            var ok = 0;
            var failed = new List<string>();
            foreach (var g in group)
            {
                var d = await DecideAsync(c, g, approve: true, comment);
                if (d.Ok) ok++;
                else failed.Add($"#{Fa(g.DocNo)} ({g.Sender}): {d.Message}");
            }

            var text = ok > 0 ? $"☑️ {BotText.Number(ok)} مورد یکجا تایید شد." : "⚠️ هیچ‌کدام تایید نشد.";
            if (failed.Count > 0)
                text += "\nانجام نشد:\n" + string.Join("\n", failed.Take(5).Select(f => "• " + f));
            return (ok, text);
        }

        // ------------------------------------------------------------ اجرای تصمیم و مسیر

        private Task<HrResult<List<KartableStep>>> HistoryCallAsync(BotFlowContext c, KartableItem item)
            => c.CallAsync($"h:{item.Type}:{item.Id}", ct => KartableApi.GetHistoryAsync(ints, c.User.PhoneNumber, item.Type, item.Id, ct));

        /// <summary>مسیر برای نکته‌ها و هشدارهاست؛ اگر نیامد بدون آن ادامه می‌دهیم.</summary>
        private async Task<List<KartableStep>> StepsAsync(BotFlowContext c, KartableItem item)
            => (await HistoryCallAsync(c, item)).Value ?? new List<KartableStep>();

        /// <summary>کلید هر تصمیم شناسه‌ی همان مورد است؛ پس هیچ موردی دو بار فرستاده نمی‌شود.</summary>
        private Task<HrResult<bool>> DecideAsync(BotFlowContext c, KartableItem item, bool approve, string comment, Guid? referTo = null)
            => c.CallAsync($"d:{item.Type}:{item.Id}",
                ct => KartableApi.DecideAsync(ints, c.User.PhoneNumber, item.Type, item.Id, approve, comment, ct, referTo));

        /// <summary>مبلغ، مشتری، سابقه و نکته‌های INTS؛ اگر نیامد، کارت با اطلاعات لیست ساخته می‌شود.</summary>
        private async Task<KartableDetail?> DetailAsync(BotFlowContext c, KartableItem item)
            => (await c.CallAsync($"x:{item.Type}:{item.Id}",
                ct => KartableApi.GetDetailAsync(ints, c.User.PhoneNumber, item.Type, item.Id, ct))).Value;

        /// <summary>جستجوی همکار با نام برای ارجاع؛ null یعنی منصرف شد.</summary>
        private async Task<(Guid Id, string Name)?> PickColleagueAsync(BotFlowContext c, KartableItem item)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var q = (await c.AskTextAsync(
                    "↪️ به چه کسی ارجاع بدهم؟ بخشی از نام یا نام خانوادگی‌اش را بنویسید.",
                    maxLength: 50,
                    validate: x => x.Trim().Length < 2 ? "حداقل دو حرف بنویسید." : null,
                    placeholder: "مثلاً رضایی")).Trim();

                var found = await c.CallAsync($"p:{item.Id}:{attempt}",
                    ct => KartableApi.SearchPersonnelAsync(ints, c.User.PhoneNumber, q, ct));
                if (!found.Ok || found.Value is null)
                {
                    await c.SayAsync("⚠️ " + found.Message);
                    return null;
                }

                var options = found.Value.Select(p => new BotOption(p.Id.ToString(), "👤 " + p.Name)).ToList();
                options.Add(new BotOption("again", "🔎 جستجوی دوباره"));
                options.Add(new BotOption("cancel", "انصراف"));
                var text = found.Value.Count == 0
                    ? $"کسی با «{q}» پیدا نشد."
                    : found.Value.Count == 1 ? "این همکار پیدا شد:" : $"{BotText.Number(found.Value.Count)} همکار پیدا شد؛ کدام؟";

                var pick = await c.ChooseAsync(text, options);
                if (pick == "cancel") return null;
                if (pick == "again") continue;
                var person = found.Value.First(p => p.Id.ToString() == pick);
                return (person.Id, person.Name);
            }

            await c.SayAsync("پیدا نشد؛ از پنل INTS ارجاع دهید.");
            return null;
        }

        private static async Task<string> ApproveCommentAsync(BotFlowContext c, string question)
        {
            var answer = (await c.AskTextAsync(question, maxLength: 500,
                suggestions: new[] { NoComment, "تایید شد، با تشکر", "تایید با رعایت سقف مصوب" })).Trim();
            return answer == NoComment ? "" : answer;
        }

        /// <summary>چیزی که باید جدا و با دقت دیده شود؛ این‌ها هیچ‌وقت در تایید گروهی نمی‌آیند.</summary>
        private static bool HasWarning(KartableItem item, List<KartableStep> steps)
        {
            var done = steps.Where(s => !s.IsOpen).ToList();
            if (done.Any(s => IsRejection(s.Status))) return true;
            var text = item.Description + " " + item.Status + " " + string.Join(" ", done.Select(s => s.Comment));
            return BotText.ToLatinDigits(text).Contains("60 درصد");
        }

        // ------------------------------------------------------------ خلاصه‌ی اول

        /// <summary>خلاصه‌ی کارتابل؛ پیام صبحگاهی (KartableDigestService) هم همین را می‌فرستد.</summary>
        internal static string Summary(List<KartableItem> all, DateTime now)
        {
            var lines = new List<string> { $"📥 {BotText.Number(all.Count)} مورد منتظر شماست:" };
            foreach (var g in all.GroupBy(i => i.Type).OrderByDescending(g => g.Count()))
            {
                var info = KartableTypes.Of(g.Key);
                lines.Add($"{info.Icon} {BotText.Number(g.Count())} {info.Plural}");
            }

            lines.Add("");
            var oldest = all.OrderBy(i => i.SendTime).First();
            lines.Add($"⏳ قدیمی‌ترین: {Name(oldest)} از {oldest.Sender}، {Age(oldest.SendTime, now)}.");

            var late = all.Count(i => Days(i.SendTime, now) >= LateDays);
            if (late > 0)
                lines.Add($"⚠️ {BotText.Number(late)} مورد بیش از {BotText.Number(LateDays)} روز است منتظر مانده.");

            var top = all.GroupBy(i => i.Sender).Where(g => g.Key.Length > 0 && g.Count() > 1).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (top is not null)
                lines.Add($"👤 {top.Key} {BotText.Number(top.Count())} مورد برای شما فرستاده.");

            return string.Join("\n", lines);
        }

        // ------------------------------------------------------------ کارت هر مورد

        private static List<BotCardRow> Card(KartableItem item, List<KartableStep> steps, KartableDetail? detail, List<KartableItem> all, DateTime now)
        {
            var info = KartableTypes.Of(item.Type);
            var rows = new List<BotCardRow>();
            if (item.Sender.Length > 0) rows.Add(new("فرستنده", item.Sender));
            if (item.SendTime != default)
                rows.Add(new("رسیده", $"{Age(item.SendTime, now)} · {BotText.Date(DateOnly.FromDateTime(item.SendTime))}"));
            if (item.Status.Length > 0) rows.Add(new("وضعیت", item.Status));
            // هشدار ۶۰٪ را INTS داخل < > در توضیح می‌گذارد؛ آن را در «نکته‌ها» می‌گوییم
            var description = item.Description.Trim();
            if (description.StartsWith("<") && description.EndsWith(">")) description = "";
            if (description.Length > 0) rows.Add(new("توضیح", Fa(Short(description, 300))));

            // جزئیات خود نوع (مبلغ، مشتری، تاریخ تحویل، ...)
            if (detail is not null)
                foreach (var f in detail.Fields)
                    rows.Add(new(f.Label, f.MoneyRial is decimal rial ? HrText.Money(rial) : Fa(Short(f.Value, 300))));

            // مسیری که تا اینجا آمده (فقط مرحله‌های بسته؛ مرحله‌ی باز همین کارتابل شماست)
            var done = steps.Where(s => !s.IsOpen && s.From.Length > 0).ToList();
            if (done.Count > 0)
            {
                rows.Add(new("مسیر تا اینجا", "", "section"));
                foreach (var s in done.TakeLast(5))
                {
                    var value = s.Status.Length > 0 ? s.Status : "ارسال شد";
                    if (s.Comment.Length > 0 && s.Comment != item.Description) value += $" · «{Short(s.Comment, 80)}»";
                    if (s.Time != default) value += " · " + BotText.Date(DateOnly.FromDateTime(s.Time));
                    rows.Add(new(s.From, Fa(value)));
                }
            }

            var notes = (detail?.Insights ?? new List<string>()).Select(t => Fa(System.Text.RegularExpressions.Regex.Replace(t, @"(\d)\.(\d)", "$1٫$2"))).Concat(Insights(item, done, all, now)).Distinct().ToList();
            if (notes.Count > 0)
            {
                rows.Add(new("نکته‌ها", "", "section"));
                rows.AddRange(notes.Select(t => new BotCardRow(t, "", "note")));
            }
            return rows;
        }

        private static List<string> Insights(KartableItem item, List<KartableStep> done, List<KartableItem> all, DateTime now)
        {
            var notes = new List<string>();

            var days = Days(item.SendTime, now);
            if (item.SendTime != default && days >= LateDays)
                notes.Add($"⏰ {BotText.Number(days)} روز است در کارتابل شما منتظر مانده.");

            if (done.Any(s => IsRejection(s.Status)))
                notes.Add("↩️ این مورد قبلاً در یکی از مرحله‌ها رد شده و دوباره در گردش است.");
            else if (done.Count >= 2)
                notes.Add($"🧭 از {BotText.Number(done.Count)} مرحله گذشته و حالا نوبت شماست.");

            var text = item.Description + " " + item.Status + " " + string.Join(" ", done.Select(s => s.Comment));
            if (BotText.ToLatinDigits(text).Contains("60 درصد"))
                notes.Add("⚠️ مبلغ این مساعده بیشتر از ۶۰٪ حقوق پایه‌ی درخواست‌کننده است.");

            var sameSender = all.Count(i => i.Id != item.Id && i.Sender == item.Sender && item.Sender.Length > 0);
            if (sameSender > 0)
                notes.Add($"👤 {item.Sender} {BotText.Number(sameSender)} مورد دیگر هم در کارتابل شما دارد.");

            return notes;
        }

        // ------------------------------------------------------------ کمکی

        // «رد» تنها کافی نیست: «در گردش» هم «رد» دارد
        private static bool IsRejection(string status)
            => status.Contains("رد شد") || status.Contains("عدم تایید") || status.Contains("عدم تأیید") || status.Contains("مردود");

        internal static int Days(DateTime time, DateTime now)
            => time == default ? 0 : Math.Max(0, (now.Date - time.Date).Days);

        internal static string Age(DateTime time, DateTime now)
        {
            if (time == default) return "";
            var d = Days(time, now);
            return d switch
            {
                0 => "امروز",
                1 => "دیروز",
                _ => $"{BotText.Number(d)} روز پیش",
            };
        }

        /// <summary>«درخواست پرسنلی: مساعده»؛ عنوان فرآیند INTS (مثل «درخواست ها») خوانا نیست.</summary>
        internal static string Name(KartableItem item)
        {
            var info = KartableTypes.Of(item.Type);
            return item.Subject.Length > 0 && item.Subject != info.Title ? $"{info.Title}: {Fa(item.Subject)}" : info.Title;
        }

        private static string Short(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";

        private static string Fa(string s) => BotText.ToPersianDigits(s);
    }
}
