using System.Linq;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemKyberCrystalTool : ThunderBehaviour
    {

        private int i_mode;

        private MeshRenderer m_mesh_renderer;

        protected void Awake()
        {
            Item item = this.GetComponent<Item>();

            m_mesh_renderer = item.gameObject.GetNamedChild("screwdriver ")?.GetComponent<MeshRenderer>();

            item.OnHeldActionEvent += ButtonPressed;

            foreach (var handler in item.collisionHandlers)
            {
                handler.OnCollisionStartEvent += HandleCollision;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            m_mesh_renderer.GetPropertyBlock(block);

            switch (i_mode)
            {
                case 0:
                    block.SetColor("_EmissionColor", new Color(9.9f, 11.025f, 1.125f, 1));
                    break;
                case 1:
                    block.SetColor("_EmissionColor", new Color(1.125f, 11.025f, 1.125f, 1));
                    break;
                case 2:
                    block.SetColor("_EmissionColor", new Color(11.025f, 1.125f, 1.125f, 1));
                    break;
                case 3:
                    block.SetColor("_EmissionColor", new Color(1.125f, 1.125f, 11.025f, 1));
                    break;
                default:
                    block.SetColor("_EmissionColor", new Color(9.9f, 11.025f, 1.125f, 1));
                    i_mode = 0;
                    break;
            }

            m_mesh_renderer.SetPropertyBlock(block);
        }

        void ButtonPressed(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStop)
            {
                if (i_mode < 4)
                {
                    i_mode++;
                }
                else
                {
                    i_mode = 0;
                }


                MaterialPropertyBlock block = new MaterialPropertyBlock();
                m_mesh_renderer.GetPropertyBlock(block);

                switch (i_mode)
                {
                    case 0:
                        block.SetColor("_EmissionColor", new Color(9.9f, 11.025f, 1.125f, 1));
                        break;
                    case 1:
                        block.SetColor("_EmissionColor", new Color(1.125f, 11.025f, 1.125f, 1));
                        break;
                    case 2:
                        block.SetColor("_EmissionColor", new Color(11.025f, 1.125f, 1.125f, 1));
                        break;
                    case 3:
                        block.SetColor("_EmissionColor", new Color(1.125f, 1.125f, 11.025f, 1));
                        break;
                    default:
                        block.SetColor("_EmissionColor", new Color(9.9f, 11.025f, 1.125f, 1));
                        i_mode = 0;
                        break;
                }

                m_mesh_renderer.SetPropertyBlock(block);
            }
        }

        void HandleCollision(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null)
                return;

            if (collisionInstance.sourceColliderGroup?.name == "KyberCrystalRemover" && collisionInstance.targetColliderGroup?.name == "LightsaberHiltCollisions")
            {
                switch (i_mode)
                {
                    case 0: // Yellow : Eject
                        collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>()?.EjectCrystal();
                        break;
                    case 1: // Green : Increase Length
                        collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>()?.IncreaseLenght();
                        break;
                    case 2: // Red : Decrease Length
                        collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>()?.DecreaseLenght();
                        break;
                    case 3: // Blue : Reset Length
                        collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>()?.ResetLength();
                        break;
                    default:
                        DebugService.LogInfo("Unknown Kyber Crystal Tool mode");
                        break;
                }
            }
        }

    }
}
