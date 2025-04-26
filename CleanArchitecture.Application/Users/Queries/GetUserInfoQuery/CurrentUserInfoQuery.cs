using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUserInfoQuery
{
    public sealed record CurrentUserInfoQuery(Guid? Id):IQuery<CurentUserQueryResponse>;
}
