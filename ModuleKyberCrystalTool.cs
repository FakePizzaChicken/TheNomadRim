using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ModuleKyberCrystalTool : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemKyberCrystalTool>();
        }
    }
}
