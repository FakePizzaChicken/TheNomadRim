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

            public float f_smooth_intensity_low = 0f;
            public float f_smooth_intensity_high = 0f;
        }

        private LightsaberBlade m_blade;
        private SwingSorceryData m_swing_sorcery;
        private Rigidbody m_body;
        private Item m_item;

        private float f_dampened_intensity_low;
        private float f_dampened_intensity_high;
        private bool b_effect_active = true;

        private Dictionary<EffectInstance, float> m_time_below_threshold = new Dictionary<EffectInstance, float>();

        public void Initialize(LightsaberBlade blade, SwingSorceryData swingSorcery, Rigidbody body, Item item)
        {
            DebugService.Log("Initialize() called", "SwingSorcery Info");

            m_blade = blade;
            m_swing_sorcery = swingSorcery;
            m_body = body;
            m_item = item;

            blade.m_on_disabled_blade += OnDisabledBlade;

            if (swingSorcery.m_accent_swing != null)
            {
                swingSorcery.m_accent_swing.SetIntensity(0f);
                swingSorcery.m_accent_swing.Stop();
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

            m_swing_sorcery.m_accent_swing?.SetIntensity(0f);
            m_swing_sorcery.m_accent_swing?.Stop();
            m_swing_sorcery.m_smooth_swing_l?.SetIntensity(0f);
            m_swing_sorcery.m_smooth_swing_l?.Stop();
            m_swing_sorcery.m_smooth_swing_h?.SetIntensity(0f);
            m_swing_sorcery.m_smooth_swing_h?.Stop();
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
            if (m_swing_sorcery.b_is_accent_swinging &&
                Time.time - m_swing_sorcery.f_last_swing_time > m_swing_sorcery.f_effect_duration)
            {
                m_swing_sorcery.m_accent_swing?.SetIntensity(0f);
                m_swing_sorcery.m_accent_swing?.Stop();
                m_swing_sorcery.b_is_accent_swinging = false;
            }

            if (velocity >= ModSettings.fAccentSwingsThreshold && !m_swing_sorcery.b_is_accent_swinging && Time.time - m_swing_sorcery.f_last_swing_time >= 0.15f)
            {
                float accentVelocity = (velocity - ModSettings.fAccentSwingsThreshold) / ModSettings.fAccentSwingsThreshold;
                float intensity = Mathf.Clamp01(0.5f + accentVelocity);

                m_swing_sorcery.m_accent_swing?.SetIntensity(intensity * ModSettings.fAccentMult);

                if (m_swing_sorcery.m_accent_swing.isPlaying)
                    m_swing_sorcery.m_accent_swing?.Stop();

                m_swing_sorcery.m_accent_swing?.Play();

                m_swing_sorcery.f_effect_duration = GetEffectDuration(m_swing_sorcery.m_accent_swing);
                m_swing_sorcery.f_last_swing_time = Time.time;
                m_swing_sorcery.b_is_accent_swinging = true;

                PlayHaptics(intensity);
            }
        }

        private void UpdateSmoothEffects(float velocity)
        {
            float fadeHigh = Mathf.InverseLerp(0f, ModSettings.fAccentSwingsThreshold, velocity);

            f_dampened_intensity_low = Mathf.Lerp(f_dampened_intensity_low,
                velocity < ModSettings.fSmoothSwingThreshold ?
                Mathf.InverseLerp(0f, ModSettings.fSmoothSwingThreshold, velocity) : 0f,
                Time.deltaTime * 2f);

            f_dampened_intensity_high = Mathf.Lerp(f_dampened_intensity_high,
                fadeHigh,
                Time.deltaTime * 2f);

            UpdateEffectInstance(m_swing_sorcery.m_smooth_swing_l, f_dampened_intensity_low);
            UpdateEffectInstance(m_swing_sorcery.m_smooth_swing_h, f_dampened_intensity_high);
        }

        private void UpdateEffectInstance(EffectInstance effect, float intensity)
        {
            if (effect == null) return;

            float scaledIntensity = intensity * ModSettings.fSwingMult;
            scaledIntensity /= m_blade.i_id > 0 ? 3 : 1;

            if (scaledIntensity > 0.05f)
            {
                if (!m_time_below_threshold.ContainsKey(effect))
                    m_time_below_threshold[effect] = 0;

                m_time_below_threshold[effect] = 0;

                if (!effect.isPlaying) effect.Play();
                effect.SetIntensity(scaledIntensity);
            }
            else
            {
                if (!m_time_below_threshold.ContainsKey(effect))
                    m_time_below_threshold[effect] = 0;

                m_time_below_threshold[effect] += Time.deltaTime;

                if (m_time_below_threshold[effect] > 0.45f && effect.isPlaying)
                {
                    effect.Stop();
                }
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateIdleVolume(float velocity)
        {
            if (m_blade.m_idle_src != null)
            {
                float humFade = Mathf.Clamp01(velocity / ModSettings.fSmoothSwingThreshold);
                m_blade.m_idle_src.volume = Mathf.Lerp(
                    m_blade.f_idle_volume * ModSettings.fLightsaberHumVolumeMult,
                    m_blade.f_idle_volume * 0.3f * ModSettings.fLightsaberHumVolumeMult,
                    humFade
                );

                m_blade.m_idle_src.volume /= m_blade.i_id > 0 ? 2 : 1;
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
                return 0f;

            float maxDuration = 0f;

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
