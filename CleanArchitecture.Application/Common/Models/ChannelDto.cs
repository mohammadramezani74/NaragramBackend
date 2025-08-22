using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public class ChannelDto
    {
        public string Creator { get; set; }
        public Guid CreatorId { get; set; }
        public List<UserChannelDto> admins { get; set; }
        public bool CurrentUserAdmin { get; set; }
    }
}
