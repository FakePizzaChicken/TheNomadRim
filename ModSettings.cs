using ThunderRoad;

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

        public static ModOptionInt[] ScopeResolutions = {
            new ModOptionInt("64", 64),
            new ModOptionInt("128", 128),
            new ModOptionInt("256", 256),
            new ModOptionInt("512", 512),
            new ModOptionInt("720", 512),
            new ModOptionInt("1024", 1024),
            new ModOptionInt("1920", 1920),
            new ModOptionInt("2048", 2048)
        };


        // General
        [ModOptionCategory("General", 0)]
        [ModOption(name: "Haptic Feedback", tooltip: "Enables haptic feedback for some interactions.", category = "General", defaultValueIndex = 1)]
        public static bool bHaptics = true;
        [ModOption(name: "Lightsaber Length Adjuster Value", tooltip: "By how much the Lightsaber tool should adjust the lightsaber blade length.", category = "General", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 5, order = 2)]
        public static float fLengthAdjusterAdjusted = 0.05f;
        [ModOption(name: "Lightsaber Joining", tooltip: "Allows joining 2 seperate lightsabers together.", category = "General", defaultValueIndex = 1, order = 1)]
        public static bool bLightsaberJoining = true;


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

            foreach (var blade in Global.g_all_blades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberLightRangeMultiplier = 1f;

        [ModOption(name: "Light Intensity Multiplier", tooltip: "Multiplier for the lightsaber light intensity.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 3)]
        private static void UpdateLightIntensity(float value)
        {
            fSaberLightIntensityMultiplier = value;

            foreach (var blade in Global.g_all_blades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberLightIntensityMultiplier = 1f;

        [ModOption(name: "Glow Width", tooltip: "Set the widht of the lightsaber glow.", category = "Lightsabers - Appearance", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 4)]
        private static void UpdateLightWidth(float value)
        {
            fSaberGlowWidthMultiplier = value;

            foreach (var blade in Global.g_all_blades)
            {
                blade.SetCrystal();
            }
        }
        public static float fSaberGlowWidthMultiplier = 1f;


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

            foreach (var blade in Global.g_all_blades)
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
        [ModOption(name: "Perfect Accuracy", tooltip: "The bolts will shoot straight forward without any inaccuracies.", category = "Blasters", defaultValueIndex = 0, order = 1)]
        public static bool bPerfectAccuracy = false;

        [ModOption(name: "Scope Resolution", tooltip: "Resolution that the scope renders at", category = "Blasters", valueSourceName = nameof(ScopeResolutions), defaultValueIndex = 1, order = 2)]
        public static int iBlasterScopeResolution = 128;

        [ModOption(name: "Blaster Sound Volume", tooltip: "Volume modifier for blaster sounds.", category = "Blasters", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 3)]
        public static float fBlasterSoundVolume = 1f;

        // Blasters - Bolts
        [ModOptionCategory("Blasters - Bolts", 11)]
        [ModOption(name: "Bolt Lifetime", tooltip: "Amount in seconds that the blaster bolts stay alive.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 16, order = 1)]
        public static float fBlasterLifetime = 4f;

        [ModOption(name: "Expensive Collisions", tooltip: "Increase bolt collision accuracy", category = "Blasters - Bolts", defaultValueIndex = 0, order = 2)]
        public static bool bExpensiveBlasterCollision = false;

        [ModOption(name: "Deflect Speed Multiplier", tooltip = "The speed multiplier after a blaster bolt got deflected.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 9, order = 3)]
        public static float fDeflectSpeedMultiplier;

        [ModOption(name: "Stun Duration", tooltip: "Amount in seconds that the stun lasts.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 40, order = 4)]
        public static float fBlasterStunDuration = 10f;

        [ModOption(name: "Bolt Trail Lifetime", tooltip: "Amount in seconds that the blaster bolt trail lasts.", category = "Blasters - Bolts", valueSourceName = nameof(TrailLifetimeValues), defaultValueIndex = 7, order = 5)]
        public static float fBoltTrailLifetime = 0.07f;

        [ModOption(name: "Bolt Speed Multiplier", tooltip = "Multiply the speed of the blaster bolts.", category = "Blasters - Bolts", valueSourceName = nameof(QuarterStepValues), defaultValueIndex = 4, order = 6)]
        public static float fBoltSpeedMultiplier = 1f;

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

        [ModOption(name: "Sound Volume", tooltip: "Volume modifier for zoom sounds.", category = "Electrobinoculars", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10, order = 2)]
        public static float fZoomSoundVolume = 1f;

        // Thermal Detonator
        [ModOptionCategory("Thermal Detonator", 14)]
        [ModOption("Sound Volume", "Volume of the Thermal Detonator sounds", category = "Thermal Detonator", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 10)]
        public static float fThermalSoundVolume = 1;

        [ModOption("Damage Radius", "The radius in which damage from the thermal detonator blast is applied", category = "Thermal Detonator", valueSourceName = nameof(TenthStepValues), defaultValueIndex = 30)]
        public static float fThermalDetonateRadius = 3;

        // AI
        [ModOptionCategory("AI", 15)]
        [ModOption(name: "AI Fire Mode Switch", tooltip: "Allows AI to switch fire modes on blasters that support it", category = "AI", defaultValueIndex = 1, order = 1)]
        public static bool bAIFiremode = true;

        [ModOption(name: "AI Stun Mode", tooltip: "Allows AI to use the stun mode on blasters that have it", category = "AI", defaultValueIndex = 1, order = 2)]
        public static bool bAIStunMode = true;

        [ModOption(name: "AI One Blade for Dual Blade Lightsabers", tooltip: "Forces AI to only use one blade on dual-bladed lightsabers", category = "AI", defaultValueIndex = 0, order = 3)]
        public static bool bAIForce1Blade = false;

        [ModOption(name: "AI No Dual Wielders", tooltip: "Disallows AI to hold a second lightsaber in their off-hand", category = "AI", defaultValueIndex = 0, order = 4)]
        public static bool bAIAntiDualWielders = false;

        // Misc
        [ModOptionCategory("Misc", 16)]
        [ModOption(name: "Max Lights", tooltip: "Change the amount of lights that can affect one object", category = "Misc", defaultValueIndex = 2, order = 1, valueSourceName = nameof(MaxLights))]
        public static int iMaxLights = 2;

        [ModOption(name: "Improved Physics", tooltip: "Improves the physics quality", category = "Misc", defaultValueIndex = 1, order = 2)]
        public static bool bImprovedPhysics = true;
    }
}
