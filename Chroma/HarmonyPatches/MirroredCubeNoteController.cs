namespace Chroma.HarmonyPatches
{
    using Chroma.Colorizer;
    using HarmonyLib;

    [HarmonyPatch(typeof(MirroredCubeNoteController))]
    [HarmonyPatch("Mirror")]
    internal static class MirroredCubeNoteControllerMirror
    {
        [HarmonyPriority(Priority.Low)]
        private static void Prefix(ICubeNoteMirrorable noteController)
        {
            if (noteController is NoteController noteControllerBase)
            {
                NoteColorizer.EnableNoteColorOverride(noteControllerBase);
            }
        }

        private static void Postfix()
        {
            NoteColorizer.DisableNoteColorOverride();
        }
    }
}
