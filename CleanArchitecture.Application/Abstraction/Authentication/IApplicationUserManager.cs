using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Authentication
{
    public interface IApplicationUserManager
    {
         Guid? UserId { get;  }
        Task<User?> GetUserBy(string username,string   password);
        Task<User?> GetUserBy(Guid  Id);
        bool ExistUserBy(Guid Id);
        Task<OperationResult> CreateUserAsync(CreateUserCommand createUser,CancellationToken cancellationToken=default(CancellationToken));
        Task<OperationResult<TokenResponse>> CreateOrLoginUserAsync(string  phoneNumber,string verifycode, CancellationToken cancellationToken = default);
        Task<OperationResult> SendValidateCode(string phoneNumber);
        Task<List<GetUserResponse>> GetUsers(GetUserQuery getUser,CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> CreateUserClaimsAsync(Guid UserId, List<string> claims, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<string>> GetUserClaims(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(string nationalCode, string phoneNumber, string password);
        Task<List<string>> GetAllCalimsByUserId(Guid UserId);

    }
}
