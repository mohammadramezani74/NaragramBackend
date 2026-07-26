using CleanArchitecture.Application.Abstraction.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Storage
{
    internal sealed class ChatFileStorage : IChatFileStorage
    {
        private readonly string _root;

        public ChatFileStorage(IConfiguration configuration)
        {
            // appsettings.json:  "ChatFileStorage": { "Root": "D:\\NaraChatFiles" }
            _root = configuration["ChatFileStorage:Root"]
                    ?? Path.Combine(AppContext.BaseDirectory, "ChatFiles");

            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName);
            if (extension.Length > 10) extension = string.Empty;

            var now = DateTime.UtcNow;

            // ساختار yyyy/MM از پر شدن یک پوشه با میلیون‌ها فایل جلوگیری می‌کند
            var relativePath = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // IFormFile برای فایل‌های بزرگ‌تر از ۶۴ کیلوبایت روی دیسک بافر می‌شود،
            // پس OpenReadStream از دیسک می‌خواند نه از حافظه.
            await using var source = file.OpenReadStream();
            await using var target = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            // بافر ثابت ۸۰ کیلوبایت — مصرف حافظه مستقل از حجم فایل است
            await source.CopyToAsync(target, 81920, cancellationToken);

            return relativePath;
        }

        public string? ResolvePath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            // جلوگیری از path traversal
            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                         .TrimStart(Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_root, normalized));

            if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
                return null;

            return File.Exists(fullPath) ? fullPath : null;
        }

        public void Delete(string? relativePath)
        {
            var fullPath = ResolvePath(relativePath);
            if (fullPath is not null) File.Delete(fullPath);
        }
    }
}
