using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemBlasterBattery : ThunderBehaviour
    {
        public Item item;
        public ModuleBlasterBattery module;

        private Color indicatorGlowColor;
        private Color indicatorColor;

        public string projectileID;
        public string projectileOverride;

        public bool projectilesOnly = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ModuleBlasterBattery>();

            indicatorGlowColor = new Color(module.indicatorGlowColor[0], module.indicatorGlowColor[1], module.indicatorGlowColor[2], module.indicatorGlowColor[3]);
            indicatorColor = new Color(module.indicatorNormalColor[0], module.indicatorNormalColor[1], module.indicatorNormalColor[2], module.indicatorNormalColor[3]);

            projectileID = module.projectile;
            projectileOverride = module.projectileOverride;

            projectilesOnly = !module.overrideProjectilesOnly;

            // ToDo: Make an actual model for this someday

            GameObject mesh = item.gameObject.GetNamedChild("Mesh");
            MeshRenderer glowI = mesh.gameObject.GetNamedChild("Cube").GetComponent<MeshRenderer>();
            MeshRenderer glowI1 = mesh.gameObject.GetNamedChild("Cube (1)").GetComponent<MeshRenderer>();
            MeshRenderer glowI2 = mesh.gameObject.GetNamedChild("Cube (2)").GetComponent<MeshRenderer>();
            MeshRenderer glowI3 = mesh.gameObject.GetNamedChild("Cube (3)").GetComponent<MeshRenderer>();
            MeshRenderer normI = mesh.gameObject.GetNamedChild("Indicator").GetComponent<MeshRenderer>();

            MaterialPropertyBlock pGlow = new MaterialPropertyBlock();
            MaterialPropertyBlock pNorm = new MaterialPropertyBlock();


            if (glowI)
            {
                glowI.GetPropertyBlock(pGlow);
                pGlow.SetColor("_Color", indicatorGlowColor);
                glowI.SetPropertyBlock(pGlow);
                glowI1.SetPropertyBlock(pGlow);
                glowI2.SetPropertyBlock(pGlow);
                glowI3.SetPropertyBlock(pGlow);
            }

            if (normI)
            {
                normI.GetPropertyBlock(pNorm);
                pNorm.SetColor("_BaseColor", indicatorColor);
                normI.SetPropertyBlock(pNorm);
            }

            foreach (var handler in item.collisionHandlers)
            {
                handler.OnCollisionStartEvent += HandleCollision;
            }

        }

        void HandleCollision(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null)
                return;

            if (collisionInstance.sourceColliderGroup?.name == "BlasterBatteryCollisions" && collisionInstance.targetColliderGroup?.transform.root?.GetComponent<ItemBlaster>())
            {
                if (module.overrideProjectilesOnly)
                    collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemBlaster>().UpdateBoltOverride(projectileOverride);
                else
                    collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemBlaster>().UpdateBolts(projectileID, projectileOverride);

                var effect = Catalog.GetData<EffectData>("BoltColorChange");
                if (effect != null)
                {
                    var instance = effect.Spawn(collisionInstance.contactPoint, Quaternion.identity, null);
                    StartCoroutine(DespawnEffect(1f, instance));
                }

                if (module.oneTimeUse)
                {
                    item.ForceUngrabAll();
                    item.Despawn();
                }
            }
        }

        private IEnumerator DespawnEffect(float delay, EffectInstance instance)
        {
            yield return new WaitForSeconds(delay);
            instance.Despawn();
        }
    }
}
