using CleanArchitecture.Application.Abstraction.Files;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Files
{
    internal sealed class ChatFileStreamer(ApplicationDbContext context) : IChatFileStreamer
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ChatFileMeta?> GetMetaAsync(
            Guid fileId, Guid userId, CancellationToken ct = default)
        {
            // DATALENGTH طول را از متادیتای ستون می‌گیرد، بدون خواندن محتوا.
            // شرط عضویت هم اینجاست — نسخه‌ی قبلی هیچ بررسی دسترسی نداشت و
            // هر کاربری با داشتن FileId فایل هر گفتگویی را می‌گرفت.
            var row = await _context.Set<Domain.Entities.Chat.ChatFiles>()
                .AsNoTracking()
                .Where(f => f.Id == fileId
                         && (f.Message.Conversation.Users.Any(u => u.UserId == userId)
                          || f.Message.Channel!.Members.Any(m => m.UserId == userId)))
                .Select(f => new
                {
                    f.FileName,
                    f.Extension,
                    Length = (long)EF.Functions.DataLength(f.FileData)!,
                    Type = f.Message.MessageType
                })
                .FirstOrDefaultAsync(ct);

            if (row is null) return null;

            return new ChatFileMeta(
                fileId,
                (row.FileName ?? "file").Trim() + row.Extension,
                ResolveContentType(row.Extension),
                row.Length);
        }

        public async Task StreamToAsync(Guid fileId, Stream output, CancellationToken ct = default)
        {
            var connection = _context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT FileData FROM ChatFiles WHERE Id = @id";

            var p = command.CreateParameter();
            p.ParameterName = "@id";
            p.Value = fileId;
            command.Parameters.Add(p);

            // SequentialAccess کلید کار است: بدون آن، ADO.NET کل ردیف را
            // در حافظه می‌چیند و همان مشکل قبلی برمی‌گردد.
            await using var reader = await command.ExecuteReaderAsync(
                System.Data.CommandBehavior.SequentialAccess, ct);

            if (!await reader.ReadAsync(ct)) return;
            if (await reader.IsDBNullAsync(0, ct)) return;

            await using var source = reader.GetStream(0);
            await source.CopyToAsync(output, 81920, ct);   // بافر ثابت ۸۰ کیلوبایت
        }

        private static string ResolveContentType(string? extension) =>
            (extension ?? string.Empty).ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                // هر چیز دیگر octet-stream می‌ماند تا فایل‌هایی مثل html یا svg
                // در مرورگر اجرا نشوند
                _ => "application/octet-stream"
            };
    }
}
