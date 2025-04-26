using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.ExportUser
{
    public sealed record ExportUserQuery(string? search):IQuery<ExportViewModel>;
  
}
