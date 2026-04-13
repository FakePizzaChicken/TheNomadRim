using System.Collections.Generic;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemKyberCrystal : ThunderBehaviour
    {
        public Item item;
        public ModuleKyberCrystal module;

        public Color crystalColor;
        public Color crystalGlowColor;

        public Color coreColor;
        public Color coreColorGO;

        public Color glowColor;
        public Color glowColorGO;

        public Color altGlowColor;
        public Color lightColor;
        public Color smokeColor;

        public Texture2D gradient;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleKyberCrystal>();

            crystalColor = new Color(module.crystalColor[0], module.crystalColor[1], module.crystalColor[2], module.crystalColor.Length >= 4 ? module.crystalColor[3] : 1);
            crystalGlowColor = new Color(module.crystalGlowColor[0], module.crystalGlowColor[1], module.crystalGlowColor[2], module.crystalGlowColor.Length >= 4 ? module.crystalGlowColor[3] : 1);

            coreColor = new Color(module.coreColor[0], module.coreColor[1], module.coreColor[2], module.coreColor.Length >= 4 ? module.coreColor[3] : 1);
            coreColorGO = new Color(module.coreColorGO[0], module.coreColorGO[1], module.coreColorGO[2], module.coreColorGO.Length >= 4 ? module.coreColorGO[3] : 1);

            glowColor = new Color(module.glowColor[0], module.glowColor[1], module.glowColor[2], module.glowColor.Length >= 4 ? module.glowColor[3] : 1);
            glowColorGO = new Color(module.glowColorGO[0], module.glowColorGO[1], module.glowColorGO[2], module.glowColorGO.Length >= 4 ? module.glowColorGO[3] : 1);

            altGlowColor = new Color(module.altGlowColor[0], module.altGlowColor[1], module.altGlowColor[2], module.altGlowColor.Length >= 4 ? module.altGlowColor[3] : 1);
            lightColor = new Color(module.lightColor[0], module.lightColor[1], module.lightColor[2], module.lightColor.Length >= 4 ? module.lightColor[3] : 1);
            smokeColor = new Color(module.smokeColor[0], module.smokeColor[1], module.smokeColor[2], module.smokeColor.Length >= 4 ? module.smokeColor[3] : 1);

            UpdateAppearance();

            if (module.glowMode > 0.5 || module.useGradient)
            {
                List<Color> listColors = new List<Color>();
                foreach (var gc in module.gradientColors)
                {
                    if (gc != null)
                    {
                        listColors.Add(gc.ToUnityColor());
                    }
                }


                gradient = Util.CreateGradientTexture(listColors, module.gradientWidth);
            }

            foreach (var handler in item.collisionHandlers)
            {
                handler.OnCollisionStartEvent += HandleCollision;
            }

        }

        public void UpdateAppearance()
        {
            MeshRenderer mesh = item.gameObject.GetNamedChild("Mesh")?.GetComponent<MeshRenderer>();
            MaterialPropertyBlock kyberBlock = new MaterialPropertyBlock();

            if (mesh)
            {
                mesh.GetPropertyBlock(kyberBlock);
                kyberBlock.SetColor("_Color", crystalColor);
                kyberBlock.SetColor("_GlowColor", crystalGlowColor);
                kyberBlock.SetFloat("_GlowIntensity", module.glowIntensity);
                kyberBlock.SetFloat("_CrackedIntensity", module.crackIntensity);
                kyberBlock.SetInt("_Cracked", module.isCrystalCracked ? 1 : 0);
                mesh.SetPropertyBlock(kyberBlock);

                DebugService.LogInfo($"Updated Crystal Appearance ({module.crystalColor}, {module.crystalGlowColor})");
            }

        }

        void HandleCollision(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null)
                return;

            if (collisionInstance.sourceColliderGroup?.name == "KyberCrystalCollisions" && collisionInstance.targetColliderGroup?.name == "LightsaberHiltCollisions")
            {
                collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>().SetCrystal(this);
            }
        }
    }
}
