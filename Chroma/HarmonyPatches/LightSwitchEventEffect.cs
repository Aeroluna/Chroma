﻿namespace Chroma.HarmonyPatches
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Chroma.Events;
    using Chroma.Extensions;
    using Chroma.Utils;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;

    [ChromaPatch(typeof(LightSwitchEventEffect))]
    [ChromaPatch("Start")]
    internal class LightSwitchEventEffectStart
    {
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static void Postfix(LightSwitchEventEffect __instance, BeatmapEventType ____event)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            __instance.StartCoroutine(WaitThenStart(__instance, ____event));
        }

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static IEnumerator WaitThenStart(LightSwitchEventEffect __instance, BeatmapEventType ____event)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            yield return new WaitForEndOfFrame();
            LightSwitchEventEffectExtensions.LSEStart(__instance, ____event);
        }
    }

    [ChromaPatch(typeof(LightSwitchEventEffect))]
    [ChromaPatch("OnDestroy")]
    internal class LightSwitchEventEffectOnDestroy
    {
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static void Postfix(LightSwitchEventEffect __instance, BeatmapEventType ____event)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            LightSwitchEventEffectExtensions.LSEDestroy(__instance, ____event);
        }
    }

    [ChromaPatch(typeof(LightSwitchEventEffect))]
    [ChromaPatch("SetColor")]
    internal class LightSwitchEventEffectSetColor
    {
        private static bool Prefix(Color color)
        {
            if (LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger.OverrideLightWithIdActivation != null)
            {
                LightWithId[] lights = LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger.OverrideLightWithIdActivation;
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].ColorWasSet(color);
                }

                return false;
            }

            return true;
        }
    }

    [ChromaPatch(typeof(LightSwitchEventEffect))]
    [ChromaPatch("HandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger")]
    internal class LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger
    {
        internal static LightWithId[] OverrideLightWithIdActivation { get; set; }

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        internal static void ColorLightSwitch(MonoBehaviour __monobehaviour, BeatmapEventData beatmapEventData, BeatmapEventType _event)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            __monobehaviour.SetLastValue(beatmapEventData.value);

            Color? c = null;

            // LightColors
            if (ChromaLegacyRGBEvent.LightColors.TryGetValue(_event, out List<TimedColor> dictionaryID))
            {
                List<TimedColor> colors = dictionaryID.Where(n => n.Time <= beatmapEventData.time).ToList();
                if (colors.Count > 0)
                {
                    c = colors.Last().Color;
                }
            }

            // CustomJSONData _customData individual override
            if (beatmapEventData is CustomBeatmapEventData customData)
            {
                dynamic dynData = customData.customData;
                if (__monobehaviour is LightSwitchEventEffect)
                {
                    LightSwitchEventEffect instance = (LightSwitchEventEffect)__monobehaviour;

                    int? lightID = (int?)Trees.at(dynData, "_lightID");
                    if (lightID.HasValue)
                    {
                        LightWithId[] lights = instance.GetLights();
                        if (lights.Length > lightID)
                        {
                            SetOverrideLightWithIds(lights[lightID.Value]);
                        }
                    }

                    int? propID = (int?)Trees.at(dynData, "_propID");
                    if (propID.HasValue)
                    {
                        LightWithId[][] lights = instance.GetLightsPropagationGrouped();
                        if (lights.Length > propID)
                        {
                            SetOverrideLightWithIds(lights[propID.Value]);
                        }
                    }
                }

                if (__monobehaviour is LightSwitchEventEffect)
                {
                    dynamic gradient = Trees.at(dynData, "_lightGradient");
                    if (gradient != null)
                    {
                        // GRADIENT
                        float duration = (float)Trees.at(gradient, "_duration");
                        Color initcolor = ChromaUtils.GetColorFromData(gradient, "_startColor");
                        Color endcolor = ChromaUtils.GetColorFromData(gradient, "_endColor");
                        string easingstring = (string)Trees.at(gradient, "_easing");
                        Functions easing;
                        if (string.IsNullOrEmpty(easingstring))
                        {
                            easing = Functions.easeLinear;
                        }
                        else
                        {
                            easing = (Functions)Enum.Parse(typeof(Functions), easingstring);
                        }

                        c = ChromaGradientEvent.AddGradient(beatmapEventData.type, initcolor, endcolor, customData.time, duration, easing);
                    }
                }

                // RGB
                Color? color = ChromaUtils.GetColorFromData(dynData);
                if (color != null)
                {
                    c = color;

                    // Clear any active gradient
                    if (ChromaGradientEvent.Gradients.TryGetValue(_event, out ChromaGradientEvent gradient))
                    {
                        UnityEngine.Object.Destroy(gradient);
                        ChromaGradientEvent.Gradients.Remove(_event);
                    }
                }
            }

            if (c.HasValue)
            {
                __monobehaviour.SetLightingColors(c.Value, c.Value);
            }
            else if (!ChromaGradientEvent.Gradients.TryGetValue(_event, out _))
            {
                __monobehaviour.Reset();
            }
        }

        // 0 = off
        // 1 = blue on, 5 = red on
        // 2 = blue flash, 6 = red flash
        // 3 = blue fade, 7 = red fade
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static bool Prefix(LightSwitchEventEffect __instance, BeatmapEventData beatmapEventData, BeatmapEventType ____event)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            if (beatmapEventData.type != ____event)
            {
                return true;
            }

            ColorLightSwitch(__instance, beatmapEventData, ____event);

            return true;
        }

        private static void SetOverrideLightWithIds(params LightWithId[] lights)
        {
            OverrideLightWithIdActivation = lights;
        }

        private static void Postfix()
        {
            OverrideLightWithIdActivation = null;
        }
    }
}
