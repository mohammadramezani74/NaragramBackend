using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Bots
{
    /// <summary>
    /// چه کسی کدام ربات را می‌بیند. همه‌جا (لیست مخاطبین، شروع، موتور) از همین
    /// استفاده می‌شود تا محدودیت پنل در سرور هم یکسان اعمال شود.
    /// ادمین همه‌ی ربات‌ها را می‌بیند، حتی خاموش‌ها، تا قبل از روشن کردن امتحانشان کند.
    /// </summary>
    public static class BotVisibility
    {
        public static List<Guid> ParseIds(string? json)
            => BotJson.Deserialize<List<Guid>>(json) ?? new List<Guid>();

        public static async Task<bool> CanUseAsync(
            Bot bot, Guid userId, bool isAdmin, IApplicationRoleManager roles, CancellationToken ct)
        {
            if (isAdmin)
                return true;
            if (!bot.IsEnabled)
                return false;

            switch (bot.AccessMode)
            {
                case BotAccessMode.All:
                    return true;
                case BotAccessMode.Users:
                    return ParseIds(bot.AccessListJson).Contains(userId);
                case BotAccessMode.Roles:
                    var allowed = ParseIds(bot.AccessListJson);
                    if (allowed.Count == 0)
                        return false;
                    var mine = await roles.GetRolesByUserId(userId, ct);
                    return mine.Any(r => allowed.Contains(r.RoleId));
                default:
                    return false;
            }
        }

        /// <summary>شناسه‌ی کاربرِ ربات‌هایی که این کاربر می‌تواند ببیند.</summary>
        public static async Task<HashSet<Guid>> VisibleBotUserIdsAsync(
            IApplicationUnitOfWork uow, IApplicationRoleManager roles, Guid userId, bool isAdmin, CancellationToken ct)
        {
            var bots = await uow.Bots.AsNoTracking().ToListAsync(ct);
            var visible = new HashSet<Guid>();
            if (bots.Count == 0)
                return visible;

            // نقش‌ها یک بار خوانده می‌شوند، نه برای هر ربات.
            HashSet<Guid>? myRoles = null;
            foreach (var bot in bots)
            {
                if (isAdmin) { visible.Add(bot.UserId); continue; }
                if (!bot.IsEnabled) continue;

                var ok = bot.AccessMode switch
                {
                    BotAccessMode.All => true,
                    BotAccessMode.Users => ParseIds(bot.AccessListJson).Contains(userId),
                    BotAccessMode.Roles => (myRoles ??= (await roles.GetRolesByUserId(userId, ct)).Select(r => r.RoleId).ToHashSet())
                                               .Overlaps(ParseIds(bot.AccessListJson)),
                    _ => false,
                };
                if (ok) visible.Add(bot.UserId);
            }

            return visible;
        }
    }
}
