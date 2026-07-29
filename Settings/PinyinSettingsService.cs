using STS2PinyinEverything.Data;
using STS2PinyinEverything.Utils;

namespace STS2PinyinEverything.Settings
{
    internal static class PinyinSettingsService
    {
        public static bool Enabled => Read(static settings => settings.Enabled, true);

        public static bool ShowTones => Read(static settings => settings.ShowTones, true);

        public static bool AutoSpacing => Read(static settings => settings.AutoSpacing, true);

        public static PinyinToneNotation ToneNotation =>
            Read(static settings => settings.ToneNotation, PinyinToneNotation.ToneMarks);

        public static PinyinOutputStyle OutputStyle
        {
            get
            {
                if (!ShowTones)
                {
                    return PinyinOutputStyle.Plain;
                }

                return ToneNotation == PinyinToneNotation.ToneNumbers
                    ? PinyinOutputStyle.ToneNumbers
                    : PinyinOutputStyle.ToneMarks;
            }
        }

        private static TValue Read<TValue>(Func<PinyinSettings, TValue> selector, TValue fallback)
        {
            try
            {
                return selector(ModDataStore.GetSettings());
            }
            catch
            {
                return fallback;
            }
        }
    }
}
