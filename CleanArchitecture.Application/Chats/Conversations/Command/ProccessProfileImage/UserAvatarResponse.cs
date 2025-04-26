using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.ProccessProfileImage
{
    public sealed class UserAvatarResponse
    {
        public string? MyAvatar { get; set; }
        public string? OtherAvatar { get; set; }
    }
}
