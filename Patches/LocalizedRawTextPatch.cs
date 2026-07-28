using MegaCrit.Sts2.Core.Localization;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class LocalizedRawTextPatch : IPatchMethod
    {
        public static string PatchId => "localized_raw_text_to_pinyin";
        public static string Description => "Convert every localized string to pinyin at its source";

        public static ModPatchTarget[] GetTargets()
        {
            return [new ModPatchTarget(typeof(LocString), nameof(LocString.GetRawText), Type.EmptyTypes)];
        }

        public static void Postfix(ref string __result)
        {
            __result = PinyinTextTransformer.Transform(__result);
        }
    }
}
