using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class MegaTextReadyPatch : IPatchMethod
    {
        public static string PatchId => "scene_mega_text_to_pinyin";
        public static bool IsCritical => false;
        public static string Description => "Convert scene-authored MegaText content to pinyin after initialization";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new ModPatchTarget(typeof(MegaLabel), nameof(MegaLabel._Ready), Type.EmptyTypes),
                new ModPatchTarget(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel._Ready), Type.EmptyTypes)
            ];
        }

        [HarmonyBefore(Const.ExclaimEverythingPatcherId)]
        public static void Postfix(object __instance)
        {
            switch (__instance)
            {
                case MegaLabel label:
                    label.SetTextAutoSize(PinyinTextTransformer.Transform(label.Text));
                    break;
                case MegaRichTextLabel richTextLabel:
                    richTextLabel.SetTextAutoSize(PinyinTextTransformer.Transform(richTextLabel.Text));
                    break;
            }
        }
    }
}
