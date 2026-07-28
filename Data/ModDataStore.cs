using STS2PinyinEverything.Settings;
using STS2RitsuLib;
using STS2RitsuLib.Utils.Persistence;

namespace STS2PinyinEverything.Data
{
    internal static class ModDataStore
    {
        public const string SettingsKey = "settings";

        private const string SettingsFileName = "settings.json";

        private static readonly STS2RitsuLib.Data.ModDataStore Store =
            STS2RitsuLib.Data.ModDataStore.For(Const.ModId);

        public static void Initialize()
        {
            using (RitsuLibFramework.BeginModDataRegistration(Const.ModId))
            {
                Store.Register(
                    SettingsKey,
                    SettingsFileName,
                    SaveScope.Global,
                    () => new PinyinSettings(),
                    true);
            }
        }

        public static PinyinSettings GetSettings()
        {
            return Store.Get<PinyinSettings>(SettingsKey);
        }
    }
}
