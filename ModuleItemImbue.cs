using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleItemImbue : ItemModule
    {

        public string s_grip = "Handle";
        public string s_imbue_collider_group = "TipCollisions";
        public string s_spell_id = "Lightning";

        public string toggleOnAnimation = "Open";
        public string toggleOffAnimation = "Close";


        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemImbue>();
        }
    }
}
