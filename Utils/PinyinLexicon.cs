using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace STS2PinyinEverything.Utils
{
    internal enum PinyinOutputStyle
    {
        Plain,
        ToneMarks,
        ToneNumbers
    }

    internal static class PinyinLexicon
    {
        private const string ResourceName = "STS2PinyinEverything.Resources.PinyinLexicon.tsv.br";

        private static readonly Lazy<LexiconData> Data = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string Convert(string hanText, PinyinOutputStyle outputStyle, bool autoSpacing)
        {
            var data = Data.Value;
            var builder = new StringBuilder(hanText.Length * 3);
            var index = 0;
            var hasOutput = false;

            while (index < hanText.Length)
            {
                if (TryMatchPhrase(data, hanText, index, out var phrase))
                {
                    AppendSyllables(phrase.Reading.GetValue(outputStyle));
                    index += phrase.Text.Length;
                    continue;
                }

                var codePoint = ReadCodePoint(hanText, index, out var length);
                if (data.CharacterReadings.TryGetValue(codePoint, out var reading))
                {
                    AppendSyllables(reading.GetValue(outputStyle));
                }
                else
                {
                    AppendSyllables(hanText.Substring(index, length));
                }

                index += length;
            }

            return builder.ToString();

            void AppendSyllables(string value)
            {
                if (autoSpacing && hasOutput)
                {
                    builder.Append(' ');
                }

                builder.Append(autoSpacing
                    ? value
                    : value.Replace(" ", string.Empty, StringComparison.Ordinal));
                hasOutput = true;
            }
        }

        private static bool TryMatchPhrase(
            LexiconData data,
            string text,
            int index,
            out PhraseReading phrase)
        {
            phrase = default;
            var first = ReadCodePoint(text, index, out var firstLength);
            var secondIndex = index + firstLength;
            if (secondIndex >= text.Length)
            {
                return false;
            }

            var second = ReadCodePoint(text, secondIndex, out _);
            if (!data.PhrasesByPrefix.TryGetValue(GetPrefixKey(first, second), out var candidates))
            {
                return false;
            }

            var remaining = text.AsSpan(index);
            foreach (var candidate in candidates)
            {
                if (remaining.StartsWith(candidate.Text.AsSpan(), StringComparison.Ordinal))
                {
                    phrase = candidate;
                    return true;
                }
            }

            return false;
        }

        private static LexiconData Load()
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                                 ?? throw new InvalidOperationException(
                                     $"Missing embedded pinyin resource: {ResourceName}");
            using var brotli = new BrotliStream(resource, CompressionMode.Decompress);
            using var reader = new StreamReader(brotli, Encoding.UTF8, true);

            var characters = new Dictionary<int, PinyinReading>();
            var phraseBuckets = new Dictionary<long, List<PhraseReading>>();

            while (reader.ReadLine() is { } line)
            {
                var columns = line.Split('\t', 5);
                if (columns.Length != 5)
                {
                    continue;
                }

                var reading = new PinyinReading(columns[2], columns[3], columns[4]);
                if (columns[0] == "C" &&
                    int.TryParse(columns[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                {
                    characters[codePoint] = reading;
                    continue;
                }

                if (columns[0] != "P" || !TryGetPrefixKey(columns[1], out var prefixKey))
                {
                    continue;
                }

                if (!phraseBuckets.TryGetValue(prefixKey, out var bucket))
                {
                    bucket = [];
                    phraseBuckets[prefixKey] = bucket;
                }

                bucket.Add(new PhraseReading(columns[1], reading));
            }

            foreach (var bucket in phraseBuckets.Values)
            {
                bucket.Sort(static (left, right) =>
                {
                    var lengthComparison = right.Text.Length.CompareTo(left.Text.Length);
                    return lengthComparison != 0
                        ? lengthComparison
                        : string.CompareOrdinal(left.Text, right.Text);
                });
            }

            return new LexiconData(characters, phraseBuckets);
        }

        private static bool TryGetPrefixKey(string text, out long key)
        {
            key = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var first = ReadCodePoint(text, 0, out var firstLength);
            if (firstLength >= text.Length)
            {
                return false;
            }

            var second = ReadCodePoint(text, firstLength, out _);
            key = GetPrefixKey(first, second);
            return true;
        }

        private static long GetPrefixKey(int first, int second)
        {
            return ((long)first << 21) | (uint)second;
        }

        private static int ReadCodePoint(string text, int index, out int length)
        {
            if (char.IsHighSurrogate(text[index]) &&
                index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]))
            {
                length = 2;
                return char.ConvertToUtf32(text[index], text[index + 1]);
            }

            length = 1;
            return text[index];
        }

        private sealed record LexiconData(
            Dictionary<int, PinyinReading> CharacterReadings,
            Dictionary<long, List<PhraseReading>> PhrasesByPrefix);

        private readonly record struct PhraseReading(string Text, PinyinReading Reading);

        private readonly record struct PinyinReading(string Plain, string ToneMarks, string ToneNumbers)
        {
            public string GetValue(PinyinOutputStyle outputStyle)
            {
                return outputStyle switch
                {
                    PinyinOutputStyle.ToneMarks => ToneMarks,
                    PinyinOutputStyle.ToneNumbers => ToneNumbers,
                    _ => Plain
                };
            }
        }
    }
}
