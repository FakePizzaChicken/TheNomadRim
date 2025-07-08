using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleBlaster : ItemModule
    {
        // Fire configuration

        public bool b_charge_shot;
        public float f_charge_time = 1f;
        public float f_charged_spread = 1f;
        public int i_charged_multi_shot = 0;

        public int i_num_spawn_points;
        public int i_bolts_per_shot = 1;
        public int i_burst_bolts = 3;
        public float f_batch_spread = 1.5f;
        public float f_burst_spread = 3.0f;
        public float f_burst_delay = 0.1f;
        public float f_shoot_delay;
        public float f_bullet_velocity;
        public float f_accuracy;
        public float f_recoil;
        public bool b_has_scope;
        public float[] f_scope_fovs = { 70f, 60f, 50f };
        public int[] f_blaster_modes = { 0 }; // 0 - single shot, 1 - burst, 2 - rapid fire, 3 - Stun
        public bool b_play_batch_sound_once = false;

        // Projectiles
        public string s_shoot_bolt;
        public string s_charged_projectile;
        public string s_bolt_override;
        public string s_charged_override;

        // Actions
        public string s_action = "actionShoot";
        public string s_action_held;
        public string s_action_secondary;
        public string s_action_held_secondary;

        // Audio
        public string s_shoot_sound;
        public string s_stun_sound;
        public string s_charge_sound;
        public string s_charged_shoot_sound;
        public AudioContainer m_shoot_sounds;
        public AudioContainer m_stun_sounds;
        public AudioContainer m_charge_sounds;
        public AudioContainer m_charged_shots;
        public float f_shoot_volume;

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
            if (!string.IsNullOrEmpty(s_shoot_sound))
                yield return Catalog.LoadAssetCoroutine(s_shoot_sound, (AudioContainer x) => m_shoot_sounds = x, "AudioContainer");

            if (!string.IsNullOrEmpty(s_stun_sound))
                yield return Catalog.LoadAssetCoroutine(s_stun_sound, (AudioContainer x) => m_stun_sounds = x, "AudioContainer");

            if (!string.IsNullOrEmpty(s_charge_sound))
                yield return Catalog.LoadAssetCoroutine(s_charge_sound, (AudioContainer x) => m_charge_sounds = x, "AudioContainer");

            if (!string.IsNullOrEmpty(s_charged_shoot_sound))
                yield return Catalog.LoadAssetCoroutine(s_charged_shoot_sound, (AudioContainer x) => m_charged_shots = x, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (m_shoot_sounds)
                Catalog.ReleaseAsset(m_shoot_sounds);
            if (m_stun_sounds)
                Catalog.ReleaseAsset(m_stun_sounds);
            if (m_charge_sounds)
                Catalog.ReleaseAsset(m_charge_sounds);
            if (m_charged_shots)
                Catalog.ReleaseAsset(m_charged_shots);
        }
    }
}