using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleSaveHolder : ItemModule
    {
        public bool fillSlots = true;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += OnSpawn;
        }

        private void OnSpawn(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
                return;
            item.gameObject.AddComponent<ItemSaveHolder>();
            item.OnSpawnEvent -= OnSpawn;
        }

    }
}
