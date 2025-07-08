using System.Collections;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleItemThermalDetonator : ItemModule
    {
        public string s_arm_sound = "";
        public string s_disarm_sound = "";
        public string s_armed_tick = "";
        public string s_dangerous_armed_tick = "";
        public string s_explosion_sound = "";

        public string s_explosion_effect = "";
        public EffectData m_explosion;

        public AudioContainer m_arm_sound;
        public AudioContainer m_disarm_sound;
        public AudioContainer m_armed_tick;
        public AudioContainer m_dangerous_armed_tick;
        public AudioContainer m_explosion_sound;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemThermalDetonator>();
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);

            if (!string.IsNullOrEmpty(s_explosion_effect))
                m_explosion = Catalog.GetData<EffectData>(s_explosion_effect);
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(s_arm_sound))
                yield return Catalog.LoadAssetCoroutine(s_arm_sound, delegate (AudioContainer x) { m_arm_sound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_disarm_sound))
                yield return Catalog.LoadAssetCoroutine(s_disarm_sound, delegate (AudioContainer x) { m_disarm_sound = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_armed_tick))
                yield return Catalog.LoadAssetCoroutine(s_armed_tick, delegate (AudioContainer x) { m_armed_tick = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_dangerous_armed_tick))
                yield return Catalog.LoadAssetCoroutine(s_dangerous_armed_tick, delegate (AudioContainer x) { m_dangerous_armed_tick = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_explosion_sound))
                yield return Catalog.LoadAssetCoroutine(s_explosion_sound, delegate (AudioContainer x) { m_explosion_sound = x; }, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (m_arm_sound != null)
                Catalog.ReleaseAsset(m_arm_sound);

            if (m_disarm_sound != null)
                Catalog.ReleaseAsset(m_disarm_sound);

            if (m_armed_tick != null)
                Catalog.ReleaseAsset(m_armed_tick);

            if (m_dangerous_armed_tick != null)
                Catalog.ReleaseAsset(m_dangerous_armed_tick);

            if (m_explosion_sound != null)
                Catalog.ReleaseAsset(m_explosion_sound);

        }
    }
}
