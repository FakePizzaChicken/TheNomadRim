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
        public float[] f_indicator_glow_color = { 1f, 1f, 1f, 1f };
        public float[] f_indicator_normal_color = { 1f, 1f, 1f, 1f };

        // Bolt Data

        public string s_projectile;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemBlasterBattery>();
        }
    }
}
