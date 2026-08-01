using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Files
{
    public interface IChatFileStreamer
    {
        /// <summary>متادیتای فایل و بررسی دسترسی. بدون خواندن محتوا.</summary>
        Task<ChatFileMeta?> GetMetaAsync(Guid fileId, Guid userId, CancellationToken ct = default);

        /// <summary>محتوای فایل را تکه‌تکه در جریان خروجی می‌نویسد.</summary>
        Task StreamToAsync(Guid fileId, Stream output, CancellationToken ct = default);
    }
}
