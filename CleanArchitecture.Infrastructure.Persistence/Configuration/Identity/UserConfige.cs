using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Identity
{
    public class UserConfige : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.FirsName)
                  .HasMaxLength(350)
                  .IsUnicode(true);

            builder.Property(u => u.LastName)
                   .HasMaxLength(350)
                  .IsUnicode(true);
            builder.Property(u => u.Avatar)
                  .HasMaxLength(500)
                  ;
            builder.Property(u => u.bio)
                .HasMaxLength(500)
                ;
            builder.Property(u => u.UserName)
                 .HasMaxLength(350)
                 .IsRequired();
            ;
            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(11)
                .IsUnicode(false);
            builder.Property(u => u.IsActive).IsRequired();

            builder.OwnsOne(b => b.Address, x =>
            {
                x.Property(x => x.Street).HasMaxLength(150);
                x.Property(x=> x.City).IsRequired().HasMaxLength(100);
                x.Property(x=>x.PostalCode).HasMaxLength(14);

            });
            builder.Property(x => x.Age).HasConversion<int>();
        }
    }
}
