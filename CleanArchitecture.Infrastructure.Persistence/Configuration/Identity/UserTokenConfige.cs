using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Identity
{
    public class RefreshTokenConfige : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(t => t.CreatedByIp)
                 .HasMaxLength(150);
            builder.Property(t => t.RevokedByIp)
           .HasMaxLength(150);
            builder.Property(t => t.DeviceInfo)
      .HasMaxLength(250);

        }
    }
}
