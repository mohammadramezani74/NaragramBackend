// مسیر: CleanArchitecture.Application/Common/Utilities/Text/PersianText.cs

namespace CleanArchitecture.Application.Common.Utilities.Text
{
    public static class PersianText
    {
        /// <summary>
        /// یکسان‌سازی حروف عربی و فارسی و حذف کاراکترهای نامرئی.
        /// کاربرها «ی» و «ك» را با هر دو کدپوینت تایپ می‌کنند و نیم‌فاصله هم
        /// باعث می‌شود «میرود» با «می‌رود» مطابقت نکند.
        /// </summary>
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input
                .Replace('\u064A', '\u06CC')   // ي عربی  → ی فارسی
                .Replace('\u0649', '\u06CC')   // ى الف مقصوره → ی
                .Replace('\u0643', '\u06A9')   // ك عربی  → ک فارسی
                .Replace("\u200C", "")          // نیم‌فاصله
                .Replace("\u200F", "")          // علامت راست‌به‌چپ
                .Replace("\u0640", "")          // کشیده (ـ)
                .Replace("\u064B", "").Replace("\u064C", "")  // اعراب
                .Replace("\u064D", "").Replace("\u064E", "")
                .Replace("\u064F", "").Replace("\u0650", "")
                .Replace("\u0651", "").Replace("\u0652", "")
                .Trim();
        }

        /// <summary>
        /// کاراکترهای ویژه‌ی LIKE را خنثی می‌کند تا کاربری که «%» تایپ می‌کند
        /// نتواند کل جدول را برگرداند.
        /// </summary>
        public static string EscapeLike(string input) =>
            input.Replace("\\", "\\\\")
                 .Replace("%", "\\%")
                 .Replace("_", "\\_")
                 .Replace("[", "\\[");
    }
}