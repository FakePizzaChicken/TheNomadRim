using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

namespace TheNomadRim
{
    public class ItemCustomLightsaber : ThunderBehaviour
    {
        public Item item;
        public Rigidbody body;
        public ModuleCustomLightsaber module;

        public List<Transform> pieceReferences = new List<Transform>();
        public List<ItemLightsaberPiece> pieces = new List<ItemLightsaberPiece>();

        private bool isShoto = false;
        private bool hasSleeveBeenAssigned = false;

        public ItemLightsaberBuilder currentBuilder;

        protected void Awake()
        {
            item = GetComponent<Item>();
            body = item.GetComponent<Rigidbody>();
            module = item.data.GetModule<ModuleCustomLightsaber>();

            pieceReferences.Clear();

            pieceReferences.Add(item.GetCustomReference("RefEmitter"));
            pieceReferences.Add(item.GetCustomReference("RefSwitch"));
            pieceReferences.Add(item.GetCustomReference("RefSleeve"));
            if (item.GetCustomReference("RefSleeveOptional") != null)
            {
                pieceReferences.Add(item.GetCustomReference("RefSleeveOptional"));
                isShoto = false;
            }
            else
            {
                isShoto = true;
            }
            pieceReferences.Add(item.GetCustomReference("RefPommel"));

            pieces.Clear();

            hasSleeveBeenAssigned = false;

            item.OnGrabEvent += HiltGrabbed;

            LoadHiltData();
        }

        private void HiltGrabbed(Handle handle, RagdollHand hand)
        {
            if (currentBuilder != null)
            {
                item.transform.SetParent(null);
                body.constraints = RigidbodyConstraints.None;
                body.isKinematic = false;
                body.mass = item.data.mass;
                body.drag = item.data.drag;
                body.angularDrag = item.data.angularDrag;

                foreach (var collider in item.GetComponentsInChildren<Collider>())
                {
                    foreach (var collider2 in currentBuilder.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(collider, collider2, false);
                }

                currentBuilder.isHiltInside = false;

                var sleeveSlot = currentBuilder.GetSlotFromType(ItemLightsaberBuilder.SlotType.SleeveMandatory);
                if (sleeveSlot != null)
                {
                    sleeveSlot.currentLightsaber = null;
                    sleeveSlot.currentPiece = null;
                    sleeveSlot.UpdateColor();
                }

                currentBuilder = null;
            }
        }

        public Transform GetReferenceForType(ItemLightsaberPiece.PieceType type)
        {
            switch (type)
            {
                case ItemLightsaberPiece.PieceType.Emitter:
                    return pieceReferences[0];
                case ItemLightsaberPiece.PieceType.Switch:
                    return pieceReferences[1];
                case ItemLightsaberPiece.PieceType.Sleeve:
                    foreach (var piece in pieces)
                    {
                        if (piece.pieceType == ItemLightsaberPiece.PieceType.Sleeve)
                        {
                            if (isShoto || !hasSleeveBeenAssigned)
                            {
                                hasSleeveBeenAssigned = true;
                                return pieceReferences[2];
                            }
                            else
                            {
                                return pieceReferences[3];
                            }
                        }
                    }
                    return pieceReferences[2];
                case ItemLightsaberPiece.PieceType.Pommel:
                    return isShoto ? pieceReferences[3] : pieceReferences[4];
                default: return null;
            }
        }

        public void BuildFromPieces(List<ItemLightsaberPiece> pieces)
        {
            this.pieces.Clear();
            foreach (var piece in pieces)
            {
                if (piece == null) continue;
                this.pieces.Add(piece);
                piece.AssignPieceToHilt(this);
            }

            SaveHiltData();
        }

        public void AssignHiltToBuilder(ItemLightsaberBuilder builder)
        {
            var sleeveSlot = builder.GetSlotFromType(ItemLightsaberBuilder.SlotType.Switch);
            if (sleeveSlot == null) return;

            item.transform.SetParent(sleeveSlot.slotParent.transform, false);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(-90,0,0));

            body.constraints = RigidbodyConstraints.FreezeAll;
            body.isKinematic = true;
            body.mass = 0;
            body.drag = 0;
            body.angularDrag = 0;

            foreach (var collider in item.GetComponentsInChildren<Collider>())
            {
                foreach (var collider2 in builder.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(collider, collider2, true);
            }

            bool handlesEnabled = builder.isOpen;
            foreach (var handle in item.handles)
            {
                handle.enabled = handlesEnabled;
                if (!handlesEnabled) handle.Release();
            }

            currentBuilder = builder;
            currentBuilder.isHiltInside = true;

            sleeveSlot.currentLightsaber = this;
            sleeveSlot.UpdateColor();
        }

        public void SaveHiltData()
        {
            DebugService.LogInfo("Saving Hilt Data");
            CustomLightsaberData data = new CustomLightsaberData();
            data.pieceIDs = new List<string>();

            foreach (var pieceId in pieces)
            {
                data.pieceIDs.Add(pieceId.item.itemId);
            }

            Util.CleanCustomLightsaberDataProperly(item);
            item.AddCustomData(data);

            if (data == null || !item.TryGetCustomData<CustomLightsaberData>(out var _))
                DebugService.LogInfo("Failed Saving Hilt Data");
            else
            {
                DebugService.LogInfo($"Successfully Saved Hilt Data with {data.pieceIDs.Count} entries");
            }
        }

        public void LoadHiltData()
        {
            if (item.TryGetCustomData(out CustomLightsaberData data) && data != null)
            {
                DebugService.LogInfo($"Found existing Hilt Data with {data.pieceIDs.Count} entries");
                StartCoroutine(LoadPiecesCoroutine(data.pieceIDs));
            }
            else
            {
                DebugService.LogInfo("No existing hilt data found");
            }
        }

        private System.Collections.IEnumerator LoadPiecesCoroutine(List<string> pieceIDs)
        {
            List<ItemLightsaberPiece> pieceList = new List<ItemLightsaberPiece>();
            int piecesLoaded = 0;
            int totalPieces = pieceIDs.Count;

            DebugService.LogInfo("Loading pieces");

            foreach (var pieceId in pieceIDs)
            {
                ItemData itemData = Catalog.GetData<ItemData>(pieceId);
                if (itemData == null)
                {
                    DebugService.LogWarning($"Lightsaber Hilt '{item.data.id}' is missing piece '{pieceId}'!");
                    piecesLoaded++;
                    continue;
                }

                bool pieceSpawned = false;
                itemData.SpawnAsync(hiltPiece =>
                {
                    var component = hiltPiece.GetComponent<ItemLightsaberPiece>();
                    if (component != null)
                    {
                        pieceList.Add(component);
                    }
                    pieceSpawned = true;
                    piecesLoaded++;
                });

                yield return new WaitUntil(() => pieceSpawned);
            }

            yield return new WaitUntil(() => piecesLoaded >= totalPieces);

            DebugService.LogInfo($"Loaded {pieceList.Count} pieces");

            BuildFromPieces(pieceList);
        }
    }
}