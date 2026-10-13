using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.ContactTasks
{
    /// <summary>
    /// بررسی‌ها و اعلان‌های مشترک بین هندلرهای کارها.
    /// </summary>
    internal static class ContactTaskAccess
    {
        /// <summary>
        /// مخاطب باید یک کاربر واقعی و غیر از خودِ کاربر باشد.
        /// null یعنی مشکلی نیست؛ در غیر این صورت نتیجه‌ی خطا برگردانده می‌شود.
        /// </summary>
        public static async Task<OperationResult?> ValidateContactAsync(
            IApplicationUnitOfWork uow, Guid me, Guid contactId, CancellationToken ct)
        {
            if (contactId == Guid.Empty)
                return new OperationResult().BadRequest("مخاطب مشخص نشده است.");

            if (contactId == me)
                return new OperationResult().BadRequest("برای خودتان نمی‌توانید فهرست کار بسازید.");

            var exists = await uow.Users.AsNoTracking().AnyAsync(u => u.Id == contactId, ct);
            return exists ? null : new OperationResult().NotFound("مخاطب یافت نشد.");
        }

        /// <summary>
        /// به همه‌ی دستگاه‌های خودِ کاربر خبر می‌دهد که فهرستش عوض شده.
        /// خطای ارسال، عملیات اصلی را خراب نمی‌کند.
        /// </summary>
        public static async Task NotifyOwnerAsync(
            IHubContext<NaraHub, IChatHubClient> hub, Guid me, Guid contactId)
        {
            try
            {
                await hub.Clients.User(me.ToString()).ContactTasksChanged(contactId);
            }
            catch (Exception) { }
        }
    }
}
