using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction
{
    public interface INotificationService
    {
        Task Send(string token, string Message, string Name);
        Task StoreFCMtoken(string token);
    }
}
