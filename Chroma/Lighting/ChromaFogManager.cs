namespace Chroma
{
    using System;
    using System.Collections;
    using Chroma.Utils;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;
    using static Chroma.Plugin;

    internal static class ChromaFogManager
    {
        private static BloomFogEnvironmentParams _originalFogParams;

        internal static BloomFogSO BloomFogSO { get; set; }

        internal static void Initialize(CustomEventCallbackController customEventCallback, CustomBeatmapData customBeatmap)
        {
            _originalFogParams = BloomFogSO.defaultForParams;

            customEventCallback.AddCustomEventCallback(HandleFogCustomEvents);

            object levelFogSettings = Trees.at(customBeatmap.beatmapCustomData, FOGSETTINGS);

            if (levelFogSettings != null)
            {
                BloomFogEnvironmentParams fogParams = ParseFogSettingsFromCustomData(levelFogSettings);
                BloomFogSO.defaultForParams = fogParams;
                BloomFogSO.transitionFogParams = fogParams;
            }
        }

        private static void HandleFogCustomEvents(CustomEventData customEvent)
        {
            switch (customEvent.type)
            {
                case SETFOGSETTINGS:
                    BloomFogEnvironmentParams parsedParams = ParseFogSettingsFromCustomData(customEvent.data);
                    BloomFogSO.defaultForParams = parsedParams;
                    BloomFogSO.transitionFogParams = parsedParams;
                    BloomFogSO.transition = 0;
                    break;

                case ANIMATEFOGSETTINGS:
                    dynamic newSettings = Trees.at(customEvent.data, NEWFOGSETTINGS);
                    BloomFogEnvironmentParams newParams = ParseFogSettingsFromCustomData(newSettings);
                    BloomFogSO.transitionFogParams = newParams;
                    BloomFogSO.transition = 0;

                    float duration = ((float?)Trees.at(customEvent.data, DURATION)) ?? 1;
                    string easingString = ((string)Trees.at(customEvent.data, EASING)) ?? "easeLinear";
                    Functions easing = (Functions)Enum.Parse(typeof(Functions), easingString);

                    SharedCoroutineStarter.instance.StartCoroutine(AnimateFogSettings(duration, easing));
                    break;

                case RESETFOGSETTINGS:
                    BloomFogSO.defaultForParams = _originalFogParams;
                    BloomFogSO.transitionFogParams = _originalFogParams;
                    BloomFogSO.transition = 0;
                    break;
            }
        }

        private static IEnumerator AnimateFogSettings(float duration, Functions easing)
        {
            float t = 0;
            while (t < 1)
            {
                BloomFogSO.transition = Easings.Interpolate(t / duration, easing);
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            BloomFogSO.transition = 1;
        }

        private static BloomFogEnvironmentParams ParseFogSettingsFromCustomData(dynamic data)
        {
            BloomFogEnvironmentParams fogParams = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();

            fogParams.attenuation = ((float?)Trees.at(data, ATTENUATION)).GetValueOrDefault(0.1f);
            fogParams.offset = ((float?)Trees.at(data, OFFSET)).GetValueOrDefault(0f);
            fogParams.heightFogStartY = ((float?)Trees.at(data, FOGFLOOR)).GetValueOrDefault(-300f);
            fogParams.heightFogHeight = ((float?)Trees.at(data, FOGHEIGHT)).GetValueOrDefault(10f);

            return fogParams;
        }
    }
}
