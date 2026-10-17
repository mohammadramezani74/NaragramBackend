using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities.Admin
{
    /// <summary>
    /// یک کار ادمین در پنل: چه کسی، چه وقت، روی چه چیزی، چه کرد. فقط اضافه می‌شود؛ ویرایش و حذف ندارد.
    /// </summary>
    public sealed class AdminAuditLog : BaseEntity
    {
        public const int MaxNameLength = 200;
        public const int MaxDetailsLength = 1000;

        private AdminAuditLog() { }

        public Guid ActorId { get; private set; }
        public string ActorName { get; private set; } = string.Empty;

        /// <summary>مثل user.disable، user.enable، user.sessions، user.phone، user.roles، room.archive، room.restore، room.remove-member، role.create</summary>
        public string Action { get; private set; } = string.Empty;

        /// <summary>user | group | channel | role</summary>
        public string TargetType { get; private set; } = string.Empty;
        public Guid? TargetId { get; private set; }
        public string TargetName { get; private set; } = string.Empty;
        public string? Details { get; private set; }

        public static AdminAuditLog Create(Guid actorId, string actorName, string action, string targetType, Guid? targetId, string targetName, string? details)
            => new()
            {
                ActorId = actorId,
                ActorName = Clip(actorName, MaxNameLength),
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = Clip(targetName, MaxNameLength),
                Details = string.IsNullOrWhiteSpace(details) ? null : Clip(details.Trim(), MaxDetailsLength),
                CreatedByUserId = actorId,
            };

        private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
    }
}
