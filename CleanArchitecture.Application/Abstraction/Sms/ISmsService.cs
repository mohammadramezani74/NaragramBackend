using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Sms
{
    public interface ISmsService
    {
        Task SendVerificationCode(string phoneNumber, string code);
        Task SendMessageToUser(string phoneNumber, string message);

        /// <summary>پیامک قالب‌دار sms.ir (خط خدماتی؛ برای کد تأیید مناسب‌تر از ارسال عادی).</summary>
        Task SendTemplateAsync(string phoneNumber, int templateId, IReadOnlyDictionary<string, string> parameters);
    }
}
