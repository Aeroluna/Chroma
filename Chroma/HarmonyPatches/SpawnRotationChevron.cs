namespace Chroma.HarmonyPatches
{
    using UnityEngine;

    [ChromaPatch(typeof(SpawnRotationChevron))]
    [ChromaPatch("UpdateLights")]
    internal static class SpawnRotationChevronSetLightAmount
    {
        internal static bool UseChevronAlpha { get; set; } = false;

        private static bool Prefix(TubeBloomPrePassLight[] ____lights, Color ____color, float ____lightAmount)
        {
            if (!UseChevronAlpha)
            {
                return true;
            }

            foreach (TubeBloomPrePassLight light in ____lights)
            {
                light.color = ____color.ColorWithAlpha(____color.a * ____lightAmount);
            }

            return false;
        }
    }
}
