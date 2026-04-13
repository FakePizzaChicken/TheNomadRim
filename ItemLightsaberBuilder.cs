using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;
using static TheNomadRim.ItemLightsaberBuilder;

namespace TheNomadRim
{
    public class ItemLightsaberBuilder : ThunderBehaviour
    {
        ModuleLightsaberBuilder module;
        Item item;

        Handle doorHandle;
        Animator animator;

        List<BuilderSlot> slots = new List<BuilderSlot>();

        bool isCooking = false;
        public bool isOpen = false;
        public bool isHiltInside = false;

        public enum SlotType
        {
            Emitter = 0,
            SleeveOptional,
            Switch,
            SleeveMandatory,
            Pommel
        }

        protected void Awake()
        {
            item = GetComponent<Item>();
            if (!item)
            {
                DebugService.LogError("Item component not found!");
                return;
            }

            module = item.data?.GetModule<ModuleLightsaberBuilder>();
            if (module == null)
            {
                DebugService.LogError("ModuleLightsaberBuilder not found!");
                return;
            }

            var _slots = item.gameObject.GetNamedChild("Positions");
            if (!_slots)
            {
                DebugService.LogError($"Slots object 'Positions' not found on Lightsaber Builder '{item.data.id}'");
                return;
            }

            slots.Clear();
            AddSlotIfExists(_slots, "SlotEmitter", SlotType.Emitter);
            AddSlotIfExists(_slots, "SlotSleeveTop", SlotType.SleeveOptional);
            AddSlotIfExists(_slots, "SlotSwitch", SlotType.Switch);
            AddSlotIfExists(_slots, "SlotSleeveBottom", SlotType.SleeveMandatory);
            AddSlotIfExists(_slots, "SlotPommel", SlotType.Pommel);

            if (slots.Count < 5)
            {
                DebugService.LogError($"Missing slots. Found {slots.Count}/5 slots.");
            }

            var collisionVolume = _slots.GetNamedChild("CollisionVolume");
            if (collisionVolume)
            {
                var collider = collisionVolume.GetComponent<Collider>();
                if (collider) collider.isTrigger = true;
            }

            var doorHandleRef = item.GetCustomReference("DoorHandle");
            if (doorHandleRef)
            {
                doorHandle = doorHandleRef.GetComponent<Handle>();
                if (doorHandle)
                {
                    doorHandle.OnHeldActionEvent += HandleDoorEvent;
                }
                else
                {
                    DebugService.LogError($"Handle not found for DoorHandle reference");
                }
            }
            else
            {
                DebugService.LogError($"DoorHandle not found for Lightsaber Builder '{item.data.id}'");
            }

            animator = item.GetComponent<Animator>();
            if (!animator)
            {
                DebugService.LogWarning($"Animator not found for Lightsaber Builder '{item.data.id}'");
            }

            isHiltInside = false;
        }

        private void AddSlotIfExists(GameObject parent, string childName, SlotType slotType)
        {
            var slotObject = parent.GetNamedChild(childName);
            if (slotObject)
            {
                var displayObject = slotObject.GetNamedChild("SlotDisplay");
                if (displayObject && displayObject.GetComponent<Renderer>())
                {
                    slots.Add(new BuilderSlot(slotObject, slotType));
                }
                else
                {
                    DebugService.LogError($"SlotDisplay not found or missing Renderer on {childName}");
                }
            }
            else
            {
                DebugService.LogError($"Slot {childName} not found");
            }
        }

        private void HandleDoorEvent(RagdollHand hand, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart && !isCooking)
            {
                if (doorHandle.handlers?.Count > 0)
                {
                    doorHandle.Release();
                }

                animator?.Play(isOpen ? "LSBuilderClose" : "LSBuilderOpen");
                isOpen = !isOpen;

                UpdateAllPieceHandles();

                if (GetSlotFromType(SlotType.Emitter)?.currentPiece == null ||
                    GetSlotFromType(SlotType.Switch)?.currentPiece == null ||
                    GetSlotFromType(SlotType.SleeveMandatory)?.currentPiece == null)
                {
                    DebugService.LogWarning("Missing mandatory pieces for lightsaber creation!");
                    isCooking = false;
                }
                else if (!isOpen)
                {
                    StartCoroutine(CookLightsaberCreation());
                }
            }
        }

        private void UpdateAllPieceHandles()
        {
            foreach (var slot in slots)
            {
                if (slot.currentPiece != null)
                {
                    slot.currentPiece.UpdateHandleState(isOpen);
                }
            }

            var sleeveSlot = GetSlotFromType(SlotType.SleeveMandatory);
            if (sleeveSlot?.currentLightsaber != null)
            {
                foreach (var handle in sleeveSlot.currentLightsaber.item.handles)
                {
                    handle.enabled = isOpen;
                    if (!isOpen) handle.Release();
                }
            }
        }

        public IEnumerator CookLightsaberCreation()
        {
            isCooking = true;
            yield return new WaitForSeconds(1.5f);

            string hiltToSpawn = "CustomLightsaber";
            if (GetSlotFromType(SlotType.SleeveOptional)?.currentPiece == null)
                hiltToSpawn = "CustomLightsaberShoto";

            var hiltData = Catalog.GetData<ItemData>(hiltToSpawn);
            if (hiltData == null)
            {
                DebugService.LogError($"Hilt data {hiltToSpawn} not found in catalog!");
                isCooking = false;
                yield break;
            }

            hiltData.SpawnAsync(item =>
            {
                if (!item) return;

                var customLightsaber = item.GetComponent<ItemCustomLightsaber>();
                if (!customLightsaber)
                {
                    DebugService.LogError("ItemCustomLightsaber component not found on spawned hilt!");
                    return;
                }

                var pieces = new List<ItemLightsaberPiece>();
                foreach (var slot in slots)
                {
                    if (slot.currentPiece != null)
                        pieces.Add(slot.currentPiece);
                }

                customLightsaber.BuildFromPieces(pieces);
                customLightsaber.AssignHiltToBuilder(this);
            }, null, null, null, true, null, Item.Owner.Player);

            yield return new WaitForSeconds(1.5f);
            isCooking = false;
            animator?.Play("LSBuilderOpen");
            isOpen = true;
            UpdateAllPieceHandles();
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (isHiltInside || isCooking || !isOpen) return;

            var component = other.GetComponentInParent<ItemLightsaberPiece>();
            if (component != null || component.currentHilt != null)
            {
                var currentSlot = component.AssignPieceToBuilder(this);
                if (currentSlot == null) return;
                currentSlot.currentPiece = component;
                currentSlot.UpdateColor();
            }
        }

        public void RemovePieceFromSlot(ItemLightsaberPiece piece, bool delete = false)
        {
            foreach (var slot in slots)
            {
                if (slot.currentPiece == piece)
                {
                    if (delete) slot.currentPiece.item.Despawn();
                    slot.currentPiece = null;
                    slot.UpdateColor();
                    return;
                }
            }
        }

        public void RemovePieceFromSlot(SlotType slotType, bool delete = false)
        {
            foreach (var slot in slots)
            {
                if (slot.slotType == slotType)
                {
                    if (delete && slot.currentPiece != null)
                        slot.currentPiece.item.Despawn();
                    slot.currentPiece = null;
                    slot.UpdateColor();
                    return;
                }
            }
        }

        public BuilderSlot GetFreeSlotForPieceType(ItemLightsaberPiece piece)
        {
            if (piece == null) return null;

            var pieceType = piece.pieceType;

            foreach (var slot in slots)
            {
                if (slot.currentPiece == piece)
                {
                    return null;
                }
            }

            if (pieceType == ItemLightsaberPiece.PieceType.Sleeve)
            {
                var mandatorySlot = GetSlotFromType(SlotType.SleeveMandatory);
                var optionalSlot = GetSlotFromType(SlotType.SleeveOptional);

                if (mandatorySlot?.currentPiece == null)
                    return mandatorySlot;

                if (optionalSlot?.currentPiece == null)
                    return optionalSlot;

                return null;
            }

            foreach (var slot in slots)
            {
                if (slot.currentPiece == null)
                {
                    switch (pieceType)
                    {
                        case ItemLightsaberPiece.PieceType.Emitter:
                            if (slot.slotType == SlotType.Emitter) return slot;
                            break;
                        case ItemLightsaberPiece.PieceType.Switch:
                            if (slot.slotType == SlotType.Switch) return slot;
                            break;
                        case ItemLightsaberPiece.PieceType.Pommel:
                            if (slot.slotType == SlotType.Pommel) return slot;
                            break;
                    }
                }
            }

            return null;
        }

        public BuilderSlot GetSlotFromType(SlotType type)
        {
            foreach (var slot in slots)
            {
                if (slot.slotType == type)
                    return slot;
            }
            return null;
        }

        public class BuilderSlot
        {
            public GameObject slotParent;
            public Renderer slotDisplay;

            public SlotType slotType;

            public Color slotColorOccupied = new Color(0, 0, 0, 0);
            public Color slotColorMissing = new Color(1, 0, 0, 0.5f);
            public Color slotColorOptional = new Color(1, 1, 0, 0.5f);

            public ItemLightsaberPiece currentPiece = null;
            public ItemCustomLightsaber currentLightsaber = null;

            public BuilderSlot(GameObject _slotParent, SlotType _slotType)
            {
                slotParent = _slotParent;
                slotDisplay = slotParent.GetNamedChild("SlotDisplay").GetComponent<Renderer>();
                slotType = _slotType;
                Init();
            }

            private void Init()
            {
                if (slotType == SlotType.SleeveOptional || slotType == SlotType.Pommel)
                {
                    ChangeColor(slotColorOptional);
                }
                else
                {
                    ChangeColor(slotColorMissing);
                }
            }

            public void ChangeColor(Color color)
            {
                if (slotDisplay == null) return;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                slotDisplay.GetPropertyBlock(block);

                block.SetColor("_BaseColor", color);

                slotDisplay.SetPropertyBlock(block);
            }

            public void UpdateColor()
            {
                if (currentPiece != null || currentLightsaber != null)
                {
                    ChangeColor(slotColorOccupied);
                }
                else
                {
                    if (slotType == SlotType.SleeveOptional || slotType == SlotType.Pommel)
                    {
                        ChangeColor(slotColorOptional);
                    }
                    else
                    {
                        ChangeColor(slotColorMissing);
                    }
                }
            }
        }
    }
}