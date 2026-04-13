using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ModuleBlaster : ItemModule
    {
        // General Blaster Data
        public float boltForce = 3000f;

        public int[] fireModes = {0}; // 0 - single shot, 1 - burst, 2 - rapid fire, 3 - Stun

        public int magazineCapacity = 50;
        public float reloadTime = 1.0f;
        public bool multishot = false;
        public int spawnPoints = 1;

        public float heatRecoveryRate = 0.08f;
        public float heatGain = 0.5f;
        public float heatThreshold = 10f;

        public float maxInaccuracy = 10f;
        public float inaccuracyGain = 0.5f;
        public float inaccuracyRecoverRate = 0.25f;
        public float inaccuracyMultiplier = 1.0f;

        public float recoilForce = 50f;
        public float recoilMinForce = 40f;
        public float recoilForceSideways = 3f;
        public float recoilTorque = 12f;
        public float recoilMinTorque = 8f;
        public float recoilTorqueSideways = 2f;

        public float timeBetweenShots = 0.2f;
        public float timeBetweenShotsRapidFire = 0.15f;

        public int aiMaxShotsAmount = 8;

        // Bolt Data
        public string boltProjectile;
        public string stunProjectile;

        public string boltOverride;
        public string chargedBoltOverride;

        // Burst Data
        public float timeBetweenShotsBurst = 0.1f;
        public float timeBetweenBursts = 0.2f;
        public float burstSize = 3;
        public bool playBurstSoundOnce = false;

        // Charged Data
        public float chargeTime = 1f;
        public bool chargedMultishot = false;

        public bool requiresSpin = false;

        // Stun Data
        public float timeBetweenStunShots = 0.3f;

        // Scope Data
        public bool hasScope = false;
        public float[] scopeFOVs = { 10, 5, 2.5f };
        public string reticleTextureAddress = "";
        public Texture2D reticleTexture;

        // Actions
        public string primaryAction = "shoot";
        public string primaryActionHold;
        public string altAction;
        public string altActionHold;

        public string primaryForegripAction;
        public string primaryForegripActionHold;
        public string altForegripAction;
        public string altForegripActionHold;

        public string primaryScopeAction;
        public string primaryScopeActionHold;
        public string altScopeAction;
        public string altScopeActionHold;

        // Audio
        public AudioContainer fireSoundContainer;
        public string fireSound;
        public AudioContainer corebassSoundContainer;
        public string corebassSound;
        public AudioContainer hifiSoundContainer;
        public string hifiSound;

        public AudioContainer stunSoundContainer;
        public string stunSound;
        public AudioContainer corebassStunSoundContainer;
        public string corebassStunSound;
        public AudioContainer hifiStunSoundContainer;
        public string hifiStunSound;

        public AudioContainer chargedFireSoundContainer;
        public string chargedFireSound;
        public AudioContainer corebassChargedFireSoundContainer;
        public string corebassChargedFireSound;
        public AudioContainer hifiChargedFireSoundContainer;
        public string hifiChargedFireSound;

        public AudioContainer chargeStartContainer;
        public string chargeStartSound;

        public AudioContainer chargeLoopContainer;
        public string chargeLoopSound;

        public AudioContainer chargeStopContainer;
        public string chargeStopSound;

        public AudioContainer switchSoundContainer;
        public string switchSound = "PC.TheNomadRim.Sound.SwitchFiremode";

        public AudioContainer overheatSoundContainer;
        public string overheatSound;

        public AudioContainer overheatLoopSoundContainer;
        public string overheatLoopSound;

        public AudioContainer emptySoundContainer;
        public string emptySound;

        public AudioContainer reloadStartContainer;
        public string reloadStartSound;

        public AudioContainer reloadStartContainer2;
        public string reloadStartSound2;

        public AudioContainer reloadFinishedContainer;
        public string reloadFinishedSound;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += OnSpawn;
        }

        private void OnSpawn(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;

            item.gameObject.AddComponent<ItemBlaster>();
            item.OnSpawnEvent -= OnSpawn;
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(fireSound))
                yield return Catalog.LoadAssetCoroutine(fireSound, (AudioContainer x) => fireSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(corebassSound))
                yield return Catalog.LoadAssetCoroutine(corebassSound, (AudioContainer x) => corebassSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(hifiSound))
                yield return Catalog.LoadAssetCoroutine(hifiSound, (AudioContainer x) => hifiSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(stunSound))
                yield return Catalog.LoadAssetCoroutine(stunSound, (AudioContainer x) => stunSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(corebassStunSound))
                yield return Catalog.LoadAssetCoroutine(corebassStunSound, (AudioContainer x) => corebassStunSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(hifiStunSound))
                yield return Catalog.LoadAssetCoroutine(hifiStunSound, (AudioContainer x) => hifiStunSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(chargedFireSound))
                yield return Catalog.LoadAssetCoroutine(chargedFireSound, (AudioContainer x) => chargedFireSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(corebassChargedFireSound))
                yield return Catalog.LoadAssetCoroutine(corebassChargedFireSound, (AudioContainer x) => corebassChargedFireSoundContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(hifiChargedFireSound))
                yield return Catalog.LoadAssetCoroutine(hifiChargedFireSound, (AudioContainer x) => hifiChargedFireSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(chargeStartSound))
                yield return Catalog.LoadAssetCoroutine(chargeStartSound, (AudioContainer x) => chargeStartContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(chargeLoopSound))
                yield return Catalog.LoadAssetCoroutine(chargeLoopSound, (AudioContainer x) => chargeLoopContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(chargeStopSound))
                yield return Catalog.LoadAssetCoroutine(chargeStopSound, (AudioContainer x) => chargeStopContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(switchSound))
                yield return Catalog.LoadAssetCoroutine(switchSound, (AudioContainer x) => switchSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(overheatSound))
                yield return Catalog.LoadAssetCoroutine(overheatSound, (AudioContainer x) => overheatSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(overheatLoopSound))
                yield return Catalog.LoadAssetCoroutine(overheatLoopSound, (AudioContainer x) => overheatLoopSoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(emptySound))
                yield return Catalog.LoadAssetCoroutine(emptySound, (AudioContainer x) => emptySoundContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(reloadStartSound))
                yield return Catalog.LoadAssetCoroutine(reloadStartSound, (AudioContainer x) => reloadStartContainer = x, "AudioContainer");
            if (!string.IsNullOrEmpty(reloadStartSound2))
                yield return Catalog.LoadAssetCoroutine(reloadStartSound2, (AudioContainer x) => reloadStartContainer2 = x, "AudioContainer");

            if (!string.IsNullOrEmpty(reloadFinishedSound))
                yield return Catalog.LoadAssetCoroutine(reloadFinishedSound, (AudioContainer x) => reloadFinishedContainer = x, "AudioContainer");

            if (!string.IsNullOrEmpty(reticleTextureAddress))
                yield return Catalog.LoadAssetCoroutine(reticleTextureAddress, (Texture2D tex) => reticleTexture = tex, "Texture");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (fireSoundContainer)
                Catalog.ReleaseAsset(fireSoundContainer);
            if (corebassSoundContainer)
                Catalog.ReleaseAsset(corebassSoundContainer);
            if (hifiSoundContainer)
                Catalog.ReleaseAsset(hifiSoundContainer);

            if (stunSoundContainer)
                Catalog.ReleaseAsset(stunSoundContainer);
            if (corebassStunSoundContainer)
                Catalog.ReleaseAsset(corebassStunSoundContainer);
            if (hifiStunSoundContainer)
                Catalog.ReleaseAsset(hifiStunSoundContainer);

            if (chargedFireSoundContainer)
                Catalog.ReleaseAsset(chargedFireSoundContainer);
            if (corebassChargedFireSoundContainer)
                Catalog.ReleaseAsset(corebassChargedFireSoundContainer);
            if (hifiChargedFireSoundContainer)
                Catalog.ReleaseAsset(hifiChargedFireSoundContainer);

            if (chargeStartContainer)
                Catalog.ReleaseAsset(chargeStartContainer);
            if (chargeLoopContainer)
                Catalog.ReleaseAsset(chargeLoopContainer);
            if (chargeStopContainer)
                Catalog.ReleaseAsset(chargeStopContainer);

            if (switchSoundContainer)
                Catalog.ReleaseAsset(switchSoundContainer);

            if (overheatSoundContainer)
                Catalog.ReleaseAsset(overheatSoundContainer);

            if (overheatLoopSoundContainer)
                Catalog.ReleaseAsset(overheatLoopSoundContainer);

            if (emptySoundContainer)
                Catalog.ReleaseAsset(emptySoundContainer);

            if (reloadStartContainer)
                Catalog.ReleaseAsset(reloadStartContainer);
            if (reloadStartContainer2)
                Catalog.ReleaseAsset(reloadStartContainer2);

            if (reloadFinishedContainer)
                Catalog.ReleaseAsset(reloadFinishedContainer);

            if (reticleTexture)
                Catalog.ReleaseAsset(reticleTexture);
        }
    }
}