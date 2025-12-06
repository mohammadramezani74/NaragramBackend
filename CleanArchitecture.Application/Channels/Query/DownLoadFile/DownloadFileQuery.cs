using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.DownLoadFile
{
    public sealed record DownloadFileQuery(Guid FileId):IQuery<string>;
}
