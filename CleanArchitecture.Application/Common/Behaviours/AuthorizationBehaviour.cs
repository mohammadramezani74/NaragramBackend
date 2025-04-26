using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Behaviours
{
    public sealed class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IApplicationUserManager _usermanager;
        private readonly IApplicationRoleManager _roleManager;

        public AuthorizationBehaviour(IApplicationUserManager usermanager, IApplicationRoleManager roleManager)
        {
            _usermanager = usermanager;
            _roleManager = roleManager;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
          
            var authorizeAttributes = request?.GetType().GetCustomAttributes<AuthorizeAttribute>();
            if (authorizeAttributes.Any())
            {

                var userId = _usermanager.UserId;
                if (!userId.HasValue)
                    throw new UnauthorizedAccessException();


                //Role Base
                var authorizeAttributesWithRoles = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles)| !string.IsNullOrWhiteSpace(a.Claims));
                if (authorizeAttributesWithRoles.Any(x=>x.Roles!=null|x.Claims!=null))
                {
                    var UserRoles = await _roleManager.GetRolesByUserId(userId!.Value);
                    if (authorizeAttributesWithRoles.Any(x=>x.Roles!=null))
                    {
                        foreach (var roles in authorizeAttributesWithRoles.Select(a => a.Roles.Split(',')))
                        {
                            var authorized = false;
                            foreach (var role in roles)
                            {
                                var isInRole = UserRoles.Select(x => x.Name.Trim()).ToArray().Any(x => x == role);
                                if (isInRole)
                                {
                                    authorized = true;
                                    break;
                                }
                            }


                            if (!authorized)
                            {
                                throw new UnauthorizedAccessException();
                            }
                        }
                    }

                    var authorizeAttributesWithClaims = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Claims));
                    if (authorizeAttributesWithRoles.Any(x=>x.Claims !=null))
                    {
                        var Userclaims = await _usermanager.GetAllCalimsByUserId(userId!.Value);
                        foreach (var RequiredClaims in authorizeAttributesWithRoles.Select(a => a.Claims))
                        {
                            var authorized = false;

                            var isInRole = Userclaims.Any(x => x == RequiredClaims.Trim());
                            if (isInRole)
                            {
                                authorized = true;
                                break;
                            }



                            if (!authorized)
                            {
                                throw new UnauthorizedAccessException();
                            }
                        }
                    }
                }
            }
            return await next();
        }
    }
}
