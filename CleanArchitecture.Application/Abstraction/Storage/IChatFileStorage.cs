using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Storage
{
    public interface IChatFileStorage
    {
        /// <summary>
        /// فایل را به صورت استریم روی دیسک می‌نویسد و مسیر نسبی‌اش را برمی‌گرداند.
        /// هیچ‌وقت کل فایل را در حافظه لود نمی‌کند.
        /// </summary>
        Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>مسیر کامل روی دیسک را از مسیر نسبی می‌سازد. null اگر فایل موجود نباشد.</summary>
        string? ResolvePath(string? relativePath);

        void Delete(string? relativePath);
    }
}
