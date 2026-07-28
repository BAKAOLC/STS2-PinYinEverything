using Godot;
using HarmonyLib;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class GodotTextSetterPatch : IPatchMethod
    {
        public static string PatchId => "godot_text_properties_to_pinyin";
        public static bool IsCritical => false;
        public static string Description => "Convert generic Godot control text to pinyin";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new ModPatchTarget(typeof(Button), nameof(Button.Text), null, true, MethodType.Setter),
                new ModPatchTarget(typeof(Label), nameof(Label.Text), null, true, MethodType.Setter),
                new ModPatchTarget(typeof(RichTextLabel), nameof(RichTextLabel.Text), null, true, MethodType.Setter)
            ];
        }

        [HarmonyBefore(Const.ExclaimEverythingPatcherId)]
        public static void Prefix(ref string value)
        {
            value = PinyinTextTransformer.Transform(value);
        }
    }
}
