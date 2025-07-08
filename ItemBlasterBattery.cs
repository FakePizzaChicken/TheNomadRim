using System;
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
        public Item m_item;
        public ModuleBlasterBattery m_module;

        private Color c_indi_glow_color;
        private Color c_indi_norm_color;

        public string s_id;

        protected void Awake()
        {
            m_item = this.GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleBlasterBattery>();

            c_indi_glow_color = new Color(m_module.f_indicator_glow_color[0], m_module.f_indicator_glow_color[1], m_module.f_indicator_glow_color[2], m_module.f_indicator_glow_color[3]);
            c_indi_norm_color = new Color(m_module.f_indicator_normal_color[0], m_module.f_indicator_normal_color[1], m_module.f_indicator_normal_color[2], m_module.f_indicator_normal_color[3]);

            s_id = m_module.s_projectile;

            GameObject mesh = m_item.gameObject.GetNamedChild("Mesh");
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
                pGlow.SetColor("_Color", c_indi_glow_color);
                glowI.SetPropertyBlock(pGlow);
                glowI1.SetPropertyBlock(pGlow);
                glowI2.SetPropertyBlock(pGlow);
                glowI3.SetPropertyBlock(pGlow);
            }

            if (normI)
            {
                normI.GetPropertyBlock(pNorm);
                pNorm.SetColor("_BaseColor", c_indi_norm_color);
                normI.SetPropertyBlock(pNorm);
            }

            foreach (var handler in m_item.collisionHandlers)
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
                collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemBlaster>().SetColors(s_id);
            }
        }
    }
}
