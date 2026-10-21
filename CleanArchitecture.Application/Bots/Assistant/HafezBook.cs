using System.Text.Json;

namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record HafezGhazal(int Number, List<string> Lines, string Interpretation);

    /// <summary>
    /// دیوان حافظ (۴۹۵ غزل با تعبیر فال) داخل خود برنامه، بدون اینترنت.
    /// داده: github.com/kavehbc/hafez (MIT).
    /// </summary>
    public static class HafezBook
    {
        private sealed record Raw(int I, List<string> P, string T);

        private static readonly Lazy<IReadOnlyList<HafezGhazal>> Ghazals = new(Load);

        public static int Count => Ghazals.Value.Count;

        public static HafezGhazal? Get(int number)
            => Ghazals.Value.FirstOrDefault(g => g.Number == number);

        public static int RandomNumber()
        {
            var all = Ghazals.Value;
            return all.Count == 0 ? 0 : all[Random.Shared.Next(all.Count)].Number;
        }

        /// <summary>متن غزل، هر بیت (دو مصراع) در یک بند.</summary>
        public static string Text(HafezGhazal ghazal)
        {
            var verses = new List<string>();
            for (var i = 0; i < ghazal.Lines.Count; i += 2)
                verses.Add(i + 1 < ghazal.Lines.Count ? $"{ghazal.Lines[i]}\n{ghazal.Lines[i + 1]}" : ghazal.Lines[i]);
            return string.Join("\n\n", verses);
        }

        private static IReadOnlyList<HafezGhazal> Load()
        {
            using var stream = typeof(HafezBook).Assembly.GetManifestResourceStream("Naragram.Hafez.json");
            if (stream is null) return Array.Empty<HafezGhazal>();
            var raw = JsonSerializer.Deserialize<List<Raw>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new List<Raw>();
            return raw.Select(r => new HafezGhazal(r.I, r.P, r.T)).ToList();
        }
    }
}
