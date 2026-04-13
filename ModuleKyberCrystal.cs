using System.Collections;
using UnityEngine;
using ThunderRoad;
using System;
using System.Collections.Generic;

namespace TheNomadRim
{
    [Serializable]
    public class Multicolor
    {
        public float[] color = { 1f, 1f, 1f, 1f };

        public Multicolor() { }

        public Multicolor(float[] color)
        {
            this.color = color;
        }

        public Color ToUnityColor()
        {
            if (color.Length >= 4)
                return new Color(color[0], color[1], color[2], color[3]);
            if (color.Length == 3)
                return new Color(color[0], color[1], color[2], 1f);
            return Color.white;
        }
    }

    public class ModuleKyberCrystal : ItemModule
    {
        //  Kyber Crystal
        public float[] crystalColor = { 1f, 1f, 1f, 1f };
        public float[] crystalGlowColor = { 1f, 1f, 1f, 1f };
        public float glowIntensity = 1f;
        public bool isCrystalCracked = false;
        public float crackIntensity = 2f;

        // Legacy Shader Data
        public float[] coreColor = { 1f, 1f, 1f, 1f };
        public float[] glowColor = { 1f, 1f, 1f, 1f };
        public float[] altGlowColor = { 1f, 1f, 1f, 1f };
        public float gradientTiling = 4;
        public float glowWidth = 1f;
        public float jitterAmount = 0.095f;
        public float glowMode = 0;
        public float scrollSpeed = 0.5f;
        public float fadeSpeed = 0.5f;

        // Color
        public float[] coreColorGO = {1f, 1f, 1f, 1f };
        public float[] glowColorGO = { 1f, 1f, 1f, 1f };
        public float lookMultiplier = 4f;

        // Gradient
        public bool useGradient = false;
        public float gradientTilingGO = 1f;
        public List<Multicolor> gradientColors = new List<Multicolor>();
        public int gradientWidth = 32;
        public float gradientIntensityGO = 3.5f;
        public float gradientSpeedGO = 1f;

        // Intense Glow
        public float intensePosition = 0.8f;
        public float intenseIntensity = 2f;
        public float intenseConvergence = 0.3f;
        public float intenseFalloff = 0.92f;

        // Flicker
        public bool flickerEnabled = true;
        public float flickerSpeed = 50f;
        public float[] flickerRange = { 0.85f, 1f };

        // Corrupted
        public bool isCorrupted = false;

        // Light
        public float[] lightColor = { 1f, 1f, 1f, 1f };
        public float lightIntensity = 2f;
        public float lightRange = 1f;

        // Smoke
        public bool useSmoke = false;
        public float[] smokeColor = { 1f, 1f, 1f, 1f };
        public float smokeScrollSpeed = 1f;
        public float smokeTiling = 8;

        public string smoothSwingLowID;
        public string smoothSwingHighID;
        public string accentSwingID;
        public EffectData smoothSwingHigh;
        public EffectData smoothSwingLow;
        public EffectData accentSwing;

        public string idleSoundAddress;
        public string activationSoundAddress;
        public string deactivationSoundAddress;

        public AudioContainer idleContainer;
        public AudioContainer actiavtionContainer;
        public AudioContainer deactivationContainer;

        public float idleVolume = 1f;
        public float idlePitch = 1f;
        public float activationVolume =1f;
        public float activationPitch = 1f;
        public float deactivationVolume = 1f;
        public float deactivationPitch = 1f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemKyberCrystal>();
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);
            if (!string.IsNullOrEmpty(smoothSwingLowID))
                smoothSwingLow = Catalog.GetData<EffectData>(smoothSwingLowID);
            if (!string.IsNullOrEmpty(smoothSwingHighID))
                smoothSwingHigh = Catalog.GetData<EffectData>(smoothSwingHighID);
            if (!string.IsNullOrEmpty(accentSwingID))
                accentSwing = Catalog.GetData<EffectData>(accentSwingID);
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(idleSoundAddress)) 
                yield return Catalog.LoadAssetCoroutine(idleSoundAddress, delegate (AudioContainer x) { idleContainer = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(activationSoundAddress))
                yield return Catalog.LoadAssetCoroutine(activationSoundAddress, delegate (AudioContainer x) { actiavtionContainer = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(deactivationSoundAddress))
                yield return Catalog.LoadAssetCoroutine(deactivationSoundAddress, delegate (AudioContainer x) { deactivationContainer = x; }, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (idleContainer != null)
                Catalog.ReleaseAsset(idleContainer);

            if (actiavtionContainer != null)
                Catalog.ReleaseAsset(actiavtionContainer);

            if (deactivationContainer != null)
                Catalog.ReleaseAsset(deactivationContainer);
        }

    }
}
