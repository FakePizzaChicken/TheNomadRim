using System.Collections;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleItemElectrobinocular : ItemModule
    {
        public string s_zoom_sound = "";
        public string s_unzoom_sound = "";

        public float[] f_zoom_fovs = { 120, 60, 1 };

        public AudioContainer m_zoom_sounds;
        public AudioContainer m_unzoom_sounds;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemElectrobinocular>();
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(s_zoom_sound))
                yield return Catalog.LoadAssetCoroutine(s_zoom_sound, delegate (AudioContainer x) { m_zoom_sounds = x; }, "AudioContainer");

            if (!string.IsNullOrEmpty(s_unzoom_sound))
                yield return Catalog.LoadAssetCoroutine(s_unzoom_sound, delegate (AudioContainer x) { m_unzoom_sounds = x; }, "AudioContainer");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (m_zoom_sounds != null)
                Catalog.ReleaseAsset(m_zoom_sounds);

            if (m_unzoom_sounds != null)
                Catalog.ReleaseAsset(m_unzoom_sounds);

        }
    }
}
