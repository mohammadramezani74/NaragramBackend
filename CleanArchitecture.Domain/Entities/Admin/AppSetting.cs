namespace CleanArchitecture.Domain.Entities.Admin
{
    /// <summary>یک تنظیم سامانه که از پنل ادمین عوض می‌شود (کلید ← JSON).</summary>
    public sealed class AppSetting
    {
        private AppSetting() { }

        public string Key { get; private set; } = string.Empty;
        public string ValueJson { get; private set; } = "{}";
        public DateTime UpdatedAt { get; private set; }
        public string UpdatedByName { get; private set; } = string.Empty;

        public static AppSetting Create(string key, string valueJson, string byName)
            => new() { Key = key, ValueJson = valueJson, UpdatedAt = DateTime.Now, UpdatedByName = byName };

        public void Update(string valueJson, string byName)
        {
            ValueJson = valueJson;
            UpdatedAt = DateTime.Now;
            UpdatedByName = byName;
        }
    }
}
