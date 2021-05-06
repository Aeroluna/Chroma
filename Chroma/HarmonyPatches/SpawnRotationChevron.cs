namespace Chroma.HarmonyPatches
{
    using UnityEngine;

    [ChromaPatch(typeof(SpawnRotationChevron))]
    [ChromaPatch("UpdateLights")]
    internal static class SpawnRotationChevronSetLightAmount
    {
        private static bool Prefix(TubeBloomPrePassLight[] ____lights, Color ____color, float ____lightAmount)
        {
            foreach (TubeBloomPrePassLight light in ____lights)
            {
                light.color = ____color.ColorWithAlpha(____color.a * ____lightAmount);
            }

            return false;
        }
    }
}
