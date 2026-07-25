using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.ChangeGroupBio
{
    public sealed class ChangeGroupBioCommandHandler(IApplicationUnitOfWork uow) : ICommandHandler<ChangeGroupBioCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(ChangeGroupBioCommand request, CancellationToken cancellationToken)
        {
            Domain.Entities.Chat.Conversation? result = await _uow.Conversation.Where(x => x.Id == request.GroupId)
                .FirstOrDefaultAsync(cancellationToken);
            result.ChangeGroupBio(request.bio);
            await _uow.SaveChangesAsync(cancellationToken);
            return new OperationResult().succedded();
        }
    }
}
