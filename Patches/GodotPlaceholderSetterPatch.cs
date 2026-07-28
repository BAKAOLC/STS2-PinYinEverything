using Godot;
using HarmonyLib;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class GodotPlaceholderSetterPatch : IPatchMethod
    {
        public static string PatchId => "godot_placeholder_properties_to_pinyin";
        public static bool IsCritical => false;
        public static string Description => "Convert displayed input placeholder text without changing user input";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new ModPatchTarget(typeof(LineEdit), nameof(LineEdit.PlaceholderText), null, true, MethodType.Setter),
                new ModPatchTarget(typeof(TextEdit), nameof(TextEdit.PlaceholderText), null, true, MethodType.Setter)
            ];
        }

        public static void Prefix(ref string value)
        {
            value = PinyinTextTransformer.Transform(value);
        }
    }
}
