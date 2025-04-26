using CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.shadowPropertyConfige
{

    public static class AddAuditableShadowPropertiesConfige
    {

        public static void AddAuditableShadowProperties(this ModelBuilder modelBuilder)
        {

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && !e.ClrType.IsAbstract && !e.IsOwned()))
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
                modelBuilder.Entity(entityType.ClrType).Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);

                modelBuilder.Entity(entityType.ClrType).Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
                modelBuilder.Entity(entityType.ClrType).Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);
            }
        }
    }
}
