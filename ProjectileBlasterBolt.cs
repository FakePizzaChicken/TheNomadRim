using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace TheNomadRim
{
    public class ProjectileBlasterBolt : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate;

        private Item item;
        private ModuleBlasterBolt module;
        private Rigidbody body;

        private MeshRenderer glowRenderer;
        private MeshRenderer meshRenderer;
        private Light light;

        private TrailRenderer trail;

        private bool hasHit = false;

        private float despawnTime;
        private float deflectTime;

        private bool destroy = false;
        private bool destroyThisTick = false;

        private bool hasDeflectedRecently = false;

        Color color;
        Color colorGO;
        Color coreColorGO;
        Color lightColor;

        float coreIntensity;
        float glowIntensity;

        int ricochets = 0;

        private float damage = -1;

        private EffectData disintegrationEffect;
        private EffectData impactEffect;

        private float blastRadius;
        private float blastRadiusDamage;
        private float blastRadiusForce;
        private string blastRadiusStatusEffect;
        private float blastRadiusStatusEffectDuration;
        private bool removeLimbs;
        private int maxLimbLimit;

        private Vector3 baseScale;
        private float baseTrailWidth;

        private List<DamagerData> originalDamagers = new List<DamagerData>();

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleBlasterBolt>();
            body = GetComponent<Rigidbody>();

            baseScale = gameObject.transform.localScale;

            if (!module.isStun)
            {
                var glow = item.gameObject.GetNamedChild("Glow");
                if (glow) glowRenderer = glow.GetComponent<MeshRenderer>();
            }

            var mesh = item.gameObject.GetNamedChild("Mesh");
            if (mesh) meshRenderer = mesh.GetComponent<MeshRenderer>();

            var lightObj = item.gameObject.GetNamedChild("Light");
            if (lightObj) light = lightObj.GetComponent<Light>();

            if (!module.isStun)
            {
                var trail = item.gameObject.GetNamedChild("Trail");
                if (trail) this.trail = trail.GetComponent<TrailRenderer>();
            }

            UpdateColor();


            if (trail)
            {
                trail.time = ModSettings.fBoltTrailLifetime;
                baseTrailWidth = trail.widthMultiplier;

                trail.Clear();
            }

            ricochets = module.bounces;
            despawnTime = ModSettings.fBlasterLifetime;
            deflectTime = 0.03f;
            hasHit = false;
            destroy = false;
            destroyThisTick = false;
            hasDeflectedRecently = false;

            ToggleDamagers(true);

            foreach (var handler in item.collisionHandlers)
            {
                foreach (var dmg in handler.damagers)
                {
                    originalDamagers.Add(dmg.data);
                    foreach (var dmg2 in dmg.data.tiers)
                    {
                        dmg2.damageMultiplier = 0f;
                        dmg2.playerDamageMultiplier = 0f;
                    }
                }

                handler.OnCollisionStartEvent += HandleCollision;
                handler.checkMinVelocity = false;
            }

            damage = originalDamagers.FirstOrDefault().tiers.FirstOrDefault().damageMultiplier;
        }

        private void ToggleDamagers(bool toggle)
        {
            foreach (var handler in item.collisionHandlers)
            {
                foreach (var dmg in handler.damagers)
                {
                    dmg.enabled = toggle;
                }
            }
        }

        public void UpdateBoltData(string data)
        {
            var boltData = Catalog.GetData<BlasterBoltData>(data);
            if (boltData == null) return;
            UpdateBoltData(boltData);
        }

        private void UpdateBoltData(BlasterBoltData data)
        {
            module.bounces = data.bounces;
            module.isStun = data.isStun;
            module.useGravity = data.useGravity;
            module.disintegrate = data.disintegrate;

            damage = module.baseDamage * data.damageMultiplier;

            if (data.overrideColorData)
            {
                color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);

                coreColorGO = new Color(data.coreColorGO[0], data.coreColorGO[1], data.coreColorGO[2], 1);
                coreIntensity = data.coreColorGO[3];

                colorGO = new Color(data.colorGO[0], data.colorGO[1], data.colorGO[2], 1);
                glowIntensity = data.colorGO[3];

                lightColor = new Color(data.lightColor[0], data.lightColor[1], data.lightColor[2], data.lightColor[3]);

                UpdateColor(false);
            }
            else UpdateColor();

            gameObject.transform.localScale = baseScale * data.boltSizeMultiplier;

            if (trail) trail.widthMultiplier = baseTrailWidth * data.boltSizeMultiplier;
            disintegrationEffect = data.disintegrateEffect;
            impactEffect = data.impactEffect;

            blastRadius = data.blastRadius;
            blastRadiusDamage = data.blastRadiusDamage;
            blastRadiusForce = data.blastRadiusForce;
            blastRadiusStatusEffect = data.blastRadiusStatusEffect;
            blastRadiusStatusEffectDuration = data.blastRadiusStatusEffectDuration;
            removeLimbs = data.removeLimbs;
            maxLimbLimit = data.maxLimbLimit;

            ricochets = module.bounces;
        }

        protected override void ManagedFixedUpdate()
        {
            body.useGravity = module.useGravity;

            if (ModSettings.bExpensiveBlasterCollision)
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            if (destroy)
            {
                ToggleDamagers(false);
                if (destroyThisTick)
                {
                    DespawnBolt();
                    return;
                }
                destroyThisTick = true;
            }

            UpdateTimes();

            CheckDeflectBolt();

            if (hasHit || despawnTime <= 0)
                destroy = true;

            if (item.isTelekinesisGrabbed)
            {
                despawnTime = ModSettings.fBlasterLifetime;
            }

            base.ManagedUpdate();
        }

        //-------------------------------------------------------------------------------------------\\

        private void HandleCollision(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null || collisionInstance.targetCollider == null)
                return;

            if (!module.disintegrate && !module.isStun && (collisionInstance.targetCollider.material.name == "Lightsaber (Instance)" || collisionInstance.sourceCollider.material.name == "Lightsaber (Instance)"))
            {
                body.velocity = -body.velocity * ModSettings.fDeflectSpeedMultiplier;
                despawnTime = ModSettings.fBlasterLifetime;

                if (body.velocity.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(Vector3.up, body.velocity);
                }

                item.ResetObjectCollision();
                item.ResetColliderCollision();
                return;
            }
            else if (module.disintegrate)
            {
                var creature = collisionInstance.targetCollider?.GetComponentInParent<Creature>();

                if (creature != null)
                {
                    creature.handLeft?.TryRelease();
                    creature.handRight?.TryRelease();

                    creature.holders.ForEach(holder => holder.UnSnapAll());

                    if (disintegrationEffect != null)
                    {
                        disintegrationEffect.Spawn(position: collisionInstance.contactPoint, rotation: Quaternion.identity);
                    }

                    creature.Despawn();
                }

                hasHit = true;
            }
            else if (module.isStun)
            {
                var creature = collisionInstance.targetCollider?.GetComponentInParent<Creature>();

                if (creature != null)
                {
                    creature.GetOrAddComponent<StunBehaviour>()?.Stun();
                    creature.GetOrAddComponent<StunGlow>()?.Glow();
                }

                if (ricochets > 0)
                {
                    bool bounced = (Vector3.Angle(body.velocity, -collisionInstance.contactNormal) - 90) < 25;
                    ricochets--;

                    if (bounced)
                        body.velocity = Vector3.Reflect(body.velocity, collisionInstance.contactNormal) * 0.86f;
                    else
                        hasHit = true;
                }
                else hasHit = true;
            }
            else if (collisionInstance.damageStruct.hitRagdollPart != null)
            {
                var hitPart = collisionInstance.damageStruct.hitRagdollPart;
                if (hitPart?.ragdoll?.creature == null) { hasHit = true; return; }
                var crt = hitPart.ragdoll.creature;

                var equipments = crt.equipment?.GetWornContents();
                float lastDefense = 1;
                if (!equipments.IsNullOrEmpty())
                {
                    foreach (var eq in equipments)
                    {
                        if (eq?.data == null) continue;
                        var stats = eq.data.GetModule<ItemModuleStats>();
                        if (stats == null) continue;
                        if (stats.TryGetStat("Defense", out var stat))
                        {
                            ItemStatInt itemStat = stat as ItemStatInt;
                            if (itemStat != null && itemStat.value > lastDefense)
                                lastDefense = itemStat.value;
                        }
                    }
                }

                bool isHead = hitPart.type == RagdollPart.Type.Head || hitPart.type == RagdollPart.Type.Neck;
                var dmg = isHead ? 
                    (damage / lastDefense) * ModSettings.fBlasterBoltHeadshotDamageMultiplier :
                    (damage / lastDefense) * ModSettings.fBlasterBoltDamageMultiplier;
                crt.Damage(dmg);

                hasHit = true;
            }
            else if (ricochets > 0)
            {
                bool bounced = (Vector3.Angle(body.velocity, -collisionInstance.contactNormal) - 90) < 25;
                ricochets--;

                if (bounced)
                    body.velocity = Vector3.Reflect(body.velocity, collisionInstance.contactNormal) * 0.86f;
                else
                    hasHit = true;
            }
            else
            {
                if (impactEffect != null)
                {
                    impactEffect.Spawn(position: collisionInstance.contactPoint, rotation: Quaternion.identity);
                }

                ApplyBlastRadius(collisionInstance.contactPoint);

                hasHit = true;
            }

            item.ResetObjectCollision();
            item.ResetColliderCollision();
        }

        private void ApplyBlastRadius(Vector3 pos)
        {
            if (blastRadius <= 0) return;

            var hitMask = (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << 24) | (1 << 25) | (1 << 26) | (1 << 27) | (1 << 31);
            var ignoreMask = ~((1 << 10) | (1 << 13) | (1 << 26) | (1 << 27) | (1 << 31));

            List<RagdollPart> affectedLimbs = new List<RagdollPart>();
            List<Creature> affectedCreatures = new List<Creature>();

            foreach (Collider collider in Physics.OverlapSphere(pos, blastRadius, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (collider == null)
                {
                    continue;
                }

                var body = collider.GetComponent<Rigidbody>() ?? collider.GetComponentInParent<Rigidbody>();
                var dist = Vector3.Distance(pos, collider.transform.position);

                if (!body)
                    continue;

                if (dist < 0.4f || !Physics.Linecast(pos, collider.transform.position, ignoreMask, QueryTriggerInteraction.Ignore))
                {
                    Creature crt = collider.GetComponentInParent<Creature>();

                    if (crt == Player.currentCreature && Player.invincibility)
                    {
                        continue;
                    }

                    Item itemObj = collider.GetComponentInParent<Item>();
                    GolemCrystal crystal = collider.GetComponentInParent<GolemCrystal>();
                    Breakable breakable = collider.GetComponentInParent<Breakable>();

                    if (crt != null)
                    {
                        if (!affectedCreatures.Contains(crt))
                        {
                            affectedCreatures.Add(crt);

                            var damageMultiplier = 1-(dist / blastRadius);
                            crt.Damage(blastRadiusDamage*damageMultiplier);
                        }

                        if (string.IsNullOrEmpty(blastRadiusStatusEffect))
                        {
                            crt.Inflict(blastRadiusStatusEffect, this, blastRadiusStatusEffectDuration);
                        }

                        if (removeLimbs)
                        {
                            var rp = collider.GetComponent<RagdollPart>() ?? collider.GetComponentInParent<RagdollPart>();
                            if (rp && rp.sliceAllowed && !rp.isSliced && !affectedLimbs.Contains(rp) && affectedLimbs.Count < maxLimbLimit)
                            {
                                affectedLimbs.Add(rp);
                            }
                        }

                        crt.ragdoll.SetState(Ragdoll.State.Destabilized);

                        if (breakable) breakable.Break();
                        if (crystal) crystal.Break();

                        body.AddExplosionForce(blastRadiusForce * ((blastRadius - dist) / blastRadius), pos, blastRadius, 1f);
                    }
                }
                StartCoroutine(DismemberCreature(affectedLimbs));
            }
        }

        private IEnumerator DismemberCreature(List<RagdollPart> limbs)
        {
            Dictionary<RagdollPart.Type, float> thresholds = new Dictionary<RagdollPart.Type, float>()
            {
                { RagdollPart.Type.Head, 0.40f },
                { RagdollPart.Type.Neck, 0.40f },
                { RagdollPart.Type.LeftArm, 0.60f },
                { RagdollPart.Type.RightArm, 0.60f },
                { RagdollPart.Type.LeftHand, 0.30f },
                { RagdollPart.Type.RightHand, 0.30f },
                { RagdollPart.Type.LeftLeg, 0.55f },
                { RagdollPart.Type.RightLeg, 0.55f },
                { RagdollPart.Type.LeftFoot, 0.35f },
                { RagdollPart.Type.RightFoot, 0.35f }
            };

            var valid = new List<(RagdollPart part, float mult)>();
            foreach (var part in limbs)
            {
                if (part == null || part.isSliced) continue;
                if (!thresholds.ContainsKey(part.type)) continue;

                float dist = Vector3.Distance(part.transform.position, transform.position);
                float mult = (blastRadius - dist) / blastRadius;

                if (mult >= thresholds[part.type])
                    valid.Add((part, mult));
            }

            int GetPriority(RagdollPart.Type type)
            {
                if (type == RagdollPart.Type.LeftArm || type == RagdollPart.Type.RightArm ||
                    type == RagdollPart.Type.LeftLeg || type == RagdollPart.Type.RightLeg)
                    return 3;

                if (type == RagdollPart.Type.LeftHand || type == RagdollPart.Type.RightHand ||
                    type == RagdollPart.Type.LeftFoot || type == RagdollPart.Type.RightFoot)
                    return 2;

                if (type == RagdollPart.Type.Head || type == RagdollPart.Type.Neck)
                    return 1;

                return 0;
            }

            valid.Sort((a, b) =>
            {
                int prioA = GetPriority(a.part.type);
                int prioB = GetPriority(b.part.type);

                if (prioB != prioA) return prioB.CompareTo(prioA);
                return b.mult.CompareTo(a.mult);
            });

            var slicedParts = new HashSet<RagdollPart.Type>();

            foreach (var entry in valid)
            {
                if (entry.part == null || entry.part.isSliced) continue;

                if ((entry.part.type == RagdollPart.Type.LeftHand && slicedParts.Contains(RagdollPart.Type.LeftArm)) ||
                    (entry.part.type == RagdollPart.Type.RightHand && slicedParts.Contains(RagdollPart.Type.RightArm)) ||
                    (entry.part.type == RagdollPart.Type.LeftFoot && slicedParts.Contains(RagdollPart.Type.LeftLeg)) ||
                    (entry.part.type == RagdollPart.Type.RightFoot && slicedParts.Contains(RagdollPart.Type.RightLeg)))
                {
                    continue;
                }

                bool result = entry.part.TrySlice();
                if (result)
                {
                    DebugService.LogInfo($"Sliced {entry.part.type} | Mult: {entry.mult:F2}");

                    if (entry.part.type == RagdollPart.Type.LeftArm) slicedParts.Add(RagdollPart.Type.LeftArm);
                    if (entry.part.type == RagdollPart.Type.RightArm) slicedParts.Add(RagdollPart.Type.RightArm);
                    if (entry.part.type == RagdollPart.Type.LeftLeg) slicedParts.Add(RagdollPart.Type.LeftLeg);
                    if (entry.part.type == RagdollPart.Type.RightLeg) slicedParts.Add(RagdollPart.Type.RightLeg);
                }

                yield return null;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateColor(bool useDefault = true)
        {

            if (module.isStun)
                return;

            if (useDefault)
            {
                color = new Color(module.color[0], module.color[1], module.color[2], module.color[3]);

                coreColorGO = new Color(module.coreColorGO[0], module.coreColorGO[1], module.coreColorGO[2], 1);
                coreIntensity = module.coreColorGO[3];

                colorGO = new Color(module.colorGO[0], module.colorGO[1], module.colorGO[2], 1);
                glowIntensity = module.colorGO[3];

                lightColor = new Color(module.lightColor[0], module.lightColor[1], module.lightColor[2], module.lightColor[3]);
            }

            MaterialPropertyBlock glowBlock = new MaterialPropertyBlock();
            MaterialPropertyBlock trailBlock = new MaterialPropertyBlock();

            if (glowRenderer)
            {
                if (Global.globalUsePP)
                {
                    glowRenderer.gameObject.SetActive(false);
                }
                else
                {
                    glowRenderer.gameObject.SetActive(true);
                    glowRenderer.GetPropertyBlock(glowBlock);
                    glowBlock.SetColor("_Color", color);
                    glowBlock.SetInt("_IsCorrupted", 1);
                    glowRenderer.SetPropertyBlock(glowBlock);
                }
            }

            if (meshRenderer)
            {
                meshRenderer.GetPropertyBlock(glowBlock);

                glowBlock.SetColor("_CoreColor", coreColorGO);
                glowBlock.SetColor("_GlowColor", colorGO);

                glowBlock.SetFloat("_CoreIntensity", coreIntensity);
                glowBlock.SetFloat("_GlowIntensity", glowIntensity);

                meshRenderer.SetPropertyBlock(glowBlock);
            }

            if (trail)
            { 
                trail.GetPropertyBlock(trailBlock);
                if (Global.globalUsePP)
                { 
                    trailBlock.SetColor("_GlowColor", colorGO);
                }
                else
                {
                    trailBlock.SetColor("_GlowColor", color);
                }
                trail.SetPropertyBlock(trailBlock);
            }

            if (light)
                light.color = lightColor;

        }

        private void CheckDeflectBolt()
        {
            if (module.isStun || deflectTime > 0 || !ModSettings.bDeflectAssist)
            {
                deflectTime -= Time.deltaTime;
                return;
            }

            foreach (var blade in Global.allBlades)
            {
                if (!blade.b_is_active) continue;

                Vector3 extents = new Vector3(ModSettings.fDeflectAssistRadius, blade.f_current_length, ModSettings.fDeflectAssistRadius);

                Vector3 center = blade.lightsaberBlade.transform.position + (blade.lightsaberBlade.transform.up * (blade.f_current_length / 2));
                Quaternion rotation = blade.lightsaberBlade.transform.rotation;

                Collider[] hits = Physics.OverlapBox(center, extents, rotation);

                bool deflected = hits.Any(x => x.transform.IsChildOf(this.transform));

                if (deflected)
                {
                    float rnd = UnityEngine.Random.Range(0, 100);
                    if (rnd > ModSettings.iDeflectChance) continue;

                    DeflectBolt(blade);
                    break;
                }

            }
        }

       private void DeflectBolt(LightsaberBlade blade)
        {
            Vector3 deflectionDir;

            if (blade.m_item?.mainHandler != null)
            {
                deflectionDir = -blade.m_item.mainHandler.transform.right;
            }
            else
            {
                deflectionDir = Vector3.Reflect(body.velocity.normalized, blade.lightsaberBlade.transform.right);
            }

            float currentSpeed = body.velocity.magnitude;
            body.velocity = deflectionDir * currentSpeed * ModSettings.fDeflectSpeedMultiplier;

            if (body.velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(body.velocity, Vector3.up);

            despawnTime = ModSettings.fBlasterLifetime;
            hasHit = false;
            hasDeflectedRecently = true;
            deflectTime = 0.03f;

            GameObject obj = new GameObject("Impact");
            obj.transform.position = transform.position;
            PlayDeflectEffect(obj.transform);

            item.ResetObjectCollision();
            item.ResetColliderCollision();
        }

        private void DespawnBolt()
        {
            if (trail)
            {
                trail.time = ModSettings.fBoltTrailLifetime;
                trail.Clear();
            }
            despawnTime = ModSettings.fBlasterLifetime;
            deflectTime = 0.1f;
            ricochets = module.bounces;
            hasHit = false;
            destroy = false;
            destroyThisTick = false;
            hasDeflectedRecently = false;
            item.Despawn();
            ToggleDamagers(true);
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
            if (despawnTime > 0)
            {
                despawnTime -= Time.deltaTime;
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