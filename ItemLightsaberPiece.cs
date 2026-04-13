using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheNomadRim
{
    public class ItemLightsaberPiece : ThunderBehaviour
    {
        public Item item;
        public Rigidbody body;
        private ModuleLightsaberPiece module;

        public enum PieceType
        {
            Emitter = 0,
            Switch,
            Sleeve,
            Pommel
        }

        public PieceType pieceType;

        public ItemLightsaberBuilder currentBuilder;
        public ItemCustomLightsaber currentHilt;
        private float lastRemovalTime;
        private const float reassignCooldown = 1.0f;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleLightsaberPiece>();
            body = item.GetComponent<Rigidbody>();

            switch (module.pieceType)
            {
                case "Emitter":
                    pieceType = PieceType.Emitter;
                    break;
                case "Switch":
                    pieceType = PieceType.Switch;
                    break;
                case "Sleeve":
                    pieceType = PieceType.Sleeve;
                    break;
                case "Pommel":
                    pieceType = PieceType.Pommel;
                    break;
                default:
                    DebugService.LogWarning($"Unknown piece type: {module.pieceType}");
                    break;
            }

            currentBuilder = null;
            lastRemovalTime = -reassignCooldown;

            item.OnGrabEvent += PieceGrabbed;
        }

        public void AssignPieceToHilt(ItemCustomLightsaber hilt)
        {
            var reference = hilt.GetReferenceForType(this.pieceType);
            if (reference == null)
            {
                DebugService.LogError($"No reference found for piece type {pieceType} on hilt!");
                return;
            }

            if (body != null)
            {
                body.mass = 0;
                body.drag = 0;
                body.angularDrag = 0;
                body.isKinematic = true;
            }

            item.transform.SetParent(reference, true);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(90, 0, 0);

            foreach (var group in item.colliderGroups)
            {
                group.colliders.ToList().ForEach(collider => collider.enabled = false);
            }

            foreach (var handle in item.handles)
            {
                handle.Release();
                handle.enabled = false;
                handle.gameObject.SetActive(false);
            }

            if (currentBuilder != null)
            {
                currentBuilder.RemovePieceFromSlot(this, false);
                currentBuilder = null;
            }

            currentHilt = hilt;

            if (!hilt.pieces.Contains(this))
            {
                hilt.pieces.Add(this);
            }

            item.SetOwner(Item.Owner.None);

            item.enabled = false;

            DebugService.LogInfo($"Piece {item.name} successfully assigned to hilt as {pieceType}");
        }

        private void PieceGrabbed(Handle handle, RagdollHand hand)
        {
            if (currentBuilder != null)
            {
                item.transform.SetParent(null);
                body.isKinematic = false;
                body.mass = item.data.mass;
                body.drag = item.data.drag;
                body.angularDrag = item.data.angularDrag;
                body.constraints = RigidbodyConstraints.None;

                foreach (var collider in item.GetComponentsInChildren<Collider>())
                {
                    foreach (var collider2 in currentBuilder.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(collider, collider2, false);
                }

                currentBuilder.RemovePieceFromSlot(this, false);
                currentBuilder = null;
                lastRemovalTime = Time.time;
            }
        }

        public ItemLightsaberBuilder.BuilderSlot AssignPieceToBuilder(ItemLightsaberBuilder builder)
        {
            if (Time.time - lastRemovalTime < reassignCooldown)
                return null;

            if (currentBuilder == builder)
                return null;

            currentBuilder = builder;

            var selectedSlot = builder.GetFreeSlotForPieceType(this);
            if (selectedSlot == null)
            {
                currentBuilder = null;
                return null;
            }

            foreach (var handle in item.handles)
            {
                handle.Release();
                handle.enabled = builder.isOpen;
            }

            foreach (var collider in item.GetComponentsInChildren<Collider>())
            {
                foreach (var collider2 in builder.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(collider, collider2, true);
            }

            item.transform.SetParent(selectedSlot.slotParent.transform, false);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            body.constraints = RigidbodyConstraints.FreezeAll;

            body.mass = 0;
            body.drag = 0;
            body.angularDrag = 0;
            body.isKinematic = true;

            return selectedSlot;
        }

        public void UpdateHandleState(bool enabled)
        {
            foreach (var handle in item.handles)
            {
                if (!enabled) handle.Release();
                handle.enabled = enabled;
            }
        }
    }
}