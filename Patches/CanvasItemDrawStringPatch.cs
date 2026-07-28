using Godot;
using HarmonyLib;
using STS2PinyinEverything.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2PinyinEverything.Patches
{
    public sealed class CanvasItemDrawStringPatch : IPatchMethod
    {
        public static string PatchId => "canvas_draw_string_to_pinyin";
        public static bool IsCritical => false;
        public static string Description => "Convert custom-drawn CanvasItem strings to pinyin";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new ModPatchTarget(typeof(CanvasItem), nameof(CanvasItem.DrawString),
                [
                    typeof(Font),
                    typeof(Vector2),
                    typeof(string),
                    typeof(HorizontalAlignment),
                    typeof(float),
                    typeof(int),
                    typeof(Color?),
                    typeof(TextServer.JustificationFlag),
                    typeof(TextServer.Direction),
                    typeof(TextServer.Orientation)
                ]),
                new ModPatchTarget(typeof(CanvasItem), nameof(CanvasItem.DrawString),
                [
                    typeof(Font),
                    typeof(Vector2),
                    typeof(string),
                    typeof(HorizontalAlignment),
                    typeof(float),
                    typeof(int),
                    typeof(Color?),
                    typeof(TextServer.JustificationFlag),
                    typeof(TextServer.Direction),
                    typeof(TextServer.Orientation),
                    typeof(float)
                ])
            ];
        }

        [HarmonyBefore(Const.ExclaimEverythingPatcherId)]
        public static void Prefix(ref string text)
        {
            text = PinyinTextTransformer.Transform(text);
        }
    }
}
