using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class Global
    {
        public static List<LightsaberBlade> allBlades = new List<LightsaberBlade>();
        public static bool globalUsePP = false;
    }

    public class Util
    {
        public static void PlayHaptic(RagdollHand hand, float intensity)
        {
            if (!ModSettings.bHaptics)
                return;

            if (hand != null) PlayerControl.GetHand(hand.side).HapticShort(intensity);
            else
            {
                PlayerControl.GetHand(Side.Left).HapticShort(intensity);
                PlayerControl.GetHand(Side.Right).HapticShort(intensity);
            }
        }

        public static void PlayHaptic(bool left, bool right, float intensity)
        {
            var leftHand = left ? Player.local.handLeft : null;
            var rightHand = right ? Player.local.handRight : null;

            if (leftHand != null && rightHand != null) PlayHaptic(null, intensity);

            if (leftHand)
                PlayHaptic(leftHand.ragdollHand, intensity);

            if (rightHand)
                PlayHaptic(rightHand.ragdollHand, intensity);
        }

        // Sounds

        public static void PlaySoundLooped(AudioSource source, AudioContainer audioContainer = null, float volume = -1)
        {
            if (source)
            {
                if (audioContainer != null) source.clip = audioContainer.PickAudioClip(UnityEngine.Random.Range(0, audioContainer.sounds.Count - 1));
                source.volume = volume == -1 ? source.volume : volume;
                source.loop = true;
                source.Play();
            }
            return;
        }

        public static void PlaySound(AudioSource source, AudioContainer audioContainer = null, float volume = -1, bool stopPlaying = false)
        {
            if (source)
            {
                if (stopPlaying) source.Stop();

                source.volume = volume == -1 ? source.volume : volume; source.loop = false;
                if (audioContainer == null && source.clip == null) return;
                source.PlayOneShot(audioContainer ? audioContainer.GetRandomAudioClip() : source.clip);
            }
        }

        public static void StopLoopedSound(AudioSource source)
        {
            if (source)
            {
                source.Stop();
            }
        }

        // Save data clean up 

        public static void CleanCustomBlasterDataProperly(Item item)
        {
            if (item.HasCustomData<BlasterSaveData>())
            {
                item.RemoveCustomData<BlasterSaveData>();

                if (item.TryGetCustomData(out BlasterSaveData data))
                {

                    DebugService.LogInfo($"Custom Data still persists, forcing clean-up now ...");

                    item.contentCustomData.Remove(data);
                    item.OverrideCustomData(item.contentCustomData);
                }
            }
        }

        public static void CleanCustomSaveHolderDataProperly(Item item)
        {
            if (item.HasCustomData<ItemSaveHolderData>())
            {
                item.RemoveCustomData<ItemSaveHolderData>();

                if (item.TryGetCustomData(out ItemSaveHolderData data))
                {

                    DebugService.LogInfo($"Custom Data still persists, forcing clean-up now ...");

                    item.contentCustomData.Remove(data);
                    item.OverrideCustomData(item.contentCustomData);
                }
            }
        }

        public static void CleanCustomLightsaberDataProperly(Item item)
        {
            if (item.HasCustomData<CustomLightsaberData>())
            {
                item.RemoveCustomData<CustomLightsaberData>();
                if (item.TryGetCustomData(out CustomLightsaberData data))
                {
                    DebugService.LogInfo($"Custom Data still persists, forcing clean-up now ...");
                    item.contentCustomData.Remove(data);
                    item.OverrideCustomData(item.contentCustomData);
                }
            }
        }

        // Gradient

        public static Texture2D CreateGradientTexture(List<Color> colors, int width = 256)
        {
            width = Mathf.Max(width, 2);

            DebugService.LogInfo($"Creating Gradient Texture with width {width}");

            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };


            for (int x = 0; x < width; x++)
            {
                Color color = EvaluateGradient(colors, x / (float)(width - 1));
                texture.SetPixel(x, 1, color);
            }

            texture.Apply();
            return texture;
        }

        public static Color EvaluateGradient(List<Color> colors, float t)
        {
            if (colors.Count == 0) return Color.magenta;
            if (colors.Count == 1) return colors[0];

            float segmentSize = 1f / (colors.Count - 1);
            int segmentIndex = Mathf.FloorToInt(t * (colors.Count - 1));
            segmentIndex = Mathf.Clamp(segmentIndex, 0, colors.Count - 2);

            float segmentT = (t - segmentIndex * segmentSize) / segmentSize;
            return Color.Lerp(colors[segmentIndex], colors[segmentIndex + 1], segmentT);
        }
    }

    class DebugService
    {
        private static string GetFileNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "UnknownFile";

            return path.Replace('\\', '/').Split('/').Last();
        }


        public static void LogInfo(string message, [CallerFilePath] string filePath = "") => Log(message, "Info", filePath);
        public static void LogWarning(string message, [CallerFilePath] string filePath = "") => Log(message, "Warning", filePath);
        public static void LogError(string message, [CallerFilePath] string filePath = "") => Log(message, "Error", filePath);

        public static void Log(string message, string type, string filePath = "")
        {
            UnityEngine.Debug.Log($"[{type}: {GetFileNameFromPath(filePath)}] {message}");
        }
    }
}