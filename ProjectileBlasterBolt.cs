using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ProjectileBlasterBolt : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate;

        private Item m_item;
        private ModuleBlasterBolt m_module;
        private Rigidbody m_body;

        private MeshRenderer m_glow_renderer;
        private Light m_light;

        private TrailRenderer m_trail;

        private bool b_has_hit = false;

        private float t_despawn_time;
        private float t_deflect_time;

        private bool b_destroy = false;

        private bool b_has_deflected_recently = false;

        Color c_color;
        Color c_light_color;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleBlasterBolt>();
            m_body = GetComponent<Rigidbody>();

            if (!m_module.b_is_stun)
            {
                var glow = m_item.gameObject.GetNamedChild("Glow");
                if (glow) m_glow_renderer = glow.GetComponent<MeshRenderer>();
            }

            var lightObj = m_item.gameObject.GetNamedChild("Light");
            if (lightObj) m_light = lightObj.GetComponent<Light>();

            if (!m_module.b_is_stun)
            {
                var trail = m_item.gameObject.GetNamedChild("Trail");
                if (trail) m_trail = trail.GetComponent<TrailRenderer>();
            }

            UpdateColor();

            if (m_trail)
                m_trail.time = ModSettings.fBoltTrailLifetime;

            t_despawn_time = ModSettings.fBlasterLifetime;
            t_deflect_time = 0.1f;
            b_has_hit = false;
            b_destroy = false;
            b_has_deflected_recently = false;

            foreach (var handler in m_item.collisionHandlers)
            {
                handler.OnCollisionStartEvent += HandleCollision;
            }
        }

        protected override void ManagedFixedUpdate()
        {
            m_body.useGravity = m_module.b_use_gravity;

            if (ModSettings.bExpensiveBlasterCollision)
            {
                m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                m_body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            if (b_destroy)
            {
                DespawnBolt();
                return;
            }

            UpdateTimes();

            CheckDeflectBolt();

            if (b_has_hit || t_despawn_time <= 0)
                b_destroy = true;

            if (m_item.isTelekinesisGrabbed)
            {
                t_despawn_time = ModSettings.fBlasterLifetime;
            }

            base.ManagedUpdate();
        }

        //-------------------------------------------------------------------------------------------\\

        private void HandleCollision(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null || collisionInstance.targetCollider == null)
                return;

            if (!m_module.b_is_stun && (collisionInstance.targetCollider.material.name == "Lightsaber (Instance)" || collisionInstance.sourceCollider.material.name == "Lightsaber (Instance)"))
            {
                m_body.velocity = -m_body.velocity * ModSettings.fDeflectSpeedMultiplier;
                t_despawn_time = ModSettings.fBlasterLifetime;

                if (m_body.velocity.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(Vector3.up, m_body.velocity);
                }

                m_item.ResetObjectCollision();
                m_item.ResetColliderCollision();
                return;
            }
            else if (m_module.b_is_stun)
            {
                var creature = collisionInstance.targetCollider?.GetComponentInParent<Creature>();

                if (creature != null)
                {
                    creature.GetOrAddComponent<StunBehaviour>()?.Stun();
                    creature.GetOrAddComponent<StunGlow>()?.Glow();
                }
            }
            b_has_hit = true;

            m_item.ResetObjectCollision();
            m_item.ResetColliderCollision();
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateColor()
        {

            if (m_module.b_is_stun)
                return;

            c_color = new Color(m_module.f_color[0], m_module.f_color[1], m_module.f_color[2], m_module.f_color[3]);
            c_light_color = new Color(m_module.f_light[0], m_module.f_light[1], m_module.f_light[2], m_module.f_light[3]);

            MaterialPropertyBlock glowBlock = new MaterialPropertyBlock();
            MaterialPropertyBlock trailBlock = new MaterialPropertyBlock();

            if (m_glow_renderer)
            {
                m_glow_renderer.GetPropertyBlock(glowBlock);
                glowBlock.SetColor("_Color", c_color);
                glowBlock.SetInt("_IsCorrupted", 1);
                m_glow_renderer.SetPropertyBlock(glowBlock);
            }

            if (m_trail)
            { 
                m_trail.GetPropertyBlock(trailBlock);
                trailBlock.SetColor("_GlowColor", c_color);
                m_trail.SetPropertyBlock(trailBlock);
            }

            if (m_light)
                m_light.color = c_light_color;

        }

        private void CheckDeflectBolt()
        {
            if (m_module.b_is_stun)
                return;

            if (t_deflect_time > 0)
            {
                t_deflect_time -= Time.deltaTime;
                return;
            }

            foreach (var blade in Global.g_all_blades)
            {
                if (!blade.b_is_active)
                    continue;

                if (b_has_deflected_recently)
                {
                    t_deflect_time = 0.1f;
                    b_has_deflected_recently = false;
                    continue;
                }

                float distance = Vector3.Distance(blade.m_blade_object.transform.position, m_item.transform.position);

                if (distance <= ModSettings.fDeflectAssistRadius)
                {
                    float roll = UnityEngine.Random.Range(0f, 100f);

                    if (roll <= ModSettings.iDeflectChance)
                    {
                        m_body.velocity = -m_body.velocity * ModSettings.fDeflectSpeedMultiplier;
                        t_despawn_time = ModSettings.fBlasterLifetime;

                        if (m_body.velocity.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.LookRotation(Vector3.up, m_body.velocity);
                        }

                        b_has_hit = false;

                        GameObject obj = new GameObject("Impact");
                        obj.transform.position = m_item.transform.position;
                        obj.transform.rotation = m_item.transform.rotation;

                        if (m_body.velocity.sqrMagnitude < 0.5f)
                        {
                            b_has_hit = true;
                        }

                        PlayDeflectEffect(obj.transform);

                        m_item.ResetObjectCollision();
                        m_item.ResetColliderCollision();

                        b_has_deflected_recently = true;
                        t_deflect_time = 0.1f;

                        break;
                    }
                }
            }

        }

        private void DespawnBolt()
        {
            if (m_trail)
            {
                m_trail.time = ModSettings.fBoltTrailLifetime;
                m_trail.Clear();
            }
            t_despawn_time = ModSettings.fBlasterLifetime;
            t_deflect_time = 0.1f;
            b_has_hit = false;
            b_destroy = false;
            b_has_deflected_recently = false;
            m_item.Despawn();
        }

        private void PlayDeflectEffect(Transform transform)
        {
            var sparks = Catalog.GetData<EffectData>("LightsaberSparks").Spawn(transform);
            var sound = Catalog.GetData<EffectData>("LightsaberDeflectProjectile").Spawn(transform);

            sound?.Play();
            sparks?.Play(skipLoops: true);

            if (sparks != null)
                StartCoroutine(DespawnParticle(sparks, 0.1f));
        }

        private IEnumerator DespawnParticle(EffectInstance instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            instance.Despawn();
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateTimes()
        {
            if (t_despawn_time > 0)
            {
                t_despawn_time -= Time.deltaTime;
            }
        }
    }


    //-------------------------------------------------------------------------------------------\\


    class StunBehaviour : ThunderBehaviour
    {
        Creature m_creature;
        float f_stun_time;

        protected void Awake()
        {
            m_creature = GetComponent<Creature>();

            m_creature.OnKillEvent += OnDeath;
        }

        protected void Update()
        {
            if (m_creature.isPlayer)
            {
                m_creature.handLeft?.TryRelease();
                m_creature.handRight?.TryRelease();
                f_stun_time = 0;
                Destroy(this);
                return;
            }

            if (f_stun_time > 0)
            {
                m_creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                f_stun_time -= Time.deltaTime;
            }

            if (f_stun_time <= 0)
                Destroy(this);
        }

        private void OnDeath(CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart)
            {
                Destroy(this);
            }
        }

        public void Stun()
        {
            f_stun_time = ModSettings.fBlasterStunDuration;
        }
    }


    //-------------------------------------------------------------------------------------------\\


    public class StunGlow : ThunderBehaviour
    {
        public Dictionary<int, Color[]> m_original_emission_colors = new Dictionary<int, Color[]>();
        public Dictionary<int, float[]> m_original_emission_values = new Dictionary<int, float[]>();
        public Creature m_creature;

        protected void Awake()
        {
            if (GetComponents<StunGlow>().Length > 1)
            {
                Destroy(this);
            }
        }

        public void Glow()
        {
            m_creature = GetComponent<Creature>();

            if (m_creature)
            {
                Color glowColor = new Color(0f, 0.92f, 1.74f);

                SaveOriginalEmissionColors(m_creature.renderers);
                SetEmissionColor(m_creature.renderers, glowColor, 1f);
            }

            StartCoroutine(RestoreAfter(0.1f));
        }

        private IEnumerator RestoreAfter(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (m_creature)
                RestoreEmissionColors(m_creature.renderers);

            Destroy(this);
        }

        //-------------------------------------------------------------------------------------------\\

        private void SaveOriginalEmissionColors(List<Creature.RendererData> renderers)
        {
            foreach (var rendererData in renderers)
            {
                var renderer = rendererData.renderer;
                int key = renderer.GetHashCode();
                Material[] materials = renderer.materials;

                Color[] colors = new Color[materials.Length];
                float[] emissions = new float[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {

                    colors[i] = materials[i].GetColor("_EmissionColor");
                    emissions[i] = materials[i].GetFloat("_UseEmission");
                }

                m_original_emission_colors[key] = colors;
                m_original_emission_values[key] = emissions;
            }
        }

        private void SetEmissionColor(List<Creature.RendererData> renderers, Color color, float emission)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (var rendererData in renderers)
            {
                var renderer = rendererData.renderer;
                int materialCount = renderer.sharedMaterials.Length;

                for (int i = 0; i < materialCount; i++)
                {
                    block.Clear();
                    renderer.GetPropertyBlock(block, i);

                    block.SetFloat("_UseEmission", emission);
                    block.SetColor("_EmissionColor", color);

                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        private void RestoreEmissionColors(List<Creature.RendererData> renderers)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (var rendererData in renderers)
            {
                var renderer = rendererData.renderer;
                int key = renderer.GetHashCode();

                if (m_original_emission_colors.TryGetValue(key, out var colors) && m_original_emission_values.TryGetValue(key, out var emission))
                {
                    int materialCount = renderer.sharedMaterials.Length;

                    for (int i = 0; i < materialCount && i < colors.Length; i++)
                    {
                        block.Clear();
                        block.SetColor("_EmissionColor", colors[i]);
                        block.SetFloat("_UseEmission", emission[i]);
                        renderer.SetPropertyBlock(block, i);
                    }
                }
            }
        }
    }
}