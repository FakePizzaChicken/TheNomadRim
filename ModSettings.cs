using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheNomadRim
{
    public class ModSettings
    {
        public static ModOptionFloat[] QuarterStepValues() => ModOptionFloat.CreateArray(0, 20, 0.25f);

        public static ModOptionFloat[] TenthStepValues() => ModOptionFloat.CreateArray(0, 5, 0.1f);

        public static ModOptionFloat[] TrailLifetimeValues() => ModOptionFloat.CreateArray(0, 1, 0.01f);

        public static ModOptionFloat[] DegreeValues() => ModOptionFloat.CreateArray(0, 360, 1f);

        public static ModOptionInt[] Chances() => ModOptionInt.CreateArray(0, 100, 5);

        public static ModOptionFloat[] RadiusAssist() => ModOptionFloat.CreateArray(0, 1, 0.01f);

        public static ModOptionFloat[] Zero2One() => ModOptionFloat.CreateArray(0, 1, 0.1f);

        public static ModOptionInt[] MaxLights() => ModOptionInt.CreateArray(0, 8, 1);

        public static ModOptionFloat[] damageMultipliers = {
            new ModOptionFloat("0.0x", 0f),
            new ModOptionFloat("0.1x", 0.1f),
            new ModOptionFloat("0.2x", 0.2f),
            new ModOptionFloat("0.3x", 0.3f),
            new ModOptionFloat("0.4x", 0.4f),
            new ModOptionFloat("0.5x", 0.5f),
            new ModOptionFloat("0.6x", 0.6f),
            new ModOptionFloat("0.7x", 0.7f),
            new ModOptionFloat("0.8x", 0.8f),
            new ModOptionFloat("0.9x", 0.9f),
            new ModOptionFloat("1.0x", 1.0f),
            new ModOptionFloat("1.25x", 1.25f),
            new ModOptionFloat("1.5x", 1.5f),
            new ModOptionFloat("1.75x", 1.75f),
            new ModOptionFloat("2.0x", 2.0f),
            new ModOptionFloat("2.5x", 2.5f),
            new ModOptionFloat("3.0x", 3.0f),
            new ModOptionFloat("3.5x", 3.5f),
            new ModOptionFloat("4.0x", 4.0f),
            new ModOptionFloat("4.5x", 4.5f),
            new ModOptionFloat("5.0x", 5.0f),
            new ModOptionFloat("6.0x", 6.0f),
            new ModOptionFloat("7.0x", 7.0f),
            new ModOptionFloat("8.0x", 8.0f),
            new ModOptionFloat("9.0x", 9.0f),
            new ModOptionFloat("10.0x", 10.0f),
            new ModOptionFloat("15.0x", 15.0f),
            new ModOptionFloat("20.0x", 20.0f)
        };

        public static ModOptionFloat[] RainIntensities =
        {
            new ModOptionFloat("No Rain", 0),
            new ModOptionFloat("Light Drops", 5),
            new ModOptionFloat("Very Light Rain", 30),
            new ModOptionFloat("Light Rain", 50),
            new ModOptionFloat("Medium Rain", 100),
            new ModOptionFloat("Heavy Rain", 500),
            new ModOptionFloat("Storm", 1500),
            new ModOptionFloat("Big Storm", 3500),
            new ModOptionFloat("Bigger Storm", 5500),
            new ModOptionFloat("Biggest Storm", 10000)
        };

        public static ModOptionInt[] ScopeResolutions = {
            new ModOptionInt("64", 64),
            new ModOptionInt("128", 128),
            new ModOptionInt("256", 256),
            new ModOptionInt("512", 512),
            new ModOptionInt("1024", 1024),
            new ModOptionInt("2048", 2048)
        };

        public static ModOptionInt[] ClearSecretsProgress =
        {
            new ModOptionInt("Clear", 0)
        };

        // General
        [ModOptionCategory("General", 0)]
        [ModOption(name: "Haptic Feedback", tooltip: "Enables haptic feedback for some interactions.", category = "General", defaultValueIndex = 1)]
        public static bool bHaptics = true;
        [ModOption(name: "Lightsaber Length Adjuster Value", tooltip: "By how much the Lightsaber tool should adjust the lightsaber blade length.", category = "General", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 5, order = 2)]
        public static float fLengthAdjusterAdjusted = 0.05f;
        [ModOption(name: "Lightsaber Joining", tooltip: "Allows joining 2 seperate lightsabers together.", category = "General", defaultValueIndex = 1, order = 1)]
        public static bool bLightsaberJoining = true;
        [ModOption("Item Improved Collisions", "Improves collision detection for all TNR melee weapons", category = "General", defaultValueIndex = 1, order = 3)]
        public static bool bItemImprovedCollisions = true;
        [ModOption("Improved Collisions Threshold", "How fast the items have to be swung for the improved collision detection to activate", category = "General", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 20, order = 4)]
        public static float fItemImprovedCollisionsThreshold = 5f;


        // Lightsabers - Better Collision
        [ModOptionCategory("Lightsabers - Better Collision", 1)]
        [ModOption(name: "Better Collision Detection", tooltip: "Increases collision accuracy when swinging lightsabers rapidly.", category = "Lightsabers - Better Collision", defaultValueIndex = 1, order = 1)]
        public static bool bBetterCollisions = true;

        [ModOption(name: "Better Collision Threshold", tooltip: "The minimum speed required for better collision detection.", category = "Lightsabers - Better Collision", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 26, order = 2)]
        public static float fBetterCollisionsVelocity = 6.5f;


        // Lightsabers - Auto Deactivate
        [ModOptionCategory("Lightsabers - Auto Deactivate", 2)]
        [ModOption(name: "Auto-Deactivate", tooltip: "Turns off lightsaber automatically when dropped.", category = "Lightsabers - Auto Deactivate", defaultValueIndex = 0, order = 1)]
        public static bool bDeactivateOnDrop = false;

        [ModOption(name: "Auto-Deactivate Delay", tooltip: "Delay in seconds before the lightsaber deactivates.", category = "Lightsabers - Auto Deactivate", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 16, order = 2)]
        public static float fDeactivateDelay = 4f;


        // Lightsabers - Recall
        [ModOptionCategory("Lightsabers - Recall", 3)]
        [ModOption(name: "Lightsaber Recall", tooltip: "Allows you to recall the lightsaber after throwing it.", category = "Lightsabers - Recall", defaultValueIndex = 1, order = 1)]
        public static bool bSaberRecalling = true;

        [ModOption(name: "Recall Threshold", tooltip: "The minimum velocity required to make the lightsaber recallable.", category = "Lightsabers - Recall", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 32, order = 2)]
        public static float fSaberThrowVelocity = 8f;

        [ModOption(name: "Auto-Activate On Recall", tooltip: "Automatically ignites the lightsaber when it is recalled.", category = "Lightsabers - Recall", defaultValueIndex = 0, order = 3)]
        public static bool bActivateOnRecall = false;

        [ModOption(name: "Recall Speed Multiplier", tooltip: "Multiplier of the speed the lightsaber gets recalled at.", category = "Lightsabers - Recall", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 4)]
        public static float fRecallSpeedMult = 1f;


        // Lightsabers - Trail
        [ModOptionCategory("Lightsabers - Trail", 4)]
        [ModOption(name: "Lightsaber Trail", tooltip: "Toggle the trail effect.", category = "Lightsabers - Trail", defaultValueIndex = 1, order = 1)]
        public static bool bLightsaberTrail = true;

        [ModOption(name: "Trail Lifetime", tooltip: "Duration in seconds for which the trail remains visible.", category = "Lightsabers - Trail", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 3, order = 2)]
        public static float fTrailLifetime = 0.03f;


        // Lightsabers - Sounds
        [ModOptionCategory("Lightsabers - Sounds", 5)]
        [ModOption(name: "Toggle Sound Volume", tooltip: "Volume multiplier of toggle sounds for lightsabers.", category = "Lightsabers - Sounds", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 1)]
        public static float fLightsaberToggleVolumeMult = 1f;

        [ModOption(name: "Hum Sound Volume", tooltip: "Volume multiplier of hum sounds for lightsabers.", category = "Lightsabers - Sounds", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 2)]
        public static float fLightsaberHumVolumeMult = 1f;

        [ModOption(name: "Accent Swing Intensity Multiplier", tooltip: "Intensity/Volume multiplier of the accent swing.", category = "Lightsabers - Sounds", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 3)]
        public static float fAccentMult = 1f;

        [ModOption(name: "Swing Intensity Multiplier", tooltip: "Intensity/Volume multiplier of the swing.", category = "Lightsabers - Sounds", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 4)]
        public static float fSwingMult = 1f;



        // Lightsabers - Appearance
        [ModOptionCategory("Lightsabers - Appearance", 6)]
        [ModOption(name: "Lightsaber Ignition Speed", tooltip: "The speed the lightsaber blade ignites at.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 1, order = 1)]
        public static float fLightsaberIgniteSpeed = 0.1f;

        [ModOption(name: "Light Range Multiplier", tooltip: "Multiplier for the lightsaber light range.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 2)]
        private static void UpdateLightRange(float value)
        {
            fSaberLightRangeMultiplier = value;

            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberLightRangeMultiplier = 1f;

        [ModOption(name: "Light Intensity Multiplier", tooltip: "Multiplier for the lightsaber light intensity.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 3)]
        private static void UpdateLightIntensity(float value)
        {
            fSaberLightIntensityMultiplier = value;

            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberLightIntensityMultiplier = 1f;

        [ModOption(name: "Glow Width", tooltip: "Set the widht of the lightsaber glow.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 4)]
        private static void UpdateLightWidth(float value)
        {
            fSaberGlowWidthMultiplier = value;

            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberGlowWidthMultiplier = 1f;

        [ModOption("Global Length Multiplier", "Multiplier for the length of all lightsaber blades", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 5)]
        private static void UpdateGlobalLengthMultiplier(float value)
        {
            fGlobalLightsaberLenghtMultiplier = value;
            foreach (var blade in Global.allBlades)
            {
                blade.UpdateBladeDimensions();
            }
        }
        public static float fGlobalLightsaberLenghtMultiplier = 1f;

        [ModOption("Global Width Multiplier", "Multiplier for the width of all lightsaber blades", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 6)]
        private static void UpdateGlobalWidthMultiplier(float value)
        {
            fGlobalLightsaberWidthMultiplier = value;
            foreach (var blade in Global.allBlades)
            {
                blade.UpdateBladeDimensions();
            }
        }
        public static float fGlobalLightsaberWidthMultiplier = 1f;

        [ModOptionSlider]
        [ModOption(name: "Global Lightsaber Bloom Multiplier", tooltip: "Dev.", category = "Lightsabers - Appearance", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 100, order = 7)]
        private static void UpdateBlloommm(float value)
        {
            fGlobalSaberBloomMult = value;

            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }
        public static float fGlobalSaberBloomMult = 1f;

        [ModOption("Dual Lights", "Enabling this will use a light close to the bottom of the blade and a light at the tip of the blade instead of one at the center", category = "Lightsabers - Appearance", defaultValueIndex = 0)]
        private static void UpdateDualLights(bool on)
        {
            bDualLights = on;
            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }
        public static bool bDualLights = false;


        // Lightsabers - Deflect Assist
        [ModOptionCategory("Lightsabers - Deflect Assist", 7)]
        [ModOption(name: "Deflect Assist", tooltip: "Helps you in deflecting blaster bolts.", category = "Lightsabers - Deflect Assist", defaultValueIndex = 1, order = 1)]
        public static bool bDeflectAssist = true;

        [ModOption(name: "Deflect Chance", tooltip: "The chance in % that the Deflect Assist will deflect blaster bolts.", category = "Lightsabers - Deflect Assist", valueSourceName = nameof(Chances), defaultValueIndex = 16, order = 2)]
        public static int iDeflectChance = 80;

        [ModOption(name: "Deflect Assist Radius", tooltip: "The radius from the lightsaber blade the bolt has to be in to be deflected.", category = "Lightsabers - Deflect Assist", valueSourceName = nameof(RadiusAssist), defaultValueIndex = 30, order = 3)]
        public static float fDeflectAssistRadius = 0.3f;


        // Lightsabers - Swings (W.I.P)
        [ModOptionCategory("Lightsabers - Swings (W.I.P)", 8)]
        [ModOption(name: "Smooth Swing", tooltip: "Allows smooth swing to play (W.I.P)", category = "Lightsabers - Swings (W.I.P)", defaultValueIndex = 1, order = 1)]
        private static void fixSmoothSwing(bool on)
        {
            bAccentSwings = on;

            foreach (var blade in Global.allBlades)
            {
                if (blade.m_whoosh_point)
                {
                    if (on)
                    {
                        blade.m_whoosh_point.maxVelocity = float.MaxValue;
                        blade.m_whoosh_point.minVelocity = float.MaxValue;
                    }
                    else
                    {
                        blade.m_whoosh_point.maxVelocity = blade.f_original_whoosh_max;
                        blade.m_whoosh_point.minVelocity = blade.f_original_whoosh_min;
                    }
                }
            }
        }
        public static bool bAccentSwings = true;

        [ModOption(name: "Accent Swings Threshold", tooltip: "The minimum speed required for accent swings.", category = "Lightsabers - Swings (W.I.P)", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 32, order = 2)]
        public static float fAccentSwingsThreshold = 8.0f;

        [ModOption(name: "Smooth Swing Threshold", tooltip: "The minimum speed required for the high smooth swing to play.", category = "Lightsabers - Swings (W.I.P)", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 24, order = 2)]
        public static float fSmoothSwingThreshold = 6.0f;


        // Lightsabers - Spinning
        [ModOptionCategory("Lightsabers - Spinning", 9)]
        [ModOption(name: "Spinning", tooltip: "Enable lightsaber spinning when thrown.", category = "Lightsabers - Spinning", defaultValueIndex = 1, order = 1)]
        public static bool bLightsaberSpinning = true;

        [ModOption(name: "Spinning Speed", tooltip: "The speed of the lightsaber spinning.", category = "Lightsabers - Spinning", valueSourceName = nameof(DegreeValues), defaultValueIndex = 1, order = 2)]
        public static float fLightsaberSpinningSpeed = 1f;


        // Blasters
        [ModOptionCategory("Blasters", 10)]
        [ModOption(name: "No Recoil", tooltip: "Removes the recoil from blasters.", category = "Blasters", defaultValueIndex = 0, order = 0)]
        public static bool bNoRecoil = false;

        [ModOption(name: "No Spread", tooltip: "The bolts will shoot straight forward without any inaccuracies.", category = "Blasters", defaultValueIndex = 0, order = 1)]
        public static bool bNoSpread = false;

        [ModOption(name: "Scope Resolution", tooltip: "Resolution that the scope renders at", category = "Blasters", valueSourceName = nameof(ScopeResolutions), defaultValueIndex = 1, order = 2)]
        public static int iBlasterScopeResolution = 128;

        [ModOption(name: "Blaster Sound Volume", tooltip: "Volume modifier for blaster sounds.", category = "Blasters", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 3)]
        public static float fBlasterSoundVolume = 1f;

        [ModOption("Blaster Overheat Rate Multiplier", "Higher values mean faster overheating. Set to 0 to disable entirely", category = "Blasters", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 4, order = 4)]
        public static float fBlasterOverheatMultiplier = 1.0f;

        [ModOption("Infinite Ammo", "Makes every blaster have infinite ammo", category = "Blasters", defaultValueIndex = 0)]
        public static bool bInfiniteAmmo = false;

        [ModOption("Reload requires Blaster Batteries", "Disallows reloads by holding down a button. You will now be required to apply a blaster battery.", category = "Blasters", defaultValueIndex = 0)]
        public static bool bBatteryRecharg = false;

        // Blasters - Bolts
        [ModOptionCategory("Blasters - Bolts", 11)]
        [ModOption(name: "Bolt Lifetime", tooltip: "Amount in seconds that the blaster bolts stay alive.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 16, order = 1)]
        public static float fBlasterLifetime = 4f;

        [ModOption(name: "Expensive Collisions", tooltip: "Increase bolt collision accuracy", category = "Blasters - Bolts", defaultValueIndex = 1, order = 2)]
        public static bool bExpensiveBlasterCollision = true;

        [ModOption(name: "Deflect Speed Multiplier", tooltip = "The speed multiplier after a blaster bolt got deflected.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 9, order = 3)]
        public static float fDeflectSpeedMultiplier;

        [ModOption(name: "Stun Duration", tooltip: "Amount in seconds that the stun lasts.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 40, order = 4)]
        public static float fBlasterStunDuration = 10f;

        [ModOption(name: "Bolt Trail Lifetime", tooltip: "Amount in seconds that the blaster bolt trail lasts.", category = "Blasters - Bolts", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 7, order = 5)]
        public static float fBoltTrailLifetime = 0.07f;

        [ModOption(name: "Bolt Speed Multiplier", tooltip = "Multiply the speed of the blaster bolts.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 4, order = 6)]
        public static float fBoltSpeedMultiplier = 1f;

        [ModOptionSlider()]
        [ModOption(name: "Blaster Bolt Damage Multiplier", tooltip: "Damage multiplier for non-headshot hits", category = "Blasters - Bolts", valueSourceName = nameof(damageMultipliers), defaultValueIndex = 7, order = 7)]
        public static float fBlasterBoltDamageMultiplier = 0.7f;

        [ModOptionSlider()]
        [ModOption(name: "Blaster Bolt Headshot Damage Multiplier", tooltip: "Damage multiplier for headshots", category = "Blasters - Bolts", valueSourceName = nameof(damageMultipliers), defaultValueIndex = 18, order = 8)]
        public static float fBlasterBoltHeadshotDamageMultiplier = 4f;

        // Jetpack
        [ModOptionCategory("Jetpack", 12)]
        [ModOption(name: "Jetpack Deadzone", tooltip: "How far the deadzone is to start flying/thrusting.", category = "Jetpack", valueSourceName = nameof(Zero2One), defaultValueIndex = 8, order = 1)]
        public static float fJetpackDeadzone = 8f;

        [ModOption(name: "Jetpack Thrust Multiplier", tooltip: "Multiplier for the Jetpack thrust force.", category = "Jetpack", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 4, order = 2)]
        public static float fJetpackThrustMultiplier = 1f;

        [ModOption(name: "Jetpack Move Multiplier", tooltip: "Multiplier for the Jetpack move force when in the air.", category = "Jetpack", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 4, order = 3)]
        public static float fJetpackMoveForceMultiplier = 1f;

        // Binoculars
        [ModOptionCategory("Electrobinoculars", 13)]
        [ModOption("Eye Resolution", "Renderer resolution of each eye on the Electrobinoculars", category = "Electrobinoculars", valueSourceName = nameof(ScopeResolutions), defaultValueIndex = 1)]
        public static int iElectrobinocularResolution = 128;

        [ModOption(name: "Zoom Volume", tooltip: "Volume modifier for zoom sounds.", category = "Electrobinoculars", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 2)]
        public static float fZoomSoundVolume = 1f;

        // Thermal Detonator
        [ModOptionCategory("Grenades", 14)]
        [ModOption("Grenade Volume", "Volume of grenade sounds", category = "Grenades", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 17)]
        public static float fThermalSoundVolume = 1.7f;

        [ModOption("Damage Radius", "The radius in which damage from the grenade blast is applied", category = "Grenades", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10)]
        public static float fThermalDetonateRadius = 1;

        // AI
        [ModOptionCategory("AI", 15)]
        [ModOption(name: "AI Fire Mode Switch", tooltip: "Allows AI to switch fire modes on blasters that support it", category = "AI", defaultValueIndex = 1, order = 1)]
        public static bool bAIFiremode = true;

        [ModOption(name: "AI Stun Mode", tooltip: "Allows AI to use the stun mode on blasters that have it", category = "AI", defaultValueIndex = 1, order = 2)]
        public static bool bAIStunMode = true;

        [ModOption(name: "AI Single Blade on Staffs", tooltip: "Forces AI to only use one blade on dual-bladed lightsabers", category = "AI", defaultValueIndex = 0, order = 3)]
        public static bool bAIForce1Blade = false;

        [ModOption(name: "AI No Dual Wielders", tooltip: "Disallows AI to hold a second lightsaber in their off-hand", category = "AI", defaultValueIndex = 0, order = 4)]
        public static bool bAIAntiDualWielders = false;

        [ModOptionSlider]
        [ModOption("AI Blaster Accuracy", "Lower values make the AI less accurate", category = "AI", defaultValueIndex = 4, order = 5, valueSourceName = nameof(QuarterStepValues))]
        public static float fAIAccuracy = 1.0f;

        // Misc
        [ModOptionCategory("Misc", 16)]
        [ModOption(name: "Improved Physics", tooltip: "Improves the physics quality", category = "Misc", defaultValueIndex = 1, order = 0)]
        public static bool bImprovedPhysics = true;

        [ModOption(name: "Use Bloom (Requires Graphics Overhaul)", "Uses the bloom variants for lightsabers an blasters (Requires Graphics Overhaul)", category = "Misc", defaultValueIndex = 0, order = 1)]
        public static void SetUseBloom(bool b)
        {
            Global.globalUsePP = b;

            foreach (var blade in Global.allBlades)
            {
                blade.SetCrystal();
            }
        }

        [ModOption(name: "Lightsaber Shadows (Requires Graphics Overhaul)", "Casts shadows for lightsaber lights (performance heavy)", category = "Misc", defaultValueIndex = 0, order = 2)]
        public static void LightsaberShadows(bool b)
        {
            bLightsaberShadows = b;

            foreach (var blade in Global.allBlades)
            {
                blade.m_light.shadows = b ? LightShadows.Soft : LightShadows.None;
            }
        }
        public static bool bLightsaberShadows = false;

        [ModOptionButton()]
        [ModOption(name: "Clear Secrets Progress", "Resets all the discovered secret items, making you start your journey again.", category = "Misc")]
        public static void ClearSecretsProgressV(int dummy)
        {
            if (!Player.currentCreature) return;

            SecretManager.ClearSecrets();
        }

        // Maps
        [ModOptionCategory("Maps", 17)]
        [ModOption(name: "Rain Intensity", tooltip: "Intensity of the rain in the Kamino: Cloning Facility map.", category = "Maps", valueSourceName = nameof(RainIntensities), defaultValueIndex = 4, order = 1)]
        public static void SetRainIntensity(float intensity)
        {
            fKaminoRainIntensity = intensity;

            if (Level.current.mode.TryGetModule<LevelModuleKaminoFacility>(out var kamino))
            {
                kamino.SetRainIntensity(intensity);
            }
        }
        public static float fKaminoRainIntensity = 100f;
    }
}
