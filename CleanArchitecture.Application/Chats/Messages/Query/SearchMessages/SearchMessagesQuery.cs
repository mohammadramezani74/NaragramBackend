using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Query.SearchMessages
{
    public sealed record SearchMessagesQuery(
           Guid? ConversationId,
           Guid? ChannelId,
           string Term,
           DateTime? Before = null,
           int Take = 20) : IQuery<SearchMessagesResponse>;

    public sealed record SearchHit(
        Guid Id,
        DateTime SendAt,
        string? Content,
        string SenderName,
        Guid SenderId,
        bool IsMine,
        int Type,
        string? FileName);

    public sealed record SearchMessagesResponse(
        SearchHit[] Items,
        DateTime? NextCursor,
        bool HasMore);
    }


