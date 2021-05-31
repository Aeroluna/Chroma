namespace Chroma.HarmonyPatches
{
    using Chroma.Colorizer;
    using HarmonyLib;

    [HarmonyPatch(typeof(MirroredCubeNoteController))]
    [HarmonyPatch("Mirror")]
    internal static class MirroredCubeNoteControllerMirror
    {
        [HarmonyPriority(Priority.Low)]
        private static void Prefix(ICubeNoteMirrorable iCubeNoteMirrorable)
        {
            if (iCubeNoteMirrorable is NoteController noteController)
            {
                NoteColorizer.EnableNoteColorOverride(noteController);
            }
        }

        private static void Postfix()
        {
            NoteColorizer.DisableNoteColorOverride();
        }
    }
}
