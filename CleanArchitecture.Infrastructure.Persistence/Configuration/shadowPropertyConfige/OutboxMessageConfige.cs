using CleanArchitecture.Domain.Common;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.shadowPropertyConfige;

public  static  class OutboxMessageConfige
{
    public static void AddOutboxMessageConfige(this ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id); 
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Occured).IsRequired();
           
        });
    }
}