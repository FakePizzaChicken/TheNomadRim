using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ModuleCustomSnap : ItemModule
    {
        public string[] interactableIds;
        public Vector3 snapPositionOverride;
        public Vector3 snapRotationOverride;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<ItemCustomSnap>();
        }
    }
}
