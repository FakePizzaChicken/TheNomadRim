using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemSaveHolder : ThunderBehaviour
    {
        private Item m_item;
        private ModuleSaveHolder m_module;
        private List<Holder> m_holders = new List<Holder>();
        private bool b_is_restoring;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleSaveHolder>();


            EventManager.onUnpossess += YasterMogurtCatchingSillyCatEatChickenNuggetsAndThereforeCompletelyObliteratingHimFunction;

            DebugService.LogInfo("Searching for holders ...");

            for (int i = 0; i < m_item.childHolders.Count; i++)
            {
                Holder holder = m_item.childHolders[i];
                if (holder != null)
                {
                    m_holders.Add(holder);


                    if (m_module.fillSlots)
                    {
                        Holder hold = holder.transform.GetComponent<Holder>();

                        Transform main_slot = hold.slots.FirstOrDefault();

                        hold.slots.Clear();

                        for (int j = 0; j < holder.data.maxQuantity; j++)
                        {
                            hold.slots.Add(main_slot);
                        }
                    }

                    holder.Snapped += OnItemSnapped;
                    holder.UnSnapped += OnItemUnsnapped;
                    DebugService.LogInfo($"Holder found : {holder.name}");
                }
            }

            LoadSavedItems();
        }


        private void LoadSavedItems()
        {
            try
            {
                if (!m_item.TryGetCustomData<ItemSaveHolderData>(out var saveData) || saveData == null || saveData.m_holder_items == null)
                {
                    return;
                }

                DebugService.LogInfo($"Loading holder with {saveData.m_holder_items.Count} saved items");

                b_is_restoring = true;

                for (int i = 0; i < saveData.m_holder_items.Count; i++)
                {
                    HolderItemData itemData = saveData.m_holder_items[i];

                    Holder targetHolder = null;
                    for (int j = 0; j < m_holders.Count; j++)
                    {
                        if (m_holders[j].transform.name == itemData.s_holder_name)
                        {
                            targetHolder = m_holders[j];
                            break;
                        }
                    }

                    if (targetHolder == null)
                    {
                        continue;
                    }

                    ItemData data = Catalog.GetData<ItemData>(itemData.s_item_id);
                    if (data == null)
                    {
                        continue;
                    }

                    data.SpawnAsync(spawnedItem =>
                    {
                        spawnedItem.SetOwner(Item.Owner.Player);

                        if (itemData.m_custom_data != null && itemData.m_custom_data.Count > 0)
                        {
                            spawnedItem.OnSpawn(itemData.m_custom_data, Item.Owner.Player);
                        }

                        targetHolder.Snap(spawnedItem, true);
                    }, customDataList: itemData.m_custom_data);
                }

                b_is_restoring = false;
            }
            catch (Exception e)
            {
            }

        }

        private void UpdateCustomData()
        {
            if (b_is_restoring)
                return;

            ItemSaveHolderData saveData = new ItemSaveHolderData();
            Util.CleanCustomSaveHolderDataProperly(m_item);

            for (int i = 0; i < m_holders.Count; i++)
            {
                Holder holder = m_holders[i];
                if (holder == null)
                {
                    continue;
                }

                foreach (Item item in holder.items)
                {
                    if (item != null)
                    {
                        if (item.holder != holder)
                            continue;

                        if (item.data != null)
                        {
                            saveData.m_holder_items.Add(new HolderItemData
                            {
                                s_holder_name = holder.transform.name,
                                s_item_id = item.itemId,
                                m_custom_data = item.contentCustomData
                            });
                        }
                    }
                }
            }

            m_item.AddCustomData(saveData);
            DebugService.LogInfo($"Finished Refreshing Custom Data");
        }

        private void OnItemSnapped(Item snappedItem)
        {
            if (snappedItem == null || b_is_restoring) return;
            UpdateCustomData();
        }

        private void OnItemUnsnapped(Item unsnappedItem)
        {
            if (unsnappedItem == null || b_is_restoring) return;
            UpdateCustomData();
        }

        // Only beta testers know the origin of this name
        private void YasterMogurtCatchingSillyCatEatChickenNuggetsAndThereforeCompletelyObliteratingHimFunction(Creature SillyCatsSoullessBody, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart && SillyCatsSoullessBody == Player.currentCreature)
            {
                foreach (var holder in m_holders)
                {
                    holder.Snapped -= OnItemSnapped;
                    holder.UnSnapped -= OnItemUnsnapped;
                }
            }
        }
    }
}