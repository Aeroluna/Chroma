﻿using Chroma.Events;
using Chroma.Extensions;
using Chroma.Settings;
using Chroma.Utils;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chroma.HarmonyPatches
{
    [HarmonyPatch(typeof(LightSwitchEventEffect))]
    [HarmonyPatch("Start")]
    internal class LightSwitchEventEffectStart
    {
        private static void Postfix(LightSwitchEventEffect __instance, ref BeatmapEventType ____event)
        {
            if (ChromaBehaviour.LightingRegistered || ChromaBehaviour.LegacyOverride) __instance.StartCoroutine(WaitThenStart(__instance, ____event));
        }

        private static IEnumerator WaitThenStart(LightSwitchEventEffect __instance, BeatmapEventType ____event)
        {
            yield return new WaitForEndOfFrame();
            LightSwitchEventEffectExtensions.LSEStart(__instance, ____event);
        }
    }

    [HarmonyPatch(typeof(LightSwitchEventEffect))]
    [HarmonyPatch("OnDestroy")]
    internal class LightSwitchEventEffectOnDestroy
    {
        private static void Postfix(LightSwitchEventEffect __instance, ref BeatmapEventType ____event)
        {
            LightSwitchEventEffectExtensions.LSEDestroy(__instance, ____event);
        }
    }

    [HarmonyPatch(typeof(LightSwitchEventEffect))]
    [HarmonyPatch("SetColor")]
    internal class LightSwitchEventEffectSetColor
    {
        private static bool Prefix(LightSwitchEventEffect __instance, ref Color color)
        {
            if (LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger.overrideLightWithIdActivation != null)
            {
                LightWithId[] lights = LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger.overrideLightWithIdActivation;
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].ColorWasSet(color);
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(LightSwitchEventEffect))]
    [HarmonyPatch("HandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger")]
    internal class LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger
    {
        internal static void ResetRandom()
        {
            ChromaLogger.Log("Resetting techniLightRandom Random 408 (Light Switch)");
            techniLightRandom = new System.Random(408);
        }

        internal static LightWithId[] overrideLightWithIdActivation = null;

        private static System.Random techniLightRandom = new System.Random(408);

        //0 = off
        //1 = blue on, 5 = red on
        //2 = blue flash, 6 = red flash
        //3 = blue fade, 7 = red fade
        private static bool Prefix(LightSwitchEventEffect __instance, ref BeatmapEventData beatmapEventData, ref BeatmapEventType ____event)
        {
            if (beatmapEventData.type != ____event) return true;

            // https://docs.google.com/spreadsheets/d/1vCTlDvx0ZW8NkkZBYW6ecvXaVRxDUKX7QIoah9PCp_c/edit#gid=0
            if (ColourManager.TechnicolourLights && (int)____event <= 4)
            {
                if (beatmapEventData.value > 0 && beatmapEventData.value <= 7)
                {
                    if (ChromaConfig.TechnicolourLightsStyle != ColourManager.TechnicolourStyle.GRADIENT)
                    { //0-4 are actual lighting events, we don't want to bother with anything else like ring spins or custom events
                        if (techniLightRandom.NextDouble() < ChromaConfig.TechnicolourLightsFrequency)
                        {
                            bool blue = beatmapEventData.value <= 3; //Blue events are 1, 2 and 3
                            switch (ChromaConfig.TechnicolourLightsGrouping)
                            {
                                case ColourManager.TechnicolourLightsGrouping.ISOLATED:
                                    VFX.MayhemEvent.ActivateTechnicolour(beatmapEventData, __instance);
                                    return false;

                                case ColourManager.TechnicolourLightsGrouping.ISOLATED_GROUP:
                                    // ternary operator gore
                                    __instance.SetLightingColours(blue ? (Color?)ColourManager.GetTechnicolour(false, beatmapEventData.time, ChromaConfig.TechnicolourLightsStyle) : null,
                                        blue ? null : (Color?)ColourManager.GetTechnicolour(true, beatmapEventData.time, ChromaConfig.TechnicolourLightsStyle));
                                    break;

                                case ColourManager.TechnicolourLightsGrouping.STANDARD:
                                default:
                                    Color? t = ColourManager.GetTechnicolour(!blue, beatmapEventData.time, ChromaConfig.TechnicolourLightsStyle);
                                    ColourManager.RecolourAllLights(blue ? null : t, blue ? t : null);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // This is for fun gradient stuff
                        VFX.TechnicolourController.Instance._lightSwitchLastValue[__instance] = beatmapEventData.value;
                    }
                }
            }

            ColourLightSwitch(__instance, beatmapEventData, ____event);

            return true;
        }

        private static void SetOverrideLightWithIds(params LightWithId[] lights)
        {
            overrideLightWithIdActivation = lights;
        }

        private static void Postfix(LightSwitchEventEffect __instance)
        {
            overrideLightWithIdActivation = null;
        }

        internal static void ColourLightSwitch(MonoBehaviour __monobehaviour, BeatmapEventData beatmapEventData, BeatmapEventType _event)
        {
            // We slap this puppy in a function so that ParticleSystemEventEffect can use it too
            Color? c = null;

            // CustomLightColours
            if (ChromaLightColourEvent.CustomLightColours.Count > 0)
            {
                if (ChromaLightColourEvent.CustomLightColours.TryGetValue(_event, out Dictionary<float, Color> dictionaryID))
                {
                    foreach (KeyValuePair<float, Color> d in dictionaryID)
                    {
                        if (d.Key <= beatmapEventData.time) c = d.Value;
                    }
                }
            }

            // CustomJSONData _customData individual override
            try
            {
                if (ChromaBehaviour.LightingRegistered && beatmapEventData is CustomBeatmapEventData customData)
                {
                    dynamic dynData = customData.customData;
                    if (__monobehaviour is LightSwitchEventEffect)
                    {
                        LightSwitchEventEffect __instance = (LightSwitchEventEffect)__monobehaviour;

                        int? lightID = (int?)Trees.at(dynData, "_lightID");
                        if (lightID.HasValue)
                        {
                            LightWithId[] lights = __instance.GetLights();
                            if (lights.Length > lightID) SetOverrideLightWithIds(lights[lightID.Value]);
                        }

                        int? propID = (int?)Trees.at(dynData, "_propID");
                        if (propID.HasValue)
                        {
                            LightWithId[][] lights = __instance.GetLightsPropagationGrouped();
                            if (lights.Length > propID) SetOverrideLightWithIds(lights[propID.Value]);
                        }
                    }

                    if (__monobehaviour is LightSwitchEventEffect)
                    {
                        dynamic gradient = Trees.at(dynData, "_lightGradient");
                        if (gradient != null)
                        {
                            // GRADIENT
                            float duration = (float)Trees.at(gradient, "_duration");
                            Color initcolor = ChromaUtils.GetColorFromData(gradient, true, "_startColor");
                            Color endcolor = ChromaUtils.GetColorFromData(gradient, true, "_endColor");

                            c = ChromaGradientEvent.AddGradient(beatmapEventData.type, initcolor, endcolor, customData.time, duration);
                        }
                    }

                    // RGB
                    Color? color = ChromaUtils.GetColorFromData(dynData);
                    if (color != null)
                    {
                        c = color;
                        // Clear any active gradient
                        if (ChromaGradientEvent.CustomGradients.TryGetValue(_event, out ChromaGradientEvent gradient))
                        {
                            UnityEngine.Object.Destroy(gradient);
                            ChromaGradientEvent.CustomGradients.Remove(_event);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ChromaLogger.Log("INVALID _customData", ChromaLogger.Level.WARNING);
                ChromaLogger.Log(e);
            }

            if (c.HasValue) __monobehaviour.SetLightingColours(c.Value, c.Value);
            else if (!ChromaGradientEvent.CustomGradients.TryGetValue(_event, out _)) __monobehaviour.Reset();
        }
    }
}