using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUserInfoQuery
{
    public   sealed record CurentUserQueryResponse
        (
        Guid Id,
        string Name,
        string? Avatar,
        string bio,
        string Address,
        string phoneNumber,
        string Email


        );
}
