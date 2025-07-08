using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleBlasterBolt : ItemModule
    {
        public float[] f_color = { 1, 0, 0, 1 };
        public float[] f_light = { 1, 0, 0, 1 };

        public bool b_is_stun = false;
        public bool b_use_gravity = false;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ProjectileBlasterBolt>();
        }

    }
}
