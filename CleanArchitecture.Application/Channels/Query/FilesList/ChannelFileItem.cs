using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.FilesList
{
    public class ChannelFileItemResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileData { get; set; } = string.Empty;
    }
}
