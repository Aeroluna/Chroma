namespace Chroma.HarmonyPatches
{
    using System.Reflection;
    using Chroma.Colorizer;
    using HarmonyLib;
    using Heck;
    using UnityEngine;
    using static ChromaObjectDataManager;

    [HarmonyPatch(typeof(ColorNoteVisuals))]
    [HarmonyPatch("HandleNoteControllerDidInit")]
    internal static class ColorNoteVisualsHandleNoteControllerDidInitColorizerInit
    {
        [HarmonyPriority(Priority.High)]
        private static void Prefix(ColorNoteVisuals __instance, NoteController noteController)
        {
            NoteColorizer.CNVStart(__instance, noteController);
        }
    }

    [HarmonyPatch(typeof(ColorNoteVisuals))]
    [HarmonyPatch("HandleNoteControllerDidInit")]
    internal static class ColorNoteVisualsHandleNoteControllerDidInitColorizer
    {
        [HarmonyPriority(Priority.Low)]
        private static void Prefix(NoteController noteController)
        {
            NoteColorizer.EnableNoteColorOverride(noteController);
        }

        private static void Postfix()
        {
            NoteColorizer.DisableNoteColorOverride();
        }
    }

    [HeckPatch(typeof(ColorNoteVisuals))]
    [HeckPatch("HandleNoteControllerDidInit")]
    internal static class ColorNoteVisualsHandleNoteControllerDidInit
    {
        private static readonly FieldInfo followedNote = typeof(MirroredCubeNoteController).GetField("followedNote", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Prefix(NoteControllerBase noteController)
        {
            ChromaNoteData chromaData = TryGetObjectData<ChromaNoteData>(noteController.noteData);
            if (chromaData == null)
            {
                return;
            }

            NoteController affectedNoteController = null;

            switch (noteController)
            {
                case MirroredCubeNoteController mirroredCubeNoteController:
                {
                    ICubeNoteMirrorable mirrorable = (ICubeNoteMirrorable)followedNote.GetValue(mirroredCubeNoteController);
                    if (mirrorable is GameNoteController noteMirrorable)
                    {
                        affectedNoteController = noteMirrorable;
                    }

                    break;
                }

                case NoteController nc:
                    affectedNoteController = nc;
                    break;
            }

            if (noteController == null)
            {
                return;
            }

            Color? color = chromaData.Color;

            if (color.HasValue)
            {
                affectedNoteController.SetNoteColors(color.Value, color.Value);
            }
            else
            {
                affectedNoteController.Reset();
            }
        }
    }
}
