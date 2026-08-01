using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public sealed record ChatFileMeta(
          Guid FileId,
          string DownloadName,
          string ContentType,
          long Length);
}
