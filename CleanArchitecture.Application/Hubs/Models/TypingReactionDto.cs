namespace CleanArchitecture.Application.Hubs.Models
{
    public record TypingReactionDto( Guid UserId, Guid MyUserId, int MessageType);

    public record BlockDto(Guid UserId,bool IsBlocked);
}
