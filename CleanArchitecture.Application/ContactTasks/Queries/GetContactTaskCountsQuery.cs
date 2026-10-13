using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks.Queries
{
    /// <summary>تعداد کارهای باز به ازای هر مخاطب، برای نشان دادن در ستون چت‌ها.</summary>
    public sealed record GetContactTaskCountsQuery : IQuery<List<ContactTaskCountDto>>;

    internal sealed class GetContactTaskCountsQueryHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager)
        : IQueryHandler<GetContactTaskCountsQuery, List<ContactTaskCountDto>>
    {
        public async Task<OperationResult<List<ContactTaskCountDto>>> Handle(
            GetContactTaskCountsQuery request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;
            var today = DateTime.Today;

            var counts = await uow.ContactTasks.AsNoTracking()
                .Where(t => t.OwnerId == me && !t.IsDone)
                .GroupBy(t => t.ContactId)
                .Select(g => new ContactTaskCountDto
                {
                    ContactId = g.Key,
                    OpenCount = g.Count(),
                    OverdueCount = g.Count(t => t.DueDate != null && t.DueDate < today),
                })
                .ToListAsync(cancellationToken);

            return OperationResult.Success(counts, counts.Count);
        }
    }
}
