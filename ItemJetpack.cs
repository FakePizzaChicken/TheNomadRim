using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemJetpack : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        protected Item m_item;
        protected ModuleItemJetpack m_module;

        ParticleSystem m_fire_left;
        ParticleSystem m_fire_right;

        AudioSource m_audio_source;

        Creature m_creature;
        Locomotion m_locomotion;
        Rigidbody m_body;

        bool b_equipped;
        bool b_is_flying;

        float f_controller_input;
        float f_original_air_speed;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleItemJetpack>();

            m_fire_right = m_item.gameObject.GetNamedChild("FlameEffect0").GetComponent<ParticleSystem>();
            m_fire_left = m_item.gameObject.GetNamedChild("FlameEffect1").GetComponent<ParticleSystem>();
            m_audio_source = m_item.gameObject.GetNamedChild("SoundSource").GetComponent<AudioSource>();


            m_item.OnSnapEvent += OnEquip;
            m_item.OnUnSnapEvent += OnUnSnapEvent;
        }

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();

            if (m_creature == null ||
                Player.currentCreature == null ||
                Player.local == null ||
                !b_equipped) return;

            if (b_equipped)
            {
                if (PlayerControl.GetHand(Side.Right).JoystickAxis.y > ModSettings.fJetpackDeadzone)
                {
                    f_controller_input = PlayerControl.GetHand(Side.Right).JoystickAxis.y;

                    if (!b_is_flying && !m_locomotion.isGrounded)
                    {
                        EnableEffects();
                        m_locomotion.horizontalAirSpeed = 0.25f * ModSettings.fJetpackMoveForceMultiplier;
                    }
                }
                else
                    f_controller_input = 0;

                if (b_is_flying && f_controller_input != 0f)
                {
                    float force = Mathf.Lerp(m_module.f_thrust / 4f * ModSettings.fJetpackThrustMultiplier,
                        m_module.f_thrust * ModSettings.fJetpackThrustMultiplier,
                        f_controller_input);
                    m_body.AddForce(m_body.transform.up * force, ForceMode.Acceleration);

                    m_audio_source.pitch = f_controller_input;
                }
                else if (b_is_flying && f_controller_input == 0f)
                {
                    DisableEffects();
                    m_locomotion.horizontalAirSpeed = f_original_air_speed;
                }


            }
        }

        //-------------------------------------------------------------------------------------------\\

        public void OnEquip(Holder holder)
        {
            if (!holder || !holder.creature)
                return;

            m_creature = holder?.creature;
            b_equipped = m_creature == Player.currentCreature;
            if (!b_equipped)
                return;

            m_locomotion = Player.local.locomotion;
            f_original_air_speed = m_locomotion.horizontalAirSpeed;
            m_body = Player.local.locomotion.physicBody.rigidBody;
        }

        public void OnUnSnapEvent(Holder holder)
        {
            DisableEffects();
            UnEquip();
        }

        //-------------------------------------------------------------------------------------------\\

        public void UnEquip()
        {
            if (m_locomotion)
                m_locomotion.horizontalAirSpeed = f_original_air_speed;
            b_equipped = false;
            m_creature = null;
            m_body = null;
        }

        public void EnableEffects()
        {
            m_audio_source.Play();
            m_audio_source.loop = true;

            m_fire_left.Play();
            m_fire_right.Play();

            b_is_flying = true;
        }

        public void DisableEffects()
        {
            m_audio_source.Stop();

            m_fire_right.Stop();
            m_fire_left.Stop();

            b_is_flying = false;
        }
    }
}
