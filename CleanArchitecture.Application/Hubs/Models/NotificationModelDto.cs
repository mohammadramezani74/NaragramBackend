using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Hubs.Models
{
    public sealed record NotificationModelDto
   (
        string Name,
        string Avatar,
        string Message,
        string Url
        );
}
