using System.Text.Json.Serialization;

namespace STS2PinyinEverything.Settings
{
    public enum PinyinToneNotation
    {
        ToneMarks,
        ToneNumbers
    }

    public sealed class PinyinSettings
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        [JsonPropertyName("show_tones")] public bool ShowTones { get; set; } = true;

        [JsonPropertyName("auto_spacing")] public bool AutoSpacing { get; set; } = true;

        [JsonPropertyName("tone_notation")]
        public PinyinToneNotation ToneNotation { get; set; } = PinyinToneNotation.ToneMarks;
    }
}
