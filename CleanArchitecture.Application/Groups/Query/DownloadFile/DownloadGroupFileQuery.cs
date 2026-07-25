using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.DownloadFile
{
    public sealed record DownloadGroupFileQuery(Guid FileId) : IQuery<string>;
}
