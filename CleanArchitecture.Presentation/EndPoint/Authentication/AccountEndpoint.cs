
using CleanArchitecture.Application.Authentication.Command.CreateRefreshToken;
using CleanArchitecture.Application.Authentication.Command.GoogleAuth;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Authentication.Command.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Authentication;

public class AccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        //269183400
        //GoogleAuthCommand
        // ورود با نام کاربری و رمز فقط در محیط توسعه (برای Swagger محلی).
        // کاربرهایی که با شماره‌ی همراه ساخته شده‌اند همه یک رمز مشترک قدیمی دارند که در
        // مخزن عمومی هم هست؛ اگر این دو مسیر روی سرور باز باشند، هر کس شماره‌ی همکاری را
        // بداند با همان رمز وارد حسابش می‌شود. ورود واقعی ناراگرام فقط
        // users/register-or-login است (شماره + کد ملی، بررسی‌شده با INTS).
        var isDevelopment = app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment();
        if (isDevelopment)
        {
            app.MapPost("Login", async ([FromServices] ISender Mediatr,
                   [FromBody] ProccessTokenCommand command,
                    CancellationToken cancellationToken) =>
            {
      
                var result = await Mediatr.Send(command, cancellationToken);
                return Results.Ok(result);
            }).AllowAnonymous();

            app.MapPost("LoginSwagger", async ([FromServices] ISender Mediatr,
              [FromServices] IHttpContextAccessor Context,
           [FromQuery] string? UserName, [FromQuery] string? password,
              CancellationToken cancellationToken) =>
            {
           
                UserName = UserName ?? Context.HttpContext!.Request.Form[nameof(UserName)].ToString();
                password = password ?? Context.HttpContext!.Request.Form[nameof(password)].ToString();

                var result = await Mediatr.Send(new ProccessTokenCommand(UserName, password), cancellationToken);
                return Results.Ok(new
                {
                    access_token = result.result.token,
                    refresh_token = result.result.refreshtoken,

                });
            }).AllowAnonymous();
        }

        // ورود با گوگل حذف شد: ناراگرام از آن استفاده نمی‌کند و هر حساب گوگلی را عضو می‌کرد.

        // تازه کردن توکن درست وقتی صدا زده می‌شود که توکن دسترسی منقضی شده؛ پس نباید توکن
        // دسترسی بخواهد (احراز هویت با خود refresh token است). قبلاً این مسیر زیر RequireAuthorization
        // گروه بود و همیشه 401 می‌داد؛ برای همین کاربرها بعد از انقضای توکن دسترسی (۷ روز)
        // بیرون می‌افتادند، با اینکه refresh token تا ۱۴ روز معتبر بود.
        app.MapPost("RefreshToken", async ([FromServices] ISender Mediatr,
 [FromBody] CreateRefreshTokenCommand command, CancellationToken cancellationToken) =>
        {

            var result = await Mediatr.Send(command, cancellationToken);
            return result.IsSucceded
                ? Results.Ok(result)
                : Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
        }).AllowAnonymous();

        // خروج با توکن دسترسیِ منقضی‌شده هم باید refresh token را باطل کند.
        app.MapPost("RevokeToken", async ([FromServices] ISender Mediatr,
[FromBody] RevokeTokenCommand command, CancellationToken cancellationToken) =>
        {

            var result = await Mediatr.Send(command, cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
