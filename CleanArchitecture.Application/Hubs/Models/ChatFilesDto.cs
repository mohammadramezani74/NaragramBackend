namespace CleanArchitecture.Application.Hubs.Models
{
    public class ChatFilesDto
    {
        public Guid FileId { get; set; }
        public  string? FileName { get; set; }
        public string? FileSize { get; set; }
    }
    
}
