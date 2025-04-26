using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            var context = eventData.Context;
            if (context != null)
            {
                SetAuditProperties(context);
            }

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
         DbContextEventData eventData,
         InterceptionResult<int> result,
         CancellationToken cancellationToken = default)
       {
            var context = eventData.Context;

            if (context != null)
            {
                SetAuditProperties(context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void SetAuditProperties(DbContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
            var userIp = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var now = DateTimeOffset.UtcNow;

            var modifiedEntries = context.ChangeTracker.Entries<IAuditableEntity>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modifiedEntries)
            {
                entry.Property(AuditableShadowProperties.ModifiedByBrowserName).CurrentValue = userAgent;
                entry.Property(AuditableShadowProperties.ModifiedByIp).CurrentValue = userIp;
                //entry.Property(AuditableShadowProperties.ModifiedDateTime).CurrentValue = now;
            }

            var addedEntries = context.ChangeTracker.Entries<IAuditableEntity>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in addedEntries)
            {
                entry.Property(AuditableShadowProperties.CreatedByBrowserName).CurrentValue = userAgent;
                entry.Property(AuditableShadowProperties.CreatedByIp).CurrentValue = userIp;
                //entry.Property(AuditableShadowProperties.CreatedDateTime).CurrentValue = now;
            }
        }
    }

}
