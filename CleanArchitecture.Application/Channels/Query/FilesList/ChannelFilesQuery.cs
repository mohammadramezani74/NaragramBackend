using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.FilesList
{
    public sealed record ChannelFilesQuery(Guid ChannelId):IQuery<IReadOnlyList<ChannelFileItemResponse>>;

}
