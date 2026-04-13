using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemLiquidPhysics : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        protected Item m_item;
        protected ModuleLiquidPhysics m_module;
        protected ItemModulePotion m_potion;
        protected LiquidContainer m_liquid_container;
        public LiquidData.Content m_liquid;

        Renderer m_renderer;
        Vector3 v_last_pos;
        Vector3 v_velocity;
        Vector3 v_last_rot;
        Vector3 v_angular_velocity;

        public float f_max_wobble = 0.03f;
        public float f_wobble_speed = 2f;
        public float f_recovery = 2f;

        float f_wobble_amount_y;
        float f_wobble_amount_z;
        float f_wobble_amount_to_add_y;
        float f_wobble_amount_to_add_z;

        float f_pulse;
        float t_time = 0.5f;

        MaterialPropertyBlock m_block = new MaterialPropertyBlock();

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleLiquidPhysics>();
            m_potion = m_item.data.GetModule<ItemModulePotion>();
            m_liquid_container = m_item.GetComponent<LiquidContainer>();
            m_liquid = m_liquid_container.contents[0];

            m_renderer = m_item.GetCustomReference("Fluid").GetComponent<MeshRenderer>();

            m_renderer.GetPropertyBlock(m_block);
            m_block.SetColor("_Color", m_liquid.liquidData.color);
            m_block.SetFloat("_Fill", GetPotionFill());
            m_renderer.SetPropertyBlock(m_block);
        }

        protected override void ManagedUpdate()
        {
            UpdateLiquidRender();
        }

        float GetPotionFill()
        {
            float ret = m_liquid.level / m_potion.maxLevel;

            if (ret <= 0)
                ret = -2;
            else if (ret >= 1)
                ret = 2;

            return ret;
        }

        void UpdateLiquidRender()
        {
            if (m_renderer == null) m_renderer = GetComponent<Renderer>();

            t_time += Time.deltaTime;

            f_wobble_amount_to_add_y = Mathf.Lerp(f_wobble_amount_to_add_y, 0, Time.deltaTime * f_recovery);
            f_wobble_amount_to_add_z = Mathf.Lerp(f_wobble_amount_to_add_z, 0, Time.deltaTime * f_recovery);

            f_pulse = 2 * Mathf.PI * f_wobble_speed;
            f_wobble_amount_y = f_wobble_amount_to_add_y * Mathf.Sin(f_pulse * t_time);
            f_wobble_amount_z = f_wobble_amount_to_add_z * Mathf.Sin(f_pulse * t_time);

            Vector2 rotValues;

            Vector3 euler = transform.rotation.eulerAngles;

            float xAngle = Mathf.DeltaAngle(0, euler.x);
            float yAngle = Mathf.DeltaAngle(0, euler.z);

            rotValues.x = Mathf.Clamp01(1f - Mathf.Abs(xAngle) / 90f);
            rotValues.y = Mathf.Clamp01(1f - Mathf.Abs(yAngle) / 90f);


            if (m_renderer != null)
            {
                m_renderer.GetPropertyBlock(m_block);
                m_block.SetColor("_Color", m_liquid.liquidData.color);
                m_block.SetFloat("_Fill", GetPotionFill());
                m_block.SetFloat("_WobbleY", f_wobble_amount_y);
                m_block.SetFloat("_WobbleZ", f_wobble_amount_z);
                m_block.SetVector("_Rotation", new Vector3(1, rotValues.x, rotValues.y ));
                m_renderer.SetPropertyBlock(m_block);
            }

            Vector3 currentPos = transform.position;
            Vector3 currentRot = transform.rotation.eulerAngles;

            v_velocity = (v_last_pos - currentPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            v_angular_velocity = currentRot - v_last_rot;

            f_wobble_amount_to_add_y += Mathf.Clamp((v_velocity.x + (v_angular_velocity.z * 0.2f)) * f_max_wobble, -f_max_wobble, f_max_wobble);
            f_wobble_amount_to_add_z += Mathf.Clamp((v_velocity.z + (v_angular_velocity.x * 0.2f)) * f_max_wobble, -f_max_wobble, f_max_wobble);

            v_last_pos = currentPos;
            v_last_rot = currentRot;
        }
    }
}
