using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class SwingSorcery : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public class SwingSorceryData
        {
            public EffectInstance m_smooth_swing_l;
            public EffectInstance m_smooth_swing_h;
            public EffectInstance m_accent_swing;

            public bool b_is_accent_swinging;

            public float f_last_swing_time;
            public float f_effect_duration;
        }

        private LightsaberBlade m_blade;
        private SwingSorceryData m_swing_sorcery;
        private Rigidbody m_body;
        private Item m_item;

        private float f_dampened_intensity_low;
        private float f_dampened_intensity_high;
        private bool b_effect_active = true;

        private Dictionary<EffectInstance, float> m_effectIntensities = new Dictionary<EffectInstance, float>();

        public void Initialize(LightsaberBlade blade, SwingSorceryData swingSorcery, Rigidbody body, Item item)
        {
            DebugService.Log("Initialize() called", "SwingSorcery Info");

            m_blade = blade;
            m_swing_sorcery = swingSorcery;
            m_body = body;
            m_item = item;

            blade.m_on_disabled_blade += OnDisabledBlade;

            // Initialize effect intensities
            if (swingSorcery.m_accent_swing != null)
            {
                swingSorcery.m_accent_swing.SetIntensity(0f);
                swingSorcery.m_accent_swing.Stop();
                m_effectIntensities[swingSorcery.m_accent_swing] = 0f;
            }

            if (swingSorcery.m_smooth_swing_l != null)
            {
                m_effectIntensities[swingSorcery.m_smooth_swing_l] = 0f;
            }

            if (swingSorcery.m_smooth_swing_h != null)
            {
                m_effectIntensities[swingSorcery.m_smooth_swing_h] = 0f;
            }

            DebugService.Log("Initialize() finished", "SwingSorcery Info");
        }

        protected override void ManagedUpdate()
        {
            if (!m_blade.b_is_active || !ModSettings.bAccentSwings)
                return;

            if (!b_effect_active || m_body.IsSleeping()) return;

            UpdateEffects();
        }

        protected override void ManagedOnDisable()
        {
            b_effect_active = false;
            OnDisabledBlade();
        }

        //-------------------------------------------------------------------------------------------\\

        private void OnDisabledBlade()
        {
            f_dampened_intensity_low = 0f;
            f_dampened_intensity_high = 0f;

            if (m_swing_sorcery.m_accent_swing != null)
            {
                m_swing_sorcery.m_accent_swing.SetIntensity(0f);
                m_swing_sorcery.m_accent_swing.Stop();
                m_effectIntensities[m_swing_sorcery.m_accent_swing] = 0f;
            }

            if (m_swing_sorcery.m_smooth_swing_l != null)
            {
                m_swing_sorcery.m_smooth_swing_l.SetIntensity(0f);
                m_swing_sorcery.m_smooth_swing_l.Stop();
                m_effectIntensities[m_swing_sorcery.m_smooth_swing_l] = 0f;
            }

            if (m_swing_sorcery.m_smooth_swing_h != null)
            {
                m_swing_sorcery.m_smooth_swing_h.SetIntensity(0f);
                m_swing_sorcery.m_smooth_swing_h.Stop();
                m_effectIntensities[m_swing_sorcery.m_smooth_swing_h] = 0f;
            }

            m_swing_sorcery.b_is_accent_swinging = false;
        }

        private void UpdateEffects()
        {
            try
            {
                float linearVel = m_body.velocity.magnitude;
                float angularVel = m_body.angularVelocity.magnitude * 0.2f;
                float velocity = Mathf.Max(linearVel, angularVel);

                UpdateAccentEffect(velocity);
                UpdateSmoothEffects(velocity);
                UpdateIdleVolume(velocity);
            }
            catch (Exception ex)
            {
                DebugService.Log($"Failed trying to update effects: {ex}", "SwingSorcery Error");
                b_effect_active = false;
            }
        }

        private void UpdateAccentEffect(float velocity)
        {
            if (m_swing_sorcery.m_accent_swing == null) return;

            if (m_swing_sorcery.b_is_accent_swinging &&
                Time.time - m_swing_sorcery.f_last_swing_time > m_swing_sorcery.f_effect_duration)
            {
                m_swing_sorcery.m_accent_swing.SetIntensity(0f);
                m_swing_sorcery.m_accent_swing.Stop();
                m_effectIntensities[m_swing_sorcery.m_accent_swing] = 0f;
                m_swing_sorcery.b_is_accent_swinging = false;
            }

            if (velocity >= ModSettings.fAccentSwingsThreshold &&
                !m_swing_sorcery.b_is_accent_swinging &&
                Time.time - m_swing_sorcery.f_last_swing_time >= 0.15f)
            {
                float accentVelocity = (velocity - ModSettings.fAccentSwingsThreshold) / ModSettings.fAccentSwingsThreshold;
                float intensity = Mathf.Clamp01(0.5f + accentVelocity);

                m_swing_sorcery.m_accent_swing.SetIntensity(intensity * ModSettings.fAccentMult);
                m_effectIntensities[m_swing_sorcery.m_accent_swing] = intensity * ModSettings.fAccentMult;

                if (m_swing_sorcery.m_accent_swing.isPlaying)
                    m_swing_sorcery.m_accent_swing.Stop();

                m_swing_sorcery.m_accent_swing.Play();

                m_swing_sorcery.f_effect_duration = GetEffectDuration(m_swing_sorcery.m_accent_swing);
                m_swing_sorcery.f_last_swing_time = Time.time;
                m_swing_sorcery.b_is_accent_swinging = true;

                PlayHaptics(intensity);
            }
        }

        private void UpdateSmoothEffects(float velocity)
        {
            float rawLow = velocity < ModSettings.fSmoothSwingThreshold ?
                           Mathf.InverseLerp(0f, ModSettings.fSmoothSwingThreshold, velocity) : 0f;
            float rawHigh = Mathf.InverseLerp(0f, ModSettings.fAccentSwingsThreshold, velocity);

            f_dampened_intensity_low = Mathf.MoveTowards(f_dampened_intensity_low, rawLow, Time.deltaTime * 4f);
            f_dampened_intensity_high = Mathf.MoveTowards(f_dampened_intensity_high, rawHigh, Time.deltaTime * 4f);

            UpdateSmoothEffect(m_swing_sorcery.m_smooth_swing_l, f_dampened_intensity_low, false);
            UpdateSmoothEffect(m_swing_sorcery.m_smooth_swing_h, f_dampened_intensity_high, true);
        }

        private void UpdateSmoothEffect(EffectInstance effect, float intensity, bool isHighEffect)
        {
            if (effect == null) return;

            if (!m_effectIntensities.ContainsKey(effect))
            {
                m_effectIntensities[effect] = 0f;
            }

            float scaledIntensity = intensity * ModSettings.fSwingMult;

            if (m_blade.i_id > 0)
            {
                scaledIntensity *= 0.5f;
            }

            if (m_blade.b_is_active)
            {
                if (!effect.isPlaying)
                {
                    effect.Play();
                    effect.SetIntensity(0f);
                    m_effectIntensities[effect] = 0f;
                }

                float minIntensity = 0.005f;
                float targetIntensity = Mathf.Max(scaledIntensity, minIntensity);

                float currentIntensity = m_effectIntensities[effect];
                float newIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 5f);

                effect.SetIntensity(newIntensity);
                m_effectIntensities[effect] = newIntensity;

            }
            else if (effect.isPlaying)
            {
                float currentIntensity = m_effectIntensities[effect];
                float newIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * 3f);

                effect.SetIntensity(newIntensity);
                m_effectIntensities[effect] = newIntensity;

                if (newIntensity < 0.01f)
                {
                    effect.Stop();
                    m_effectIntensities[effect] = 0f;
                }
            }
        }

        public void RestartEffects()
        {
            b_effect_active = true;
            f_dampened_intensity_low = 0f;
            f_dampened_intensity_high = 0f;

            if (m_swing_sorcery != null)
            {
                m_swing_sorcery.b_is_accent_swinging = false;
                m_swing_sorcery.f_last_swing_time = 0f;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateIdleVolume(float velocity)
        {
            if (m_blade.m_idle_src != null)
            {
                float humFade = Mathf.Clamp01(velocity / ModSettings.fSmoothSwingThreshold);
                float targetVolume = Mathf.Lerp(
                    m_blade.f_idle_volume * ModSettings.fLightsaberHumVolumeMult,
                    m_blade.f_idle_volume * 0.3f * ModSettings.fLightsaberHumVolumeMult,
                    humFade
                );

                if (m_blade.i_id > 0)
                {
                    targetVolume *= 0.5f;
                }

                m_blade.m_idle_src.volume = Mathf.Lerp(m_blade.m_idle_src.volume, targetVolume, Time.deltaTime * 3f);
            }
        }

        private void PlayHaptics(float intensity)
        {
            if (m_item?.handlers == null) return;

            foreach (var handler in m_item.handlers)
            {
                var ragdollHand = handler?.playerHand?.ragdollHand;
                if (ragdollHand != null)
                {
                    Util.PlayHaptic(ragdollHand, intensity);
                }
            }
        }

        private float GetEffectDuration(EffectInstance effect)
        {
            if (effect == null || effect.effects == null)
                return 0.5f;

            float maxDuration = 0.5f;

            foreach (var e in effect.effects)
            {
                if (e == null || e.gameObject == null)
                    continue;

                var sources = e.gameObject.GetComponents<AudioSource>();
                foreach (var source in sources)
                {
                    if (source.clip != null)
                    {
                        float duration = source.clip.length / Mathf.Max(source.pitch, 0.01f);
                        if (duration > maxDuration)
                            maxDuration = duration;
                    }
                }
            }

            return maxDuration;
        }
    }
}