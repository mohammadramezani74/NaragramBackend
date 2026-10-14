using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Models;
using System.Globalization;

namespace CleanArchitecture.Application.Bots.Flows
{
    /// <summary>
    /// ابزار جریان برای حرف زدن با کاربر. هر Ask یک سؤال می‌فرستد و تا جواب معتبر
    /// بیاید منتظر می‌ماند؛ اگر جواب نامعتبر باشد، پیام خطا می‌رود و همان سؤال
    /// منتظر می‌ماند. کاربر هر وقت «/انصراف» بزند جریان قطع می‌شود.
    /// </summary>
    public sealed class BotFlowContext
    {
        private readonly BotFlowExecution _run;

        internal BotFlowContext(BotFlowExecution run, BotFlowUser user, IReadOnlyDictionary<string, string> parameters)
        {
            _run = run;
            User = user;
            Params = parameters;
        }

        public BotFlowUser User { get; }

        /// <summary>تنظیم‌هایی که ادمین برای این گزینه‌ی منو داده (FlowParam).</summary>
        public IReadOnlyDictionary<string, string> Params { get; }

        public CancellationToken CancellationToken => _run.CancellationToken;

        public string Param(string key, string fallback = "")
            => Params.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        // ---------------------------------------------------------------- خروجی

        /// <param name="menuButton">دکمه‌ی «بازگشت به منو» زیر پیام.</param>
        public Task SayAsync(string text, bool menuButton = false, IEnumerable<BotCardRow>? card = null)
            => _run.SayAsync(text, menuButton, card?.ToList());

        public Task SendDocumentAsync(byte[] data, string fileName, string? caption = null)
            => _run.SendDocumentAsync(data, fileName, caption);

        /// <summary>
        /// هر کار بیرونی (API، دیتابیس، زمان) را با این صدا بزنید. نتیجه با کلید ذخیره
        /// می‌شود و در ادامه‌ی جریان دوباره اجرا نمی‌شود. خروجی باید JSON‌پذیر باشد.
        /// </summary>
        public Task<T> CallAsync<T>(string key, Func<CancellationToken, Task<T>> call)
            => _run.CallAsync(key, call);

        // ---------------------------------------------------------------- سؤال‌ها

        public async Task<string> AskTextAsync(
            string question,
            int maxLength = 1000,
            Func<string, string?>? validate = null,
            IEnumerable<string>? suggestions = null,
            string placeholder = "پاسخ را بنویسید…")
        {
            var answer = await _run.AskAsync(new BotQuestion
            {
                Kind = "text",
                Text = question,
                Placeholder = placeholder,
                Keyboard = BotFlowExecution.SuggestionKeyboard(suggestions),
                Parse = raw =>
                {
                    if (raw.Length == 0) return BotParseResult.Fail("پاسخ خالی است؛ لطفاً بنویسید.");
                    if (raw.Length > maxLength) return BotParseResult.Fail($"پاسخ حداکثر {BotText.Number(maxLength)} نویسه است.");
                    var error = validate?.Invoke(raw);
                    return error is null ? BotParseResult.Success(raw) : BotParseResult.Fail(error);
                },
            });
            return answer.Value;
        }

        public async Task<long> AskNumberAsync(
            string question,
            long min = 0,
            long max = long.MaxValue,
            IEnumerable<long>? suggestions = null,
            string? unit = null)
        {
            var suffix = string.IsNullOrEmpty(unit) ? string.Empty : " " + unit;
            var answer = await _run.AskAsync(new BotQuestion
            {
                Kind = "number",
                Text = question,
                Placeholder = "عدد را بنویسید…",
                Keyboard = BotFlowExecution.SuggestionKeyboard(suggestions?.Select(s => BotText.Number(s) + suffix)),
                Parse = raw =>
                {
                    if (!TryParseNumber(raw, out var n))
                        return BotParseResult.Fail("لطفاً فقط عدد بنویسید.");
                    if (n < min || n > max)
                        return BotParseResult.Fail($"عدد باید بین {BotText.Number(min)} و {BotText.Number(max)}{suffix} باشد.");
                    return BotParseResult.Success(n.ToString(CultureInfo.InvariantCulture), BotText.Number(n) + suffix);
                },
            });
            return long.Parse(answer.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>مبلغ به تومان. «۱۰٬۰۰۰٬۰۰۰ تومان» و «10000000» هر دو قبول است.</summary>
        public async Task<long> AskMoneyAsync(
            string question,
            long min,
            long max,
            IEnumerable<long>? suggestions = null)
        {
            var answer = await _run.AskAsync(new BotQuestion
            {
                Kind = "money",
                Text = question,
                Placeholder = "مبلغ را به تومان بنویسید…",
                Keyboard = BotFlowExecution.SuggestionKeyboard(suggestions?.Select(BotText.Money)),
                Parse = raw =>
                {
                    if (!TryParseNumber(raw, out var n))
                        return BotParseResult.Fail("لطفاً مبلغ را با عدد بنویسید، مثلاً ۵۰۰۰۰۰۰.");
                    if (n < min || n > max)
                        return BotParseResult.Fail($"مبلغ باید بین {BotText.Money(min)} و {BotText.Money(max)} باشد.");
                    return BotParseResult.Success(n.ToString(CultureInfo.InvariantCulture), BotText.Money(n));
                },
            });
            return long.Parse(answer.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>تاریخ شمسی، مثل ۱۴۰۵/۰۷/۲۲. «امروز» و «فردا» هم قبول است.</summary>
        public async Task<DateOnly> AskDateAsync(string question, DateOnly? min = null, DateOnly? max = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var answer = await _run.AskAsync(new BotQuestion
            {
                Kind = "date",
                Text = question,
                Placeholder = "مثلاً ۱۴۰۵/۰۷/۲۲",
                Keyboard = BotFlowExecution.SuggestionKeyboard(new[] { "امروز", "فردا" }),
                Parse = raw =>
                {
                    DateOnly date;
                    if (raw == "امروز") date = today;
                    else if (raw == "فردا") date = today.AddDays(1);
                    else if (!TryParsePersianDate(raw, out date))
                        return BotParseResult.Fail("تاریخ را به شکل ۱۴۰۵/۰۷/۲۲ بنویسید.");

                    if (min is DateOnly lo && date < lo)
                        return BotParseResult.Fail($"تاریخ نباید قبل از {BotText.Date(lo)} باشد.");
                    if (max is DateOnly hi && date > hi)
                        return BotParseResult.Fail($"تاریخ نباید بعد از {BotText.Date(hi)} باشد.");
                    return BotParseResult.Success(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), BotText.Date(date));
                },
            });
            return DateOnly.ParseExact(answer.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// انتخاب از چند گزینه. پیش‌فرض دکمه‌ی شیشه‌ای زیر سؤال است؛ با asKeyboard
        /// گزینه‌ها جای کیبورد می‌نشینند. نوشتن متن گزینه هم قبول است.
        /// </summary>
        public async Task<string> ChooseAsync(
            string question,
            IEnumerable<BotOption> options,
            bool asKeyboard = false,
            IEnumerable<BotCardRow>? card = null)
        {
            var list = options.ToList();
            if (list.Count == 0)
                throw new ArgumentException("ChooseAsync needs at least one option.", nameof(options));

            var answer = await _run.AskAsync(new BotQuestion
            {
                Kind = "choice",
                Text = question,
                Placeholder = "یکی از گزینه‌ها را انتخاب کنید",
                Options = list,
                InlineOptions = !asKeyboard,
                Keyboard = asKeyboard ? BotFlowExecution.SuggestionKeyboard(list.Select(o => o.Label)) : null,
                Card = card?.ToList(),
                Parse = raw =>
                {
                    var match = list.FirstOrDefault(o => string.Equals(o.Label, raw, StringComparison.Ordinal));
                    return match is null
                        ? BotParseResult.Fail("لطفاً یکی از گزینه‌ها را انتخاب کنید.")
                        : BotParseResult.Success(match.Value, match.Label);
                },
            });
            return answer.Value;
        }

        /// <summary>سؤال بله/خیر با دو دکمه، اختیاری با کارت خلاصه بالای دکمه‌ها.</summary>
        public async Task<bool> ConfirmAsync(
            string question,
            IEnumerable<BotCardRow>? card = null,
            string yes = "تأیید",
            string no = "انصراف")
        {
            var value = await ChooseAsync(
                question,
                new[] { new BotOption("yes", yes, "primary"), new BotOption("no", no) },
                asKeyboard: false,
                card: card);
            return value == "yes";
        }

        // ---------------------------------------------------------------- کمکی

        private static bool TryParseNumber(string raw, out long value)
        {
            var digits = new string(BotText.ToLatinDigits(raw).Where(char.IsAsciiDigit).ToArray());
            value = 0;
            return digits.Length is > 0 and <= 18 && long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParsePersianDate(string raw, out DateOnly date)
        {
            date = default;
            var parts = BotText.ToLatinDigits(raw).Split(new[] { '/', '-', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3
                || !int.TryParse(parts[0], out var y)
                || !int.TryParse(parts[1], out var m)
                || !int.TryParse(parts[2], out var d))
                return false;

            if (y < 100) y += 1400;
            try
            {
                var pc = new PersianCalendar();
                date = DateOnly.FromDateTime(pc.ToDateTime(y, m, d, 0, 0, 0, 0));
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
