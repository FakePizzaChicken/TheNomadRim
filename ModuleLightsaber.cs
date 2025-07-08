using ThunderRoad;
namespace TheNomadRim
{
    public class ModuleLightsaber : ItemModule
    {
        public LightsaberBlade[] m_lightsaber_blades;

        public string s_action = "actionToggle";
        public string s_held_action = "";

        public bool b_animate_on_toggle = false;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += OnSpawn;
        }

        private void OnSpawn(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
                return;
            item.gameObject.AddComponent<ItemLightsaber>();
            item.OnSpawnEvent -= OnSpawn;
        }

    }
}
