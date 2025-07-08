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
        public float[] f_color = { 1f, 1f, 1f, 1f };

        public Multicolor() { }

        public Multicolor(float[] color)
        {
            f_color = color;
        }

        public Color ToUnityColor()
        {
            if (f_color.Length >= 4)
                return new Color(f_color[0], f_color[1], f_color[2], f_color[3]);
            if (f_color.Length == 3)
                return new Color(f_color[0], f_color[1], f_color[2], 1f);
            return Color.white;
        }
    }

    public class ModuleKyberCrystal : ItemModule
    {
        //  Kyber Crystal
        public float[] f_crystal_color = { 1f, 1f, 1f, 1f };
        public float[] f_crystal_glow_color = { 1f, 1f, 1f, 1f };
        public float f_glow_intensity = 1f;

        // Lightsaber Data
        public float[] f_core_color = { 1f, 1f, 1f, 1f };
        public float[] f_glow_color = { 1f, 1f, 1f, 1f };
        public float[] f_alt_glow_color = { 1f, 1f, 1f, 1f };
        public float[] f_light_color = { 1f, 1f, 1f, 1f };

        public List<Multicolor> f_gradient_colors = new List<Multicolor>();

        public float f_mode = 0f;

        public int i_gradient_width = 32;

        public float f_scroll_speed = 0.5f;
        public float f_fade_speed = 1f;

        public float f_width = 1f;
        public float f_jitter_amount = 0.095f;

        public float f_light_intensity = 2f;
        public float f_light_range = 1f;

        public bool b_is_corrupted = false;
        public float f_crack_intensity = 1f;


        public string s_smooth_swing_low;
        public string s_smooth_swing_high;
        public string s_accent;
        public EffectData m_smooth_swing_high;
        public EffectData m_smooth_swing_low;
        public EffectData m_accent_data;

        public string s_idle_sound;
        public string s_on_sound;
        public string s_off_sound;

        public AudioContainer m_idle_container;
        public AudioContainer m_on_container;
        public AudioContainer m_off_container;

        public float f_idle_volume;
        public float f_idle_pitch = 1f;
        public float f_on_volume;
        public float f_on_pitch = 1f;
        public float f_off_volume;
        public float f_off_pitch = 1f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemKyberCrystal>();
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);
            if (!string.IsNullOrEmpty(s_smooth_swing_low))
                m_smooth_swing_low = Catalog.GetData<EffectData>(s_smooth_swing_low);
            if (!string.IsNullOrEmpty(s_smooth_swing_high))
                m_smooth_swing_high = Catalog.GetData<EffectData>(s_smooth_swing_high);
            if (!string.IsNullOrEmpty(s_accent))
                m_accent_data = Catalog.GetData<EffectData>(s_accent);
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(s_idle_sound)) 
                yield return Catalog.LoadAssetCoroutine(s_idle_sound, delegate (AudioContainer x) { m_idle_container = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_on_sound))
                yield return Catalog.LoadAssetCoroutine(s_on_sound, delegate (AudioContainer x) { m_on_container = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_off_sound))
                yield return Catalog.LoadAssetCoroutine(s_off_sound, delegate (AudioContainer x) { m_off_container = x; }, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (m_idle_container != null)
                Catalog.ReleaseAsset(m_idle_container);

            if (m_on_container != null)
                Catalog.ReleaseAsset(m_on_container);

            if (m_off_container != null)
                Catalog.ReleaseAsset(m_off_container);
        }

    }
}
