using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ModuleItemGrenade : ItemModule
    {
        public string sActivationType = "Timed"; // Timed, Impact
        public string sSafetyType = "Once"; // Once, Twice
        public bool bCanBeDeactivated = false;

        public string sActivationSoundAddress = "";
        public string sDeactivationSoundAddress = "";
        public string sActivatedLoopSoundAddress = "";
        public string sTimedSoundAddress = ""; // Required for Timed activation type, once the sound stops the grenade goes off

        public string sExplosionEffect = "";
        public string sExplosionSoundAddress = "";
        public string sExplosionBassAddress = "";
        public EffectData explosionEffect;

        public AudioContainer activationSound;
        public AudioContainer deactivationSound;
        public AudioContainer activatedLoopSound;
        public AudioContainer timedSound;
        public AudioContainer explosionSound;
        public AudioContainer explosionBassSound;

        public List<string> mDamageTypes = new List<string>() { "None" }; // None: does nothing,
                                                                          // Fire: sets creatures in radius on fire,
                                                                          // Electric: shocks creatures in radius,
                                                                          // Gravity: Disabled gravity for creatures and items in radius,
                                                                          // Dismemberment: Dismembers creatures in radius
                                                                          // Heal: Heals creatures in radius
                                                                          // Damage: Damages creatures in radius
                                                                          // Destabilize: Ragdolls creatures in radius
                                                                          // Disorientate: Disorientates creatures in radius
                                                                          // Rescale: Rescales Objects and Creatures in the radius
                                                                          // Drop: Will make the creature drop whatever they are holding


        public int iLimbLimit = 50; // How many limbs the Dismemberment damage type can dismember before stopping
        public bool bDismembermentNonFatal = false; // If enabled, only limbs that won't kill the creatrue will be dismembered

        public float fHealthAmount = 50f; // How much health the affected creatures should receive when using the Heal damage type
        public float fDamageAmount = 50f; // How much damage the affected creatures should receive when using the Damage damage type

        public bool bBreakBreakables = true; // If enabled, breakable objects will be broken by the grenade explosion
        public bool bBreakCrystals = true; // If enabled, the explosion will break Hector's crystals
        public bool bBypassShield = false; // If enabled, the explosion will bypass the shiled covering Hector's crystals

        public float fRadius = 5f; // The radius of the explosion
        public float fForce = 500f; // The force applied to objects within the explosion radius

        public bool bSetScale = false; // If enabled, the Rescale damage type will set the scale to the fRescale value, otherwise it'll multiply the current scale by fRescale
        public float fRescale = 1.2f;

        public bool bDropIncludeArmor = false; // If enabled, the Drop damage type will also make creatures drop their armor too

        // Animations
        public string sAnimActivate = "";
        public string sAnimDeactivate = "";

        // Material
        public float[] fEmissionColorActivated = {0, 0, 0, 0 };
        public float[] fEmissionColorDeactivated = {0, 0, 0, 0 };
        
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemGrenade>();
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);

            if (!string.IsNullOrEmpty(sExplosionEffect))
                explosionEffect = Catalog.GetData<EffectData>(sExplosionEffect);
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(sActivationSoundAddress))
                yield return Catalog.LoadAssetCoroutine(sActivationSoundAddress, delegate (AudioContainer x) { activationSound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(sDeactivationSoundAddress))
                yield return Catalog.LoadAssetCoroutine(sDeactivationSoundAddress, delegate (AudioContainer x) { deactivationSound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(sActivatedLoopSoundAddress))
                yield return Catalog.LoadAssetCoroutine(sActivatedLoopSoundAddress, delegate (AudioContainer x) { activatedLoopSound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(sTimedSoundAddress))
                yield return Catalog.LoadAssetCoroutine(sTimedSoundAddress, delegate (AudioContainer x) { timedSound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(sExplosionSoundAddress))
                yield return Catalog.LoadAssetCoroutine(sExplosionSoundAddress, delegate (AudioContainer x) { explosionSound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(sExplosionBassAddress))
                yield return Catalog.LoadAssetCoroutine(sExplosionBassAddress, delegate (AudioContainer x) { explosionBassSound = x; }, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (activationSound != null)
                Catalog.ReleaseAsset(activationSound);

            if (deactivationSound != null)
                Catalog.ReleaseAsset(deactivationSound);

            if (activatedLoopSound != null)
                Catalog.ReleaseAsset(activatedLoopSound);

            if (timedSound != null)
                Catalog.ReleaseAsset(timedSound);

            if (explosionSound != null)
                Catalog.ReleaseAsset(explosionSound);

            if (explosionBassSound != null)
                Catalog.ReleaseAsset(explosionBassSound);

        }
    }
}
