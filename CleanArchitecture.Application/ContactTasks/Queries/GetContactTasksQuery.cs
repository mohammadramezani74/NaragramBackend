using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks.Queries
{
    public sealed record GetContactTasksQuery(Guid ContactId) : IQuery<ContactTaskListDto>;

    internal sealed class GetContactTasksQueryHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager)
        : IQueryHandler<GetContactTasksQuery, ContactTaskListDto>
    {
        public async Task<OperationResult<ContactTaskListDto>> Handle(
            GetContactTasksQuery request, CancellationToken cancellationToken)
        {
            var me = userManager.UserId!.Value;

            // فقط کارهای خودِ کاربر؛ شرط OwnerId در همه‌ی کوئری‌ها هست.
            var mine = uow.ContactTasks.AsNoTracking()
                .Where(t => t.OwnerId == me && t.ContactId == request.ContactId);

            var open = await mine
                .Where(t => !t.IsDone)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.CreateDate)
                .Select(ContactTaskDto.Projection)
                .ToListAsync(cancellationToken);

            var done = await mine
                .Where(t => t.IsDone)
                .OrderByDescending(t => t.DoneAt)
                .Take(ContactTaskRules.DoneListLimit)
                .Select(ContactTaskDto.Projection)
                .ToListAsync(cancellationToken);

            var doneCount = done.Count < ContactTaskRules.DoneListLimit
                ? done.Count
                : await mine.CountAsync(t => t.IsDone, cancellationToken);

            return OperationResult.Success(new ContactTaskListDto
            {
                ContactId = request.ContactId,
                Open = open,
                Done = done,
                DoneCount = doneCount,
            });
        }
    }
}
