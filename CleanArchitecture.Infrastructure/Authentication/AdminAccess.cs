using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Authentication;

internal sealed class AdminAccess(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<AdminOptions> options)
    : IAdminAccess
{
    public bool IsAdmin(Guid? userId)
        => userId is Guid id
           && id != Guid.Empty
           && options.CurrentValue.UserIds.Contains(id);

    public bool CurrentUserIsAdmin
        => IsAdmin(httpContextAccessor.HttpContext?.User.GetUserId());
}
