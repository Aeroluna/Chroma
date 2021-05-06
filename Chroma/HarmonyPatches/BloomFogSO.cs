namespace Chroma.HarmonyPatches
{
    [ChromaPatch(typeof(BloomFogSO))]
    [ChromaPatch("Setup")]
    internal static class BloomFogSOSetupPatch
    {
        private static void Prefix(BloomFogSO __instance)
        {
            ChromaFogManager.BloomFogSO = __instance;
        }
    }
}
