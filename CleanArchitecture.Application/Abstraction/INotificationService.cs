using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction
{
    public interface INotificationService
    {
        Task Send(string token, string Message, string Name);
        Task StoreFCMtoken(string token);

        /// <summary>
        /// اعلان یادآوری کار. tag باعث می‌شود اعلان سیستم‌عامل با اعلانی که
        /// خود صفحه (اگر باز باشد) نشان می‌دهد یکی شود، نه دو تا.
        /// </summary>
        Task SendReminder(string token, string title, string body, string tag);

        /// <summary>
        /// یک اعلان برای چند توکن (دسته‌های ۵۰۰تایی Firebase). هیچ‌وقت استثنا
        /// بیرون نمی‌دهد، پس می‌شود بدون await صدایش زد.
        /// </summary>
        Task SendToMany(IReadOnlyCollection<string> tokens, string title, string body);
    }
}
