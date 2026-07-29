using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Query.MessagesAround
{
    /// <summary>
    /// پیام هدف را به همراه Take پیام قبل و Take پیام بعدش برمی‌گرداند،
    /// تا کاربر بتواند نتیجه‌ی جستجو را در بستر خودش ببیند.
    /// </summary>
    public sealed record MessagesAroundQuery(
        Guid MessageId,
        int Take = 20) : IQuery<MessageResponse[]>;
}
