using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Utilities.ConstChat
{
    public static class SupportedChatFilesFormat
    {
        public static bool IsImage(string extension) => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(extension);
        public static bool IsVideo(string extension) => new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv" }.Contains(extension);
        public static bool IsAudio(string extension) => new[] { ".mp3", ".wav", ".aac", ".flac" }.Contains(extension);
        public static bool IsDocument(string extension) => new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" }.Contains(extension);
    }
}
