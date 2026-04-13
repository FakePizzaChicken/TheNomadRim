using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleLightsaberPiece : ItemModule
    {

        public string pieceType = ""; // Emitter, Switch, Sleeve, Pommel

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.OnSpawnEvent += OnSpawn;
        }

        private void OnSpawn(EventTime eventTime)
        {
            item.GetOrAddComponent<ItemLightsaberPiece>();
            item.OnSpawnEvent -= OnSpawn;
        }
    }
}
