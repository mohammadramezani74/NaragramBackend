using CleanArchitecture.Application.Abstraction.Authentication;

namespace CleanArchitecture.Presentation.Extensions;

/// <summary>
/// مسیرهای مدیریتی را فقط برای حساب‌های فهرست AdminOptions باز می‌کند.
/// به بقیه 404 برمی‌گرداند، نه 403، تا وجود این مسیرها هم معلوم نشود.
/// </summary>
public sealed class AdminOnlyFilter(IAdminAccess adminAccess) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!adminAccess.CurrentUserIsAdmin)
            return Results.NotFound();

        return await next(context);
    }
}

public static class AdminOnlyExtensions
{
    public static TBuilder AdminOnly<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter<TBuilder, AdminOnlyFilter>();
}
