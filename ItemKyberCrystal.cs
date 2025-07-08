using System.Collections.Generic;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemKyberCrystal : ThunderBehaviour
    {

        public Item m_item;
        public ModuleKyberCrystal m_module;

        public Color c_core_color;
        public Color c_glow_color;
        public Color c_alt_glow_color;
        public Color c_light_color;

        public Texture2D m_gradient;

        public string s_id;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleKyberCrystal>();

            c_core_color = new Color(m_module.f_core_color[0], m_module.f_core_color[1], m_module.f_core_color[2], m_module.f_core_color[3]);
            c_glow_color = new Color(m_module.f_glow_color[0], m_module.f_glow_color[1], m_module.f_glow_color[2], m_module.f_glow_color[3]);
            c_alt_glow_color = new Color(m_module.f_alt_glow_color[0], m_module.f_alt_glow_color[1], m_module.f_alt_glow_color[2], m_module.f_alt_glow_color[3]);
            c_light_color = new Color(m_module.f_light_color[0], m_module.f_light_color[1], m_module.f_light_color[2], m_module.f_light_color[3]);

            if (m_module.f_mode > 0.5)
            {
                List<Color> listColors = new List<Color>();
                foreach (var gc in m_module.f_gradient_colors)
                {
                    if (gc != null)
                    {
                        listColors.Add(gc.ToUnityColor());
                    }
                }


                m_gradient = Util.CreateGradientTexture(listColors, m_module.i_gradient_width);
            }

            MeshRenderer mesh = m_item.gameObject.GetNamedChild("Mesh")?.GetComponent<MeshRenderer>();
            MaterialPropertyBlock kyberBlock = new MaterialPropertyBlock();

            Color kyberCrystalColor = new Color(m_module.f_crystal_color[0], m_module.f_crystal_color[1], m_module.f_crystal_color[2], m_module.f_crystal_color[3]);
            Color kyberCrystalGlow = new Color(m_module.f_crystal_glow_color[0], m_module.f_crystal_glow_color[1], m_module.f_crystal_glow_color[2], m_module.f_crystal_glow_color[3]);

            if (mesh)
            {
                mesh.GetPropertyBlock(kyberBlock);
                kyberBlock.SetColor("_Color", kyberCrystalColor);
                kyberBlock.SetColor("_GlowColor", kyberCrystalGlow);
                kyberBlock.SetFloat("_GlowIntensity", m_module.f_glow_intensity);
                kyberBlock.SetFloat("_CrackedIntensity", m_module.f_crack_intensity);
                kyberBlock.SetInt("_Cracked", m_module.b_is_corrupted ? 1 : 0);
                mesh.SetPropertyBlock(kyberBlock);
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

            if (collisionInstance.sourceColliderGroup?.name == "KyberCrystalCollisions" && collisionInstance.targetColliderGroup?.name == "LightsaberHiltCollisions")
            {
                collisionInstance.targetColliderGroup.transform.root?.GetComponent<ItemLightsaber>().SetCrystal(this);
            }
        }
    }
}
