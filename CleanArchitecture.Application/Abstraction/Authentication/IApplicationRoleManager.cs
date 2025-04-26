using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Roles.Queries.GetRoles;
using CleanArchitecture.Application.Roles.Queries.GetUserRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Authentication
{
    public interface IApplicationRoleManager
    {
        Task<OperationResult> CreateRole(string Name,CancellationToken cancellationToken=default(CancellationToken));
        Task<OperationResult> UpdateRole(string name, string newName, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> DeleteRole(string Name, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> CreateRoleClaimsAsync(Guid RoleId,List<string> Claims, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> DeleteRoleClaimsAsync(Guid roleId, string claimName, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<GetRolesResponse>> GetRoles(GetRolesQuery search,CancellationToken cancellationToken = default(CancellationToken));
        Task<UserRolesResponse[]> GetRolesByUserId(Guid UserId,CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> AddUserToRole(Guid RoleId,Guid UserId, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<string>> GetClaims(Guid RoleId, CancellationToken cancellationToken = default(CancellationToken));

    }
}
