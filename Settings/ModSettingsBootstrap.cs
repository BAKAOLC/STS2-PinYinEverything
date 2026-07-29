using MegaCrit.Sts2.Core.Localization;
using STS2PinyinEverything.Data;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace STS2PinyinEverything.Settings
{
    internal static class ModSettingsBootstrap
    {
        private static readonly Lock InitLock = new();
        private static bool _initialized;

        public static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                {
                    return;
                }

                var enabledBinding = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<PinyinSettings, bool>(
                        Const.ModId,
                        ModDataStore.SettingsKey,
                        settings => settings.Enabled,
                        (settings, value) => settings.Enabled = value),
                    () => true);

                var showTonesBinding = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<PinyinSettings, bool>(
                        Const.ModId,
                        ModDataStore.SettingsKey,
                        settings => settings.ShowTones,
                        (settings, value) => settings.ShowTones = value),
                    () => true);

                var autoSpacingBinding = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<PinyinSettings, bool>(
                        Const.ModId,
                        ModDataStore.SettingsKey,
                        settings => settings.AutoSpacing,
                        (settings, value) => settings.AutoSpacing = value),
                    () => true);

                var toneNotationBinding = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<PinyinSettings, PinyinToneNotation>(
                        Const.ModId,
                        ModDataStore.SettingsKey,
                        settings => settings.ToneNotation,
                        (settings, value) => settings.ToneNotation = value),
                    () => PinyinToneNotation.ToneMarks);

                RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
                    .WithModDisplayName(T("Pinyin Everything", "全都拼音"))
                    .WithTitle(T("Settings", "设置"))
                    .WithDescription(T(
                        "Adjust automatic pinyin conversion. Already-created text may require reopening the screen before changes appear.",
                        "调整自动拼音转换。已经创建的文本可能需要重新打开界面后才会显示更改。"))
                    .AddSection("general", section => section
                        .WithTitle(T("General", "通用"))
                        .AddToggle(
                            "enabled",
                            T("Enable pinyin conversion", "启用拼音转换"),
                            enabledBinding,
                            T(
                                "When disabled, newly displayed text remains unchanged.",
                                "关闭后，新显示的文本将保持原样。"))
                        .AddToggle(
                            "auto_spacing",
                            T("Add spaces automatically", "自动添加空格"),
                            autoSpacingBinding,
                            T(
                                "Insert spaces between pinyin syllables and adjacent letters or numbers.",
                                "在拼音音节之间，以及拼音与相邻字母或数字之间插入空格。")))
                    .AddSection("tones", section => section
                        .WithTitle(T("Tones", "声调"))
                        .AddToggle(
                            "show_tones",
                            T("Show tones", "显示声调"),
                            showTonesBinding,
                            T(
                                "Add pronunciation tones to converted pinyin.",
                                "为转换后的拼音显示声调。"))
                        .AddEnumChoice(
                            "tone_notation",
                            T("Tone notation", "声调格式"),
                            toneNotationBinding,
                            ToneNotationLabel,
                            T(
                                "Choose tone marks such as xiǎo or tone numbers such as xiao3.",
                                "选择 xiǎo 这样的声调符号，或 xiao3 这样的数字声调。"))
                        .WithEntryEnabledWhen(
                            "tone_notation",
                            () => PinyinSettingsService.ShowTones)));

                _initialized = true;
            }
        }

        private static ModSettingsText ToneNotationLabel(PinyinToneNotation notation)
        {
            return notation switch
            {
                PinyinToneNotation.ToneNumbers => T("Tone numbers (xiao3)", "数字声调（xiao3）"),
                _ => T("Tone marks (xiǎo)", "声调符号（xiǎo）")
            };
        }

        private static ModSettingsText T(string english, string simplifiedChinese)
        {
            return ModSettingsText.Dynamic(() => IsSimplifiedChinese() ? simplifiedChinese : english);
        }

        private static bool IsSimplifiedChinese()
        {
            try
            {
                return string.Equals(LocManager.Instance?.Language, "zhs", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
