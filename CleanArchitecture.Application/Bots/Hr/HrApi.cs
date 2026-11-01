using System.Globalization;
using System.Text.Json;
using CleanArchitecture.Application.Bots.Flows;

namespace CleanArchitecture.Application.Bots.Hr
{
    // ======================================================================
    // مدل‌ها و فراخوانی‌های ماژول ناراگرام INTS. هر چیزی که به شکل JSON INTS بستگی
    // دارد همین‌جاست؛ اگر INTS عوض شد فقط این فایل عوض می‌شود.
    // این مدل‌ها داخل CallAsync جریان ذخیره می‌شوند، پس باید JSON‌پذیر بمانند.
    // ======================================================================

    /// <summary>نتیجه‌ی یک فراخوانی؛ اگر Ok نباشد Message آماده‌ی نمایش به کاربر است.</summary>
    public sealed record HrResult<T>(bool Ok, string? Message, T? Value)
    {
        public static HrResult<T> Success(T value) => new(true, null, value);
        public static HrResult<T> Fail(string message) => new(false, message, default);
    }

    /// <param name="Group">۱ پرداخت، ۲ کسور، ۳ کارکرد، ۴ سایر اطلاعات.</param>
    /// <param name="IsTotal">ردیف جمع (کدهای ۱٫۱، ۱٫۲، ۲٫۱).</param>
    public sealed record PayslipLine(int Group, int Sub, string Title, string Amount);

    public sealed record Payslip(string? Title, List<PayslipLine> Lines);

    public sealed record HrProfile(
        string? Education,
        string? ContractStart,
        string? Insurance,
        string? Military,
        string? MilitaryExpire,
        string? MaritalStatus,
        string? Mobile,
        string? Email);

    /// <param name="Events">تاریخچه‌ی تأیید/رد، قدیمی اول.</param>
    public sealed record HrRequest(
        Guid Id,
        string DocNo,
        string Type,
        string? Status,
        string? Date,
        string? Organization,
        string? Beneficiary,
        decimal? Price,
        bool? HasCheckbook,
        List<string> Events)
    {
        /// <summary>هنوز هیچ تأییدکننده‌ای رویش کار نکرده؛ INTS فقط همین را اجازه‌ی حذف می‌دهد.</summary>
        public bool CanDelete => Events.Count == 0 && (Status is null || Status.Contains("در انتظار تایید منابع انسانی"));
    }

    /// <summary>درخواست تازه، همان فیلدهای CreatePersonnelRequestCommand در INTS.</summary>
    /// <param name="Type">PersonnelRequestsTypes: ۱ کسر از حقوق، ۲ اشتغال به کار، ۳ مساعده، ۴ فیش حقوقی، ۵ حکم کارگزینی.</param>
    /// <param name="PriceRial">INTS ریال می‌گیرد؛ ربات از کاربر تومان می‌پرسد.</param>
    public sealed record HrNewRequest(
        int Type,
        string? RecipientOrganization = null,
        string? Beneficiary = null,
        string? BranchName = null,
        string? BranchCode = null,
        string? BranchPhone = null,
        double? PriceRial = null,
        bool? HasCheckbook = null,
        int? Year = null,
        int? Month = null);

    public static class HrApi
    {
        public static async Task<HrResult<Payslip>> GetPayslipAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "GetCurrentPaymentSlip", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<Payslip>.Fail(r.Message);

            // دو شکل: نسخه‌ی تازه { year, month, title, items: [...] }، نسخه‌ی قدیم فقط آرایه.
            string? title = null;
            JsonElement items = default;
            if (r.Data is { ValueKind: JsonValueKind.Object } obj)
            {
                title = Str(obj, "title");
                if (obj.TryGetProperty("items", out var it)) items = it;
            }
            else if (r.Data is { ValueKind: JsonValueKind.Array } arr)
            {
                items = arr;
            }

            var lines = new List<PayslipLine>();
            if (items.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in items.EnumerateArray())
                {
                    var code = e.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : 0m;
                    var group = (int)Math.Floor(code);
                    var sub = (int)Math.Round((code - group) * 10);
                    var label = HrText.Clean(Str(e, "title"));
                    if (string.IsNullOrEmpty(label)) continue;
                    lines.Add(new PayslipLine(group, sub, label, HrText.Clean(Str(e, "amount"))));
                }
            }
            return HrResult<Payslip>.Success(new Payslip(title, lines));
        }

        /// <summary>
        /// آخرین فیش صادرشده و فیش ماه قبلش (GetCurrentAndBeforePaymentSlip). INTS ردیف‌ها را صاف و
        /// با year/month برمی‌گرداند؛ اینجا به دو فیش، جدیدتر اول، گروه می‌شوند.
        /// </summary>
        public static async Task<HrResult<List<DatedPayslip>>> GetTwoPayslipsAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "GetCurrentAndBeforePaymentSlip", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<List<DatedPayslip>>.Fail(r.Message);

            var rows = new List<(int Year, int Month, PayslipLine Line)>();
            if (r.Data is { ValueKind: JsonValueKind.Array } arr)
            {
                foreach (var e in arr.EnumerateArray())
                {
                    if (!int.TryParse(Str(e, "year"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                        || !int.TryParse(Str(e, "month"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)
                        || year <= 0 || month is < 1 or > 12)
                        continue;

                    var code = e.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : 0m;
                    var group = (int)Math.Floor(code);
                    var sub = (int)Math.Round((code - group) * 10);
                    var label = HrText.Clean(Str(e, "title"));
                    if (string.IsNullOrEmpty(label)) continue;
                    rows.Add((year, month, new PayslipLine(group, sub, label, HrText.Clean(Str(e, "amount")) ?? "")));
                }
            }

            var slips = rows
                .GroupBy(x => (x.Year, x.Month))
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .Select(g => new DatedPayslip(g.Key.Year, g.Key.Month, new Payslip(null, g.Select(x => x.Line).ToList())))
                .ToList();
            return HrResult<List<DatedPayslip>>.Success(slips);
        }

        public static async Task<HrResult<HrProfile>> GetProfileAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "GetPersonnelProfile", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<HrProfile>.Fail(r.Message);
            if (r.Data is not { ValueKind: JsonValueKind.Object } p)
                return HrResult<HrProfile>.Fail("پرونده‌ی پرسنلی شما در سامانه پیدا نشد.");

            var identity = Obj(p, "personnelIdentityInfo");
            var edu = Obj(p, "personnelEducation");
            var job = Obj(p, "personnelJobSummary");
            var ins = Obj(p, "personnelInsuranceSummary");
            var contact = Obj(p, "personnelContactSummary");

            var education = string.Join(" ", new[]
            {
                HrText.Clean(Str(edu, "educationName")),
                HrText.Clean(Str(edu, "courseName")),
            }.Where(s => !string.IsNullOrEmpty(s)));
            var field = HrText.Clean(Str(edu, "fieldname"));
            if (!string.IsNullOrEmpty(field) && !education.Contains(field)) education = string.IsNullOrEmpty(education) ? field : $"{education}، گرایش {field}";

            return HrResult<HrProfile>.Success(new HrProfile(
                Education: NullIfEmpty(education),
                ContractStart: NullIfEmpty(HrText.Clean(Str(job, "contractStartDate"))),
                Insurance: NullIfEmpty(HrText.Clean(Str(ins, "insuranceTypeName"))),
                Military: NullIfEmpty(HrText.Clean(Str(identity, "military"))),
                MilitaryExpire: NullIfEmpty(HrText.Clean(Str(identity, "educationPardonExpireDate"))),
                MaritalStatus: NullIfEmpty(HrText.Clean(Str(identity, "maritalStatusName"))),
                Mobile: NullIfEmpty(HrText.Clean(Str(contact, "mobileNo"))),
                Email: NullIfEmpty(Str(contact, "email")?.Trim())));
        }

        public static async Task<HrResult<List<HrRequest>>> GetRequestsAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "GetOwnedRequests", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<List<HrRequest>>.Fail(r.Message);

            // OperationResult INTS: { isSucceded, message, result: [...] }
            if (r.Data is not { ValueKind: JsonValueKind.Object } obj)
                return HrResult<List<HrRequest>>.Fail("جواب سامانه‌ی پرسنلی قابل خواندن نبود.");
            if (obj.TryGetProperty("isSucceded", out var okEl) && okEl.ValueKind == JsonValueKind.False)
                return HrResult<List<HrRequest>>.Fail(HrText.Clean(Str(obj, "message")) ?? "درخواست‌ها دریافت نشد.");

            var list = new List<HrRequest>();
            if (obj.TryGetProperty("result", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in arr.EnumerateArray())
                {
                    if (!Guid.TryParse(Str(e, "id"), out var id)) continue;
                    decimal? price = e.TryGetProperty("price", out var pr) && pr.ValueKind == JsonValueKind.Number ? pr.GetDecimal() : null;
                    bool? checkbook = e.TryGetProperty("hasCheckbook", out var hc) && hc.ValueKind is JsonValueKind.True or JsonValueKind.False ? hc.GetBoolean() : null;
                    list.Add(new HrRequest(
                        id,
                        HrText.Clean(Str(e, "dockNo")) ?? "",
                        HrText.Clean(Str(e, "personnelRequestsType")) ?? "درخواست",
                        NullIfEmpty(HrText.Clean(Str(e, "requestStatus"))),
                        NullIfEmpty(HrText.Clean(Str(e, "requestDate"))),
                        NullIfEmpty(HrText.Clean(Str(e, "recipientOrganization"))),
                        NullIfEmpty(HrText.Clean(Str(e, "beneficiaryName"))),
                        price,
                        checkbook,
                        HrText.Events(Str(e, "approversComment"))));
                }
            }
            return HrResult<List<HrRequest>>.Success(list);
        }

        /// <summary>
        /// ثبت درخواست پرسنلی. بدنه همان CreatePersonnelRequestCommand در INTS است؛ مبلغ‌ها ریال.
        /// INTS در جواب فقط موفق/ناموفق می‌دهد (شماره‌ی سند نه).
        /// </summary>
        public static async Task<HrResult<bool>> SubmitRequestAsync(IIntsConnector ints, string? phone, HrNewRequest request, CancellationToken ct)
        {
            var body = new Dictionary<string, object?>
            {
                ["requestType"] = request.Type,
                ["recipientOrganization"] = request.RecipientOrganization,
                ["beneficiary"] = request.Beneficiary,
                ["branchName"] = request.BranchName,
                ["branchCode"] = request.BranchCode,
                ["branchPhone"] = request.BranchPhone,
                ["price"] = request.PriceRial,
                ["hasCheckbook"] = request.HasCheckbook,
                ["year"] = request.Year,
                ["month"] = request.Month,
            };
            var r = await ints.SendAsync(HttpMethod.Post, "SubmitRequests", phone ?? "", body, ct);
            if (!r.Ok) return HrResult<bool>.Fail(r.Message);
            if (r.Data is { ValueKind: JsonValueKind.Object } obj
                && obj.TryGetProperty("isSucceded", out var okEl) && okEl.ValueKind == JsonValueKind.False)
                return HrResult<bool>.Fail(HrText.Clean(Str(obj, "message")) ?? "درخواست ثبت نشد.");
            return HrResult<bool>.Success(true);
        }

        public static async Task<HrResult<bool>> DeleteRequestAsync(IIntsConnector ints, string? phone, Guid id, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Delete, $"{id}/DeleteRequest", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<bool>.Fail(r.Message);
            if (r.Data is { ValueKind: JsonValueKind.Object } obj
                && obj.TryGetProperty("isSucceded", out var okEl) && okEl.ValueKind == JsonValueKind.False)
                return HrResult<bool>.Fail(HrText.Clean(Str(obj, "message")) ?? "حذف انجام نشد.");
            return HrResult<bool>.Success(true);
        }

        // ------------------------------------------------------------ JSON

        private static JsonElement Obj(JsonElement e, string name)
            => e.ValueKind == JsonValueKind.Object && TryGet(e, name, out var v) && v.ValueKind == JsonValueKind.Object ? v : default;

        internal static string? Str(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !TryGet(e, name, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            };
        }

        // INTS نام‌ها را camelCase می‌فرستد، ولی اگر تنظیمش عوض شد هم کار کند
        private static bool TryGet(JsonElement e, string name, out JsonElement value)
        {
            if (e.TryGetProperty(name, out value)) return true;
            foreach (var p in e.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
            value = default;
            return false;
        }

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }

    public static class HrText
    {
        /// <summary>ي و ك عربی ← فارسی، فاصله‌های اضافه حذف.</summary>
        public static string? Clean(string? s)
        {
            if (s is null) return null;
            s = s.Replace('ي', 'ی').Replace('ك', 'ک').Replace('ى', 'ی').Replace('‏', ' ').Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }

        /// <summary>
        /// «توضیحات تأییدکننده» INTS: هر رویداد یک خط «در تاریخ … توسط … تایید شد.( اعلام نظر : …)».
        /// رویدادهای تکراری (که INTS گاهی داخل هم می‌گذارد) یک بار شمرده می‌شوند.
        /// </summary>
        public static List<string> Events(string? comment)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(comment)) return result;
            foreach (var raw in comment.Split('\n'))
            {
                var line = Clean(raw)!.TrimEnd(')').Trim();
                if (line.Length == 0 || line.StartsWith('<')) continue;
                line = line.Replace(".( اعلام نظر :", " — نظر:").Replace("( اعلام نظر :", " — نظر:");
                if (line.EndsWith("— نظر:")) line = line[..^"— نظر:".Length].TrimEnd();
                if (!result.Contains(line)) result.Add(line);
            }
            return result;
        }

        /// <summary>مبلغ فیش (ریال): «259,955,037» ← «۲۵٬۹۹۵٬۵۰۴ تومان»؛ «005:50 ساعت» ← «۵:۵۰ ساعت».</summary>
        public static string PayslipValue(PayslipLine line)
        {
            var v = line.Amount ?? "";
            var digitsOnly = v.Replace(",", "");
            var isNumber = digitsOnly.Length > 0 && digitsOnly.All(char.IsAsciiDigit);

            // شماره‌ها (حساب، بیمه) مبلغ نیستند
            if (line.Title.Contains("شماره"))
                return isNumber ? BotText.ToPersianDigits(Mask(digitsOnly, line.Title.Contains("حساب"))) : BotText.ToPersianDigits(v);

            if (isNumber && line.Group != 3 && long.TryParse(digitsOnly, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                return BotText.Money(ToToman(n));

            // ساعت: 005:50 ← 5:50
            var parts = v.Split(' ', 2);
            if (parts[0].Contains(':'))
            {
                var hm = parts[0].Split(':');
                if (hm.Length == 2 && int.TryParse(hm[0], out var h))
                    v = $"{h}:{hm[1]}" + (parts.Length > 1 ? " " + parts[1] : "");
            }
            return BotText.ToPersianDigits(v);
        }

        /// <summary>شماره‌ی حساب در پیام فقط با چهار رقم آخر.</summary>
        private static string Mask(string digits, bool mask)
            => !mask || digits.Length <= 4 ? digits : "منتهی به " + digits[^4..];

        /// <summary>INTS مبلغ‌ها را ریال می‌دهد؛ به کاربر همه‌جا تومان نشان داده می‌شود.</summary>
        public static long ToToman(decimal rial) => (long)Math.Round(rial / 10m, MidpointRounding.AwayFromZero);

        public static string Money(decimal rial) => BotText.Money(ToToman(rial));
    }
}
