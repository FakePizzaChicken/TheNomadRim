using System.Collections;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleItemJetpack : ItemModule
    {
        public float maxThrust = 800f;

        public string startSound;
        public string stopSound;
        public string loopSound;

        public AudioContainer startSoundContainer, stopSoundContainer, loopSoundContainer;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<ItemJetpack>();
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            if (!string.IsNullOrEmpty(startSound))
                yield return Catalog.LoadAssetCoroutine<AudioContainer>(startSound, x => { startSoundContainer = x; }, "sound");
            if (!string.IsNullOrEmpty(stopSound))
                yield return Catalog.LoadAssetCoroutine<AudioContainer>(stopSound, x => { stopSoundContainer = x; }, "sound");
            if (!string.IsNullOrEmpty(loopSound))
                yield return Catalog.LoadAssetCoroutine<AudioContainer>(loopSound, x => { loopSoundContainer = x; }, "sound");

            yield return base.LoadAddressableAssetsCoroutine(data);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (startSoundContainer != null)
                Catalog.ReleaseAsset(startSoundContainer);
            if (stopSoundContainer != null)
                Catalog.ReleaseAsset(stopSoundContainer);
            if (loopSoundContainer != null)
                Catalog.ReleaseAsset(loopSoundContainer);
        }
    }
}
