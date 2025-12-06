using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.ChannelMembers
{
    public class ChannelMemberViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsCreator { get; set; }
        public bool IsManual { get; set; }
    }
}
