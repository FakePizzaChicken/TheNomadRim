using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemThermalDetonator : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public Item m_item;
        public ModuleItemThermalDetonator m_module;

        private Animator m_animator;
        private AudioSource m_audio_source;
        private AudioSource m_audio_source_tick;

        private SkinnedMeshRenderer m_renderer;

        private float t_held_timer;

        private bool b_armed = false;
        private bool b_fused = false;
        private bool b_detonated = false;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleItemThermalDetonator>();

            m_item.OnGrabEvent += OnGrabbed;
            m_item.OnUngrabEvent += OnDrop;
            m_item.OnHeldActionEvent += OnAction;

            m_animator = m_item.gameObject.GetComponent<Animator>();
            m_audio_source = m_item.gameObject.GetComponent<AudioSource>();
            m_audio_source_tick = m_item.gameObject.GetNamedChild("Tick").GetComponent<AudioSource>();

            m_renderer = m_item.gameObject.GetNamedChild("Mesh").GetComponent<SkinnedMeshRenderer>();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(block);
            block.SetFloat("_UseEmission", 0f);
            m_renderer.SetPropertyBlock(block);
        }

        protected override void ManagedUpdate()
        {
            if (t_held_timer > 0)
            {
                t_held_timer -= Time.deltaTime;

                if (t_held_timer <= 0 && b_armed && !b_fused)
                {
                    NoReturn();
                }
            }

            if (b_armed && b_fused)
            {
                if (!m_audio_source_tick.isPlaying)
                {
                    m_module.m_explosion.Spawn(transform.position, Quaternion.identity);
                    Detonate();

                    m_audio_source.Stop();
                    m_audio_source_tick.Stop();

                    Util.PlaySound(m_audio_source, m_module.m_explosion_sound, ModSettings.fThermalSoundVolume * 3);

                    b_detonated = true;
                    b_armed = false;
                    b_fused = false;
                }
            }

            if (b_detonated && !m_audio_source.isPlaying)
            {
                m_item.Despawn();
            }
        }

        //-------------------------------------------------------------------------------------------\\

        public void OnGrabbed(Handle handle, RagdollHand ragdollHand)
        {
            if (!ragdollHand.playerHand)
            {
                Arm();
            }
        }

        public void OnDrop(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (!ragdollHand.playerHand)
            {
                NoReturn();
            }
        }

        public void OnAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (b_armed && !b_fused)
                {
                    t_held_timer = 0.4f;
                }
            }

            if (action == Interactable.Action.AlternateUseStop)
            {
                if (b_fused)
                    return;

                if (b_armed)
                {
                    if (t_held_timer > 0.2f)
                    {
                        Disarm();
                    }
                }
                else
                {
                    Arm();
                }

                t_held_timer = 0;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void Arm()
        {
            m_animator.SetBool("Armed", true);

            Util.PlaySound(m_audio_source, m_module.m_arm_sound, ModSettings.fThermalSoundVolume);

            Util.PlaySoundLooped(m_audio_source_tick, m_module.m_armed_tick, ModSettings.fThermalSoundVolume);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(block);
            block.SetFloat("_UseEmission", 1f);
            m_renderer.SetPropertyBlock(block);

            b_armed = true;
            b_fused = false;
            b_detonated = false;
        }

        private void Disarm()
        {
            m_animator.SetBool("Armed", false);
            Util.PlaySound(m_audio_source, m_module.m_disarm_sound, ModSettings.fThermalSoundVolume);
            Util.StopLoopedSound(m_audio_source_tick);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(block);
            block.SetFloat("_UseEmission", 0f);
            m_renderer.SetPropertyBlock(block);

            b_armed = false;
            b_fused = false;
            b_detonated = false;
        }

        private void NoReturn()
        {
            Util.StopLoopedSound(m_audio_source_tick);
            Util.PlaySound(m_audio_source_tick, m_module.m_dangerous_armed_tick, ModSettings.fThermalSoundVolume);
            b_fused = true;
        }

        public void Detonate()
        {
            var pos = transform.position;

            var hitMask = (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << 24) | (1 << 25) | (1 << 26) | (1 << 27) | (1 << 31);
            var ignoreMask = ~((1 << 10) | (1 << 13) | (1 << 26) | (1 << 27) | (1 << 31));

            foreach (Collider collider in Physics.OverlapSphere(pos, ModSettings.fThermalDetonateRadius, hitMask, QueryTriggerInteraction.Ignore))
            {
                var body = collider.GetComponent<Rigidbody>() ?? collider.GetComponentInParent<Rigidbody>();
                var dist = Vector3.Distance(pos, collider.transform.position);

                if (!body)
                    continue;

                if (dist < 0.4f || !Physics.Linecast(pos, collider.transform.position, ignoreMask, QueryTriggerInteraction.Ignore))
                {
                    body.AddExplosionForce(10000f * ((ModSettings.fThermalDetonateRadius - dist) / ModSettings.fThermalDetonateRadius), pos, ModSettings.fThermalDetonateRadius, 1f);

                    if (collider.GetComponentInParent<Creature>())
                    {
                        var part = collider.GetComponent<RagdollPart>() ?? collider.GetComponentInParent<RagdollPart>();

                        if (part && part.sliceAllowed && !part.isSliced)
                        {
                            part.TrySlice();

                            CollisionInstance collisionInstance = new CollisionInstance(new DamageStruct(DamageType.Energy, 250), null, null);
                            part.ragdoll.creature.Damage(collisionInstance);
                        }
                    }
                }
            }
        }

    }
}
