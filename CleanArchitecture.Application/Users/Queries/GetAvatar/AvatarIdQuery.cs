using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetAvatar
{
    /// <param name="noDefault">true: کاربری که عکس ندارد «پیدا نشد» می‌گیرد به‌جای avatar.jpg پیش‌فرض
    /// (فرانت جدید آواتار پیش‌فرض خودش را می‌کشد؛ کلاینت‌های قدیمی بدون این پارامتر مثل قبل).</param>
    public sealed record AvatarIdQuery(Guid fileId, bool isfromchannel, bool noDefault = false) : IQuery<(byte[]? Bytes, string Name)>;
}
