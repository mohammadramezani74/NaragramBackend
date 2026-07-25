using CleanArchitecture.Application.Channels.Query.FilesList;
using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.FilesList
{
    public sealed record  GroupFilesQuery(Guid ConversationId) : IQuery<IReadOnlyList<ChannelFileItemResponse>>;
}
