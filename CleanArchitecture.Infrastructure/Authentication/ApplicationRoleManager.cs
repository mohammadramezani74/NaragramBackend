using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Roles.Queries.GetRoles;
using CleanArchitecture.Application.Roles.Queries.GetUserRoles;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Authentication
{
    internal class ApplicationRoleManager(RoleManager<Role> roleManager,
           UserManager<User> userManager) : IApplicationRoleManager
    {
        private readonly RoleManager<Role> _roleManager=roleManager;
        private readonly UserManager<User> _userManager=userManager;

        /// <summary>
        /// افزودن نقش به کاربر
        /// </summary>
        /// <param name="RoleId"></param>
        /// <param name="UserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> AddUserToRole(Guid RoleId, Guid UserId, CancellationToken cancellationToken = default)
        {
            var op=new OperationResult();
          var role=await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==RoleId,cancellationToken);
            if(role==null)
            {
                return op.NotFound("نقش مورد نظر شما یافت نشد");
            }
            var ExistedUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == UserId, cancellationToken);
   if(ExistedUser is null) { return op.NotFound("کاربر مورد نظر شما یافت نشد"); }
            var result = await _userManager.AddToRoleAsync(ExistedUser, role.Name!);
            if(result.Succeeded)
            {
                return op.succedded();
            }
            foreach (var error in result.Errors)
            {
                op.Failed(error.Description);
            }
            return op.Failed("عملیات با خطا مواجه شد");
        }
        /// <summary>
        /// افزودن نقش جدید
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> CreateRole(string Name, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var result=   await _roleManager.CreateAsync(new Role { Name = Name, ConcurrencyStamp = Guid.NewGuid().ToString() });
            if (result.Succeeded)
            {
                return op.succedded();
            }
            foreach (var error in result.Errors)
            {
                op.Failed(error.Description);
            }
            return op.Failed("عملیات با خطا مواجه شد");
        }
        /// <summary>
        /// افزودن ادعا به نقش جدید
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="claims"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> CreateRoleClaimsAsync(Guid roleId, List<string> claims, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            var role = await _roleManager.Roles.FirstOrDefaultAsync(c => c.Id == roleId, cancellationToken);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر شما یافت نشد");
            }

   
            if (claims == null || !claims.Any())
            {
                return op.Failed("لیست ادعا ها نباید خالی باشد");
            }

            foreach (var claimValue in claims)
            {
               
                var newClaim = new Claim("Permission", claimValue);

        
                var existingClaims = await _roleManager.GetClaimsAsync(role);
                if (existingClaims.Any(c => c.Type == "Permission" && c.Value == claimValue))
                {
                    continue; 
                }

          
                var result = await _roleManager.AddClaimAsync(role, newClaim);
                if (!result.Succeeded)
                {
                    return op.Failed("خطا در افزودن ادعا به نقش");
                }
            }

            return op.succedded("ادعاها با موفقیت به نقش افزوده شدند");
        }
        /// <summary>
        /// حذف نقش
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> DeleteRole(string name, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
 
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                
                return op.Failed("خطا در حذف نقش");
            }

            return op.succedded("نقش با موفقیت حذف شد");
        }
        /// <summary>
        /// حذف ادعای یک نقش
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="claimName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task<OperationResult> DeleteRoleClaimsAsync(Guid roleId, string claimName, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

          
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }

           
            var claims = await _roleManager.GetClaimsAsync(role);

           
            var claimToRemove = claims.FirstOrDefault(c => c.Type == "Permission" && c.Value == claimName);
            if (claimToRemove == null)
            {
                return op.NotFound("ادعای مورد نظر برای حذف یافت نشد");
            }

         
            var result = await _roleManager.RemoveClaimAsync(role, claimToRemove);
            if (!result.Succeeded)
            {
                return op.Failed("خطا در حذف ادعا از نقش");
            }

        
            return op.succedded("ادعای مورد نظر با موفقیت حذف شد");
        }

        /// <summary>
        /// دریافت ادعا های نقش
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<string>> GetClaims(Guid roleId, CancellationToken cancellationToken = default)
        {
            List<string> ClaimList = new();

            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role == null)
            {
                return ClaimList;
            }
            var claims= await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                ClaimList.Add(claim.Value);
            }
            return ClaimList;
        }

        /// <summary>
        /// لیست رول های سیستم
        /// </summary>
        /// <param name="search"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<GetRolesResponse>> GetRoles(GetRolesQuery search, CancellationToken cancellationToken = default(CancellationToken))
        {
         var roles=   _roleManager.Roles.AsNoTracking();
            if (search.search is not null)
                roles = roles.Where(x => x.Name!.ToLower().Contains(search.search!.ToLower()));
           var roleList= await roles.Select(x=>new GetRolesResponse(x.Id,x.Name!)
         ).ToListAsync(cancellationToken);
            return roleList;
        }
        /// <summary>
        /// نقش های کاربر
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<UserRolesResponse[]> GetRolesByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Array.Empty<UserRolesResponse>();
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            if (roleNames == null || !roleNames.Any())
            {
                return Array.Empty<UserRolesResponse>();
            }

            // لیست آیدی و نام نقش‌ها را برمی‌گردانیم
            var rolesWithIds = new List<UserRolesResponse>();

            foreach (var roleName in roleNames)
            {
                // پیدا کردن نقش با نام
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    rolesWithIds.Add(new UserRolesResponse(role.Id,roleName ));
                }
            }

            return rolesWithIds.ToArray();
        }
        /// <summary>
        /// ویرایش نقش
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult> UpdateRole(string name, string newName, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();

            // پیدا کردن نقش بر اساس نام
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return op.NotFound("نقش مورد نظر یافت نشد");
            }

            // به‌روزرسانی نام نقش
            role.Name = newName;

            // ذخیره تغییرات
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return op.Failed("خطا در به‌روزرسانی نقش");
            }

            // در صورت موفقیت
            return op.succedded("نقش با موفقیت به‌روزرسانی شد");
        }
    }
}
