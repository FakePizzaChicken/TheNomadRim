using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleCustomLightsaber : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += OnSpawn;
        }

        private void OnSpawn(EventTime time)
        {
            item.GetOrAddComponent<ItemCustomLightsaber>();
            item.OnSpawnEvent -= OnSpawn;
        }
    }
}
