using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Utilities.ConstChat
{
    public static class SupportedChatFilesFormat
    {
        // پسوندها بدون توجه به حروف بزرگ و کوچک مقایسه می‌شوند (فایل .MP3 هم صوت است).
        // .weba و .ogg و .m4a خروجی ضبط ویس مرورگرها هستند (کروم/اج، فایرفاکس، سافاری).
        private static readonly string[] Images = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private static readonly string[] Videos = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v" };
        private static readonly string[] Audios = { ".mp3", ".wav", ".aac", ".flac", ".ogg", ".oga", ".opus", ".m4a", ".weba" };
        private static readonly string[] Documents = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };

        private static string Norm(string? extension) => (extension ?? string.Empty).Trim().ToLowerInvariant();

        public static bool IsImage(string extension) => Images.Contains(Norm(extension));
        public static bool IsVideo(string extension) => Videos.Contains(Norm(extension));
        public static bool IsAudio(string extension) => Audios.Contains(Norm(extension));
        public static bool IsDocument(string extension) => Documents.Contains(Norm(extension));
    }
}
