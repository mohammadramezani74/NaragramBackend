using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public class ChannelInvite : BaseEntity
    {
        public Guid ChannelId { get; set; }
        public Channel Channel { get; set; }

        public string Token { get; private set; } = Guid.NewGuid().ToString("N");
        public bool IsActive { get; private set; } = true;

        public DateTime? ExpireAt { get; private set; }
        public int? MaxUseCount { get; private set; }
        public int UsedCount { get; private set; }

        private ChannelInvite() { }

        public static ChannelInvite Create(Guid channelId,Guid UserId ,DateTime? expireAt, int? maxUseCount)
        {
            if (maxUseCount is <= 0) throw new DomainException("حداکثر دفعات استفاده نامعتبر است.");
            return new ChannelInvite
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                ExpireAt = expireAt,
                MaxUseCount = maxUseCount,
                UsedCount = 0,
                IsActive = true,
                CreateDate = DateTime.Now,
                CreatedByUserId=UserId
            };
        }

        public bool IsExpired(DateTime now) => now >= ExpireAt || UsedCount >= MaxUseCount;

        public bool CanBeUsed(DateTime nowUtc)
        {
            if (!IsActive) return false;
            if (IsExpired(nowUtc)) return false;
            if (MaxUseCount.HasValue && UsedCount >= MaxUseCount.Value) return false;
            return true;
        }

        public void MarkUsed(DateTime nowUtc)
        {
            if (!CanBeUsed(nowUtc)) throw new DomainException("این دعوت قابل استفاده نیست.");
            UsedCount++;
            if (MaxUseCount.HasValue && UsedCount >= MaxUseCount.Value)
                IsActive = false; 
        }

        public void Deactivate() => IsActive = false;
    
    }
}
