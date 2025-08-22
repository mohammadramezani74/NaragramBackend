using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.UnitofWork
{
    public partial class ApplicationUnitOfWork(ApplicationDbContext applicationDbContext)
     : IApplicationUnitOfWork
    {
        private readonly ApplicationDbContext _context = applicationDbContext;



        public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
           

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException e)
            {
                return Result.Failure(e.Message);
            }
            catch (DbUpdateException e)
            {
                return Result.Failure(e.Message);
            }
        }
       
        public async Task<int> ExecuteDeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>()
                .Where(predicate)
                .ExecuteDeleteAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
