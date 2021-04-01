namespace Chroma.HarmonyPatches
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;

    [HarmonyPatch(
        typeof(StandardLevelScenesTransitionSetupDataSO),
        new Type[] { typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool) })]
    [HarmonyPatch("Init")]
    internal static class StandardLevelScenesTransitionSetupDataSOInit
    {
        internal static dynamic _trackLaneRingModifiers;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SceneTransitionHelper.Transpiler(instructions);
        }

        private static void Prefix(IDifficultyBeatmap difficultyBeatmap, ref OverrideEnvironmentSettings overrideEnvironmentSettings)
        {
            SceneTransitionHelper.Patch(difficultyBeatmap, ref overrideEnvironmentSettings);

            if (difficultyBeatmap.beatmapData is CustomBeatmapData customBeatmapData)
            {
                _trackLaneRingModifiers = Trees.at(customBeatmapData.customData, "_trackLaneRingModifiers");
            }
        }
    }
}
