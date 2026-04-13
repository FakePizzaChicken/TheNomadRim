using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleLightsaberBuilder : ItemModule
    {

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += Item_OnSpawnEvent;
        }

        private void Item_OnSpawnEvent(EventTime eventTime)
        {
            item.GetOrAddComponent<ItemLightsaberBuilder>();
            item.OnSpawnEvent -= Item_OnSpawnEvent;
        }
    }
}
