using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class MegaTextAutoSizePatch : IPatchMethod
    {
        public static string PatchId => "mega_text_auto_size_to_pinyin";
        public static string Description => "Convert dynamically assigned MegaText content to pinyin";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new ModPatchTarget(typeof(MegaLabel), nameof(MegaLabel.SetTextAutoSize), [typeof(string)]),
                new ModPatchTarget(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel.SetTextAutoSize),
                    [typeof(string)])
            ];
        }

        [HarmonyBefore(Const.ExclaimEverythingPatcherId)]
        public static void Prefix(ref string text)
        {
            text = PinyinTextTransformer.Transform(text);
        }
    }
}
