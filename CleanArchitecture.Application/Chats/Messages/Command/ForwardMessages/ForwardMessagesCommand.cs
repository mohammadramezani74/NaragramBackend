using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;

namespace CleanArchitecture.Application.Chats.Messages.Command.ForwardMessages
{
    /// <summary>
    /// مقصد فوروارد.
    ///
    /// معنی Id به Type بستگی دارد و عمداً همان چیزی است که لیست کناری فرانت
    /// می‌شناسد:
    ///   Private → شناسه‌ی کاربر مقابل (نه شناسه‌ی گفتگو؛ اگر گفتگویی بینشان
    ///             نباشد ساخته می‌شود)
    ///   group   → شناسه‌ی گفتگوی گروه
    ///   Channel → شناسه‌ی کانال
    /// </summary>
    public sealed record ForwardTarget(Guid Id, ConversationTyped Type);

    /// <summary>
    /// فوروارد یک یا چند پیام به یک یا چند مقصد.
    /// خروجی، تعداد پیام‌های ساخته‌شده است.
    /// </summary>
    public sealed record ForwardMessagesCommand(
        List<Guid> MessageIds,
        List<ForwardTarget> Targets) : ICommands<int>;
}
