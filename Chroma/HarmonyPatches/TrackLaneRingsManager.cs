using HarmonyLib;
using CustomJSONData;
using Chroma.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chroma.HarmonyPatches
{
    [HarmonyPatch(typeof(TrackLaneRingsManager))]
    [HarmonyPatch("Awake")]
    class TrackLaneRingsManagerAwake
    {
        private static void Prefix(ref int ____ringCount, ref float ____ringPositionStep)
        {
            dynamic ringData = StandardLevelScenesTransitionSetupDataSOInit._trackLaneRingModifiers;

            if (ringData != null && ChromaConfig.Instance.EnvironmentEnhancementsEnabled)
            {
                if (Trees.at(ringData, "_ringCount") is long ringCount) ____ringCount = (int)ringCount;

                if (Trees.at(ringData, "_ringPositionStep") is double ringPositionStepDbl) ____ringPositionStep = (float)ringPositionStepDbl;
                else if (Trees.at(ringData, "_ringPositionStep") is long ringPositionStepLng) ____ringPositionStep = ringPositionStepLng;
            }
        }
    }
}
