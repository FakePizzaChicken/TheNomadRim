using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ItemCustomSnap : ThunderBehaviour
    {
        private Item item;
        private ModuleCustomSnap module;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleCustomSnap>();
            item.OnSnapEvent += Item_OnSnapEvent;
        }

        private void Item_OnSnapEvent(Holder holder)
        {
            if (module.interactableIds.IsNullOrEmpty()) return;

            if (module.interactableIds.Contains(holder.interactableId))
            {
                item.transform.localPosition = module.snapPositionOverride;
                item.transform.localEulerAngles = module.snapRotationOverride;
            }
        }
    }
}
