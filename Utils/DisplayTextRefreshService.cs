using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Settings;

namespace STS2PinyinEverything.Utils
{
    internal static class DisplayTextRefreshService
    {
        private static int _refreshPending;

        public static void SetValue<T>(T currentValue, T newValue, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return;
            }

            setter(newValue);
            RequestRefresh();
        }

        private static void RequestRefresh()
        {
            if (Interlocked.CompareExchange(ref _refreshPending, 1, 0) != 0)
            {
                return;
            }

            try
            {
                Callable.From(RefreshCurrentLanguage).CallDeferred();
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _refreshPending, 0);
                Main.Logger.Error($"Failed to schedule display text refresh: {ex}");
            }
        }

        private static void RefreshCurrentLanguage()
        {
            try
            {
                var locManager = LocManager.Instance;
                if (locManager is null || string.IsNullOrWhiteSpace(locManager.Language))
                {
                    return;
                }

                locManager.SetLanguage(locManager.Language);
                var game = NGame.Instance;
                var reopenModSettings = game?.MainMenu is not null;
                game?.Relocalize();
                if (reopenModSettings)
                {
                    game?.MainMenu?.OpenSettingsMenu();
                    ModSettingsNavigator.RequestOpenByIds(Const.ModId, null, null, null);
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to refresh display text: {ex}");
            }
            finally
            {
                Volatile.Write(ref _refreshPending, 0);
            }
        }
    }
}
