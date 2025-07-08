using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleItemJetpack : ItemModule
    {
        public float f_thrust = 800f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<ItemJetpack>();
        }
    }
}
