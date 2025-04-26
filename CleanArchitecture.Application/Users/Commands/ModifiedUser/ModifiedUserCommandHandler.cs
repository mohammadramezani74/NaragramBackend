using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.ModifiedUser
{
    internal sealed class ModifiedUserCommandHandler(IApplicationUserManager userManager,IApplicationUnitOfWork uow) : ICommandHandler<ModifiedUserCommand>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(ModifiedUserCommand request, CancellationToken cancellationToken)
        {
          User? user= await _userManager.GetUserBy(_userManager.UserId!.Value);
            if ((user is null))
            {
                throw new ArgumentNullException("User is Null");
            }
            if (!string.IsNullOrWhiteSpace(request.phoneNumber))
            {
                user.PhoneNumber = request.phoneNumber;
            }
            if (!string.IsNullOrWhiteSpace(request.bio))
            {
                user.bio = request.bio;
            }
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user.Email = request.Email;
            }
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                user.SetAddress(request.City);
            }
            await _uow.SaveChangesAsync(cancellationToken);
            return new OperationResult().succedded();
        }
    }
}
