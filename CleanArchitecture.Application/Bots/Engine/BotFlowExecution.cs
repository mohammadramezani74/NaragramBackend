using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;
using System.Globalization;
using System.Text.Json;

namespace CleanArchitecture.Application.Bots.Engine
{
    /// <summary>وضعیت ذخیره‌شده‌ی یک جریان نیمه‌تمام (BotSession.FlowStateJson).</summary>
    internal sealed class BotFlowState
    {
        public List<BotFlowAnswer> Answers { get; set; } = new();
        public Dictionary<string, string> Calls { get; set; } = new();
        public Guid? PendingMessageId { get; set; }
        public List<List<string>>? PendingKeyboard { get; set; }
        public string? PendingPlaceholder { get; set; }
        public List<BotCardRow>? PendingCard { get; set; }
    }

    internal sealed record BotFlowAnswer(string Kind, string Value, string? Label = null);

    /// <summary>جوابی که این بار رسیده: متن تایپ‌شده یا دکمه‌ی شیشه‌ای.</summary>
    internal sealed record BotFlowInput(string? Text, string? CallbackData, Guid? MessageId);

    internal enum BotFlowOutcome
    {
        Completed,
        Waiting,
        Stale,
        ReplayMismatch,
        Failed,
    }

    internal sealed class BotQuestion
    {
        public required string Kind { get; init; }
        public required string Text { get; init; }
        public string? Placeholder { get; init; }
        public List<BotOption>? Options { get; init; }
        public bool InlineOptions { get; init; }
        public List<List<string>>? Keyboard { get; init; }
        public List<BotCardRow>? Card { get; init; }
        public required Func<string, BotParseResult> Parse { get; init; }
    }

    internal readonly record struct BotParseResult(bool Ok, string? Value, string? Label, string? Error)
    {
        public static BotParseResult Success(string value, string? label = null) => new(true, value, label, null);
        public static BotParseResult Fail(string error) => new(false, null, null, error);
    }

    /// <summary>
    /// یک بار اجرای جریان برای یک جواب تازه.
    ///
    /// روش کار («بازپخش»): جریان هر بار از اول اجرا می‌شود. سؤال‌هایی که قبلاً جواب
    /// گرفته‌اند فوراً جواب ذخیره‌شده را برمی‌گردانند و پیام‌هایی که قبلاً رفته‌اند
    /// دوباره فرستاده نمی‌شوند. به اولین سؤال بی‌جواب که برسد، جواب تازه را بررسی
    /// می‌کند؛ اگر معتبر بود ذخیره و ادامه می‌دهد تا سؤال بعدی را بپرسد، و همان‌جا
    /// می‌ایستد. «ایستادن» یعنی Task سؤال هیچ‌وقت کامل نمی‌شود و موتور با سیگنال
    /// _halt از آن جدا می‌شود؛ پس try/catch داخل جریان روی آن اثری ندارد.
    /// </summary>
    internal sealed class BotFlowExecution
    {
        public const string CancelLabel = "/انصراف";

        private readonly BotTurn _turn;
        private readonly BotMessenger _messenger;
        private readonly BotFlowInput? _input;
        private readonly bool _firstRun;
        private readonly TaskCompletionSource<BotFlowOutcome> _halt = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _askIndex;
        private bool _consumed;

        public BotFlowExecution(BotTurn turn, BotMessenger messenger, BotFlowState state, BotFlowInput? input, bool firstRun)
        {
            _turn = turn;
            _messenger = messenger;
            State = state;
            _input = input;
            _firstRun = firstRun;
        }

        public BotFlowState State { get; }

        public Exception? Error { get; private set; }

        public CancellationToken CancellationToken => _turn.CancellationToken;

        /// <summary>آیا خروجی‌ها الان واقعاً فرستاده شوند (و نه بازپخش چیزی که قبلاً رفته).</summary>
        private bool Live => _firstRun || _consumed;

        public async Task<BotFlowOutcome> RunAsync(IBotFlow flow, BotFlowContext context)
        {
            Task running;
            try
            {
                running = flow.RunAsync(context);
            }
            catch (Exception ex)
            {
                Error = ex;
                return BotFlowOutcome.Failed;
            }

            var finished = await Task.WhenAny(running, _halt.Task);
            if (finished == _halt.Task)
                return await _halt.Task;

            if (running.IsFaulted || running.IsCanceled)
            {
                Error = running.Exception?.GetBaseException() ?? new OperationCanceledException();
                return BotFlowOutcome.Failed;
            }

            return BotFlowOutcome.Completed;
        }

        public async Task SayAsync(string text, bool menuButton, List<BotCardRow>? card)
        {
            if (!Live) return;

            var markup = new BotReplyMarkup { Card = card };
            if (menuButton && _turn.Menu.Root is { } root)
                markup.Rows.Add(new List<BotButton> { new("بازگشت به منو", BotMenu.NodeData(root.Id), "back") });

            await _messenger.SendAsync(_turn, text, markup);
        }

        public async Task SendDocumentAsync(byte[] data, string fileName, string? caption)
        {
            if (!Live) return;
            await _messenger.SendDocumentAsync(_turn, data, fileName, caption);
        }

        public async Task<T> CallAsync<T>(string key, Func<CancellationToken, Task<T>> call)
        {
            if (State.Calls.TryGetValue(key, out var json))
                return JsonSerializer.Deserialize<T>(json, BotJson.Options)!;

            var result = await call(CancellationToken);
            State.Calls[key] = JsonSerializer.Serialize(result, BotJson.Options);
            return result;
        }

        public async Task<BotFlowAnswer> AskAsync(BotQuestion question)
        {
            var index = _askIndex++;

            // ۱. سؤالی که قبلاً جواب گرفته: بازپخش.
            if (index < State.Answers.Count)
            {
                var stored = State.Answers[index];
                if (!string.Equals(stored.Kind, question.Kind, StringComparison.Ordinal))
                    return await Halt<BotFlowAnswer>(BotFlowOutcome.ReplayMismatch);
                return stored;
            }

            // ۲. اولین سؤال بی‌جواب و یک جواب تازه رسیده: بررسی.
            if (_input is not null && !_consumed)
            {
                string raw;
                if (_input.CallbackData is string data)
                {
                    if (!TryParseAnswerData(data, out var askIndex, out var optionIndex)
                        || askIndex != index
                        || question.Options is null
                        || optionIndex < 0
                        || optionIndex >= question.Options.Count)
                        return await Halt<BotFlowAnswer>(BotFlowOutcome.Stale);

                    raw = question.Options[optionIndex].Label;
                }
                else
                {
                    raw = (_input.Text ?? string.Empty).Trim();
                }

                var parsed = question.Parse(raw);
                if (!parsed.Ok)
                {
                    await _messenger.SendAsync(_turn, parsed.Error ?? "این جواب معتبر نیست.");
                    return await Halt<BotFlowAnswer>(BotFlowOutcome.Waiting);
                }

                var answer = new BotFlowAnswer(question.Kind, parsed.Value!, parsed.Label);
                State.Answers.Add(answer);
                _consumed = true;

                // دکمه‌های سؤال برداشته می‌شوند و انتخاب زیرش می‌ماند.
                if (State.PendingMessageId is Guid pending && question.InlineOptions)
                {
                    await _messenger.EditAsync(_turn, pending, null, new BotReplyMarkup
                    {
                        Card = State.PendingCard,
                        Note = parsed.Label ?? parsed.Value,
                        NoteTone = "ok",
                    }, markEdited: false);
                }

                State.PendingMessageId = null;
                State.PendingKeyboard = null;
                State.PendingPlaceholder = null;
                State.PendingCard = null;
                return answer;
            }

            // ۳. سؤال تازه: بپرس و بایست.
            var markup = new BotReplyMarkup { Card = question.Card };
            if (question.InlineOptions && question.Options is { Count: > 0 })
            {
                markup.Rows = question.Options
                    .Select((o, i) => new BotButton(o.Label, $"a:{index}:{i}", o.Style))
                    .Chunk(2)
                    .Select(row => row.ToList())
                    .ToList();
            }

            var message = await _messenger.SendAsync(_turn, question.Text, markup);
            State.PendingMessageId = message.Id;
            State.PendingKeyboard = question.Keyboard;
            State.PendingPlaceholder = question.Placeholder;
            State.PendingCard = question.Card;
            return await Halt<BotFlowAnswer>(BotFlowOutcome.Waiting);
        }

        /// <summary>کیبورد پیشنهادها: دو دکمه در هر ردیف و «/انصراف» در ردیف آخر.</summary>
        public static List<List<string>>? SuggestionKeyboard(IEnumerable<string>? suggestions)
        {
            var rows = (suggestions ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Chunk(2)
                .Select(r => r.ToList())
                .ToList();

            rows.Add(new List<string> { CancelLabel });
            return rows;
        }

        private static bool TryParseAnswerData(string data, out int askIndex, out int optionIndex)
        {
            askIndex = optionIndex = -1;
            var parts = data.Split(':');
            return parts.Length == 3
                   && parts[0] == "a"
                   && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out askIndex)
                   && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out optionIndex);
        }

        private Task<T> Halt<T>(BotFlowOutcome outcome)
        {
            _halt.TrySetResult(outcome);
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        }
    }
}
