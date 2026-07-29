using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.DeleteChannel
{
    /// <summary>
    /// هم برای کانال و هم برای گروه استفاده می‌شود، چون گروه‌ها هم روی
    /// موجودیت Channel نشسته‌اند. تفاوت فقط در Conversation متناظر است
    /// که برای گروه وجود دارد و برای کانال نه.
    /// </summary>
    public sealed record DeleteChannelCommand(Guid ChannelId) : ICommands;
}
