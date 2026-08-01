namespace CleanArchitecture.Application.Chats.Messages.Query.PinnedMessages
{
    public sealed record PinnedMessageDto(
          Guid Id,
          string? Content,
          string SenderName,
          DateTime SendAt,
          DateTime? PinnedAt,
          int Type,
          string? FileName);
}
