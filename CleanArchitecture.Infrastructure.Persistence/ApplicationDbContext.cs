using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Infrastructure.Persistence.Configuration.shadowPropertyConfige;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence
{
    public partial class ApplicationDbContext
        (DbContextOptions<ApplicationDbContext> options) :IdentityDbContext<User,Role,Guid>(options)
    {



    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.AddOutboxMessageConfige();
            modelBuilder.AddAuditableShadowProperties();

        }
 
  


    }
}
