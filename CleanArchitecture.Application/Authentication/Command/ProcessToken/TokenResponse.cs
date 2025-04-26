using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.ProcessToken
{
    public record TokenResponse(string token, string refreshtoken);
}
