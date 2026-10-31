using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Application.Common.Models;
using System.Security.Claims;

namespace CleanArchitecture.Presentation.Extensions;

/// <summary>
/// دروازه‌ی مسیرهای پنل مدیریت، به این ترتیب:
///   ۱. فقط مالک‌ها (Admin:UserIds) و مدیرانی که مالک دسترسی داده؛ برای بقیه 404،
///      نه 403، تا وجود این مسیرها هم معلوم نشود.
///   ۲. تأیید دوم: سرآیند X-Admin-Session با نشستی که بعد از کد پیامکی ساخته شده.
///      نبودش 428 می‌دهد و پنل صفحه‌ی ورود با کد را نشان می‌دهد.
///   ۳. اگر دسترسی خاصی خواسته شده، حداقل یکی از آن‌ها؛ وگرنه 403.
/// </summary>
public sealed class AdminOnlyFilter(string[] anyOf, bool requireSession) : IEndpointFilter
{
    public const int VerifyStatus = StatusCodes.Status428PreconditionRequired;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var services = http.RequestServices;

        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var me) || me == Guid.Empty)
            return Results.NotFound();

        var access = services.GetRequiredService<IAdminAccess>();

        // فهرست مدیران را تازه کن (۳۰ ثانیه کش) تا حذف یا تغییر دسترسی زود اثر کند
        if (!access.IsOwner(me))
            await services.GetRequiredService<IAppSettingsStore>().AdminGrantsAsync(http.RequestAborted);

        if (!access.IsAdmin(me))
            return Results.NotFound();

        // راه اضطراری فقط از روی سرور: اگر پیامک از کار افتاد و مالک پشت در ماند،
        // «AdminAuth:RequireSms»: false در appsettings.Local.json تأیید دوم را خاموش می‌کند.
        var smsRequired = !string.Equals(services.GetRequiredService<IConfiguration>()["AdminAuth:RequireSms"], "false", StringComparison.OrdinalIgnoreCase);

        if (requireSession && smsRequired)
        {
            var sessions = services.GetRequiredService<IAdminSessionService>();
            var token = http.Request.Headers[IAdminSessionService.HeaderName].ToString();
            if (!sessions.Validate(token, me))
                return Results.Json(new OperationResult
                {
                    Status = VerifyStatus,
                    IsSucceded = false,
                    Message = "برای ورود به پنل مدیریت کد پیامکی لازم است.",
                }, statusCode: VerifyStatus);
        }

        if (anyOf.Length > 0 && !anyOf.Any(p => access.Has(me, p)))
            return Results.Json(new OperationResult().Forbiden("به این بخش پنل دسترسی ندارید."),
                statusCode: StatusCodes.Status403Forbidden);

        return await next(context);
    }
}

public static class AdminOnlyExtensions
{
    /// <summary>
    /// فقط مدیران با نشست تأییدشده. اگر دسترسی داده شود، داشتن حداقل یکی لازم است.
    /// روی گروه و مسیر با هم قابل استفاده است (هر دو باید رد شوند).
    /// </summary>
    public static TBuilder AdminOnly<TBuilder>(this TBuilder builder, params string[] anyOf) where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(new AdminOnlyFilter(anyOf, requireSession: true));

    /// <summary>فقط مدیران، بدون نیاز به نشست؛ برای خود مراحل ورود با کد.</summary>
    public static TBuilder AdminSignIn<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(new AdminOnlyFilter([], requireSession: false));
}
