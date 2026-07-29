using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Localization;
using STS2PinyinEverything.Settings;

namespace STS2PinyinEverything.Utils
{
    internal static class PinyinTextTransformer
    {
        private static readonly ConcurrentDictionary<CacheKey, string> RunCache = [];

        public static void WarmUp()
        {
            _ = PinyinLexicon.Convert("中文", PinyinOutputStyle.Plain, true);
        }

        public static string Transform(string text)
        {
            if (string.IsNullOrEmpty(text) ||
                !PinyinSettingsService.Enabled ||
                !IsChineseLocale())
            {
                return text;
            }

            var outputStyle = PinyinSettingsService.OutputStyle;
            var autoSpacing = PinyinSettingsService.AutoSpacing;
            StringBuilder? builder = null;
            var copyStart = 0;

            for (var index = 0; index < text.Length;)
            {
                if (text[index] == '[' && TrySkipBbCode(text, index, out var nextIndex))
                {
                    index = nextIndex;
                    continue;
                }

                if (!IsHanAt(text, index, out var runeLength))
                {
                    index++;
                    continue;
                }

                var runStart = index;
                index += runeLength;
                while (index < text.Length && IsHanAt(text, index, out runeLength))
                {
                    index += runeLength;
                }

                var run = text[runStart..index];
                var converted = ConvertHanRun(run, outputStyle, autoSpacing);

                builder ??= new StringBuilder(text.Length + converted.Length);
                builder.Append(text, copyStart, runStart - copyStart);

                if (autoSpacing && NeedsLeadingSpace(text, runStart))
                {
                    builder.Append(' ');
                }

                builder.Append(converted);

                if (autoSpacing && NeedsTrailingSpace(text, index))
                {
                    builder.Append(' ');
                }

                copyStart = index;
            }

            if (builder == null)
            {
                return text;
            }

            builder.Append(text, copyStart, text.Length - copyStart);
            return builder.ToString();
        }

        private static bool IsChineseLocale()
        {
            return LocManager.Instance is { Language: "zhs" or "zht" };
        }

        private static string ConvertHanRun(string run, PinyinOutputStyle outputStyle, bool autoSpacing)
        {
            return RunCache.GetOrAdd(
                new CacheKey(run, outputStyle, autoSpacing),
                static key => PinyinLexicon.Convert(key.Text, key.OutputStyle, key.AutoSpacing));
        }

        private static bool NeedsLeadingSpace(string text, int runStart)
        {
            var index = FindPreviousVisibleIndex(text, runStart - 1);
            if (index < 0 ||
                char.IsWhiteSpace(text[index]) ||
                !IsWordBoundaryCharacter(text, index, true))
            {
                return false;
            }

            var followsBbCodeTag = runStart > 0 && text[runStart - 1] == ']';
            return !followsBbCodeTag || !IsHanAt(text, index, out _);
        }

        private static bool NeedsTrailingSpace(string text, int runEnd)
        {
            var index = FindNextVisibleIndex(text, runEnd);
            return index < text.Length &&
                   !char.IsWhiteSpace(text[index]) &&
                   IsWordBoundaryCharacter(text, index, false);
        }

        private static int FindPreviousVisibleIndex(string text, int index)
        {
            while (index >= 0 && text[index] == ']')
            {
                var openIndex = text.LastIndexOf('[', index);
                if (openIndex < 0)
                {
                    break;
                }

                index = openIndex - 1;
            }

            return index;
        }

        private static int FindNextVisibleIndex(string text, int index)
        {
            while (index < text.Length && text[index] == '[' &&
                   TrySkipBbCodeTag(text, index, out var nextIndex, out _))
            {
                index = nextIndex;
            }

            return index;
        }

        private static bool IsWordBoundaryCharacter(string text, int index, bool includeClosingBrace)
        {
            if (includeClosingBrace && text[index] == '}')
            {
                return true;
            }

            if (!includeClosingBrace && text[index] == '{')
            {
                return true;
            }

            if (IsHanAt(text, index, out _))
            {
                return true;
            }

            return char.IsLetterOrDigit(text[index]);
        }

        private static bool IsHanAt(string text, int index, out int runeLength)
        {
            var first = text[index];
            int value;

            if (char.IsHighSurrogate(first) &&
                index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]))
            {
                value = char.ConvertToUtf32(first, text[index + 1]);
                runeLength = 2;
            }
            else
            {
                value = first;
                runeLength = 1;
            }

            return value is >= 0x3400 and <= 0x4DBF or
                >= 0x4E00 and <= 0x9FFF or
                >= 0xF900 and <= 0xFAFF or
                >= 0x20000 and <= 0x2FA1F or
                >= 0x30000 and <= 0x323AF;
        }

        private static bool TrySkipBbCode(string text, int index, out int nextIndex)
        {
            if (!TrySkipBbCodeTag(text, index, out nextIndex, out var isImageOpen) || !isImageOpen)
            {
                return nextIndex > index;
            }

            var imageCloseIndex = text.IndexOf("[/img]", nextIndex, StringComparison.OrdinalIgnoreCase);
            if (imageCloseIndex < 0)
            {
                nextIndex = text.Length;
                return true;
            }

            nextIndex = imageCloseIndex + "[/img]".Length;
            return true;
        }

        private static bool TrySkipBbCodeTag(
            string text,
            int index,
            out int nextIndex,
            out bool isImageOpen)
        {
            var closeIndex = text.IndexOf(']', index + 1);
            if (closeIndex < 0)
            {
                nextIndex = index;
                isImageOpen = false;
                return false;
            }

            var tag = text.AsSpan(index + 1, closeIndex - index - 1).Trim();
            isImageOpen = tag.Equals("img".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                          tag.StartsWith("img=".AsSpan(), StringComparison.OrdinalIgnoreCase);
            nextIndex = closeIndex + 1;
            return true;
        }

        private readonly record struct CacheKey(
            string Text,
            PinyinOutputStyle OutputStyle,
            bool AutoSpacing);
    }
}
