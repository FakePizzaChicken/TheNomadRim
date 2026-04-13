using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleBlasterBattery : ItemModule
    {
        public float[] indicatorGlowColor = { 1f, 1f, 1f, 1f };
        public float[] indicatorNormalColor = { 1f, 1f, 1f, 1f };

        public bool oneTimeUse = false;

        // Bolt Data

        public bool overrideProjectilesOnly;
        public string projectile;
        public string projectileOverride;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemBlasterBattery>();
        }
    }
}
