using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.GetAvatar
{
    /// <param name="NoDefault">true: گروه بی‌عکس «پیدا نشد» می‌گیرد به‌جای group2.svg (فرانت آواتار خودش را می‌کشد).</param>
    public sealed record  GroupAvatarQuery(Guid FileId, bool NoDefault = false):IQuery<(byte[]? Bytes, string Name)>;

}
