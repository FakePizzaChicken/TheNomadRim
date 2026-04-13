using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ThunderRoad;
using ThunderRoad.Skill;
using Unity.XR.CoreUtils;
using UnityEngine;
using static ThunderRoad.BrainModuleHitReaction.PushBehaviour;

namespace TheNomadRim
{
    public class ItemGrenade : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public Item item;
        public ModuleItemGrenade module;

        private Animator animator;
        private AudioSource audioSource;
        private Renderer renderer;

        private float heldTimer;
        private int safetyPosition;
        private bool hasDetonated = false;

        private Color enabledColor, disabledColor;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleItemGrenade>();

            item.OnGrabEvent += OnGrabbed;
            item.OnUngrabEvent += OnDrop;
            item.OnHeldActionEvent += OnAction;

            animator = item.gameObject.GetComponent<Animator>();
            audioSource = item.gameObject.GetComponent<AudioSource>();

            renderer = item.GetCustomReference<Transform>("Renderer").GetComponent<Renderer>();

            enabledColor = new Color(module.fEmissionColorActivated[0], module.fEmissionColorActivated[1], module.fEmissionColorActivated[2], module.fEmissionColorActivated[3]);
            disabledColor = new Color(module.fEmissionColorDeactivated[0], module.fEmissionColorDeactivated[1], module.fEmissionColorDeactivated[2], module.fEmissionColorDeactivated[3]);

            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_UseEmission", 0f);
                block.SetColor("_EmissionColor", disabledColor);
                renderer.SetPropertyBlock(block);
            }

            foreach (var item1 in item.collisionHandlers)
            {
                item1.OnCollisionStartEvent += OnItemCollision;
            }

            heldTimer = 0f;
            safetyPosition = 0;

            item.OnCullEvent += Item_OnCullEvent;
        }

        private void Item_OnCullEvent(bool culled)
        {
            if (culled && safetyPosition > 0) gameObject.SetActive(true);
        }

        private void OnItemCollision(CollisionInstance collision)
        {
            DebugService.LogInfo("Collision Event Started");

            if (hasDetonated) return;

            if (module.sActivationType == "Impact" && (safetyPosition > 0 || string.IsNullOrEmpty(module.sSafetyType)))
            {
                var explosion = module.explosionEffect.Spawn(transform.position, Quaternion.identity);
                Detonate();
            }
        }


        protected override void ManagedUpdate()
        {
            if (heldTimer > 0)
            {
                heldTimer -= Time.deltaTime;
                if (module.sSafetyType == "Twice")
                {
                    if (heldTimer <= 0 && safetyPosition == 1)
                    {
                        NoReturn();
                    }
                }
            }
            if (safetyPosition == 2 && hasDetonated == false)
            {
                if (!audioSource.isPlaying)
                {
                    var explosion = module.explosionEffect.Spawn(transform.position, Quaternion.identity);
                    Detonate();

                    Util.StopLoopedSound(audioSource);

                    safetyPosition = 2;
                }
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
            if (action == Interactable.Action.AlternateUseStart && (module.sSafetyType == "Once" || module.sSafetyType == "Twice"))
            {
                if (safetyPosition == 1)
                {
                    heldTimer = 0.4f;
                }
            }

            if (action == Interactable.Action.AlternateUseStop)
            {
                if (safetyPosition == 2 || hasDetonated) return;

                if (safetyPosition >= 1 && module.bCanBeDeactivated)
                {
                    if (heldTimer > 0.2f)
                        Disarm();
                }
                else
                {
                    Arm();
                }

                heldTimer = 0;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void Arm()
        {
            safetyPosition = 1;

            if (!string.IsNullOrEmpty(module.sAnimActivate) && animator != null)
            {
                animator.Play(module.sAnimActivate);
            }

            if (module.activationSound != null) Util.PlaySound(audioSource, module.activationSound, ModSettings.fThermalSoundVolume);
            if (module.activatedLoopSound != null) Util.PlaySoundLooped(audioSource, module.activatedLoopSound, ModSettings.fThermalSoundVolume);

            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_UseEmission", 1f);
                block.SetColor("_EmissionColor", enabledColor);
                renderer.SetPropertyBlock(block);
            }
        }

        private void Disarm()
        {
            safetyPosition = 0;

            if (!string.IsNullOrEmpty(module.sAnimDeactivate) && animator != null) animator.Play(module.sAnimDeactivate);

            Util.StopLoopedSound(audioSource);
            if (module.deactivationSound != null) Util.PlaySound(audioSource, module.deactivationSound, ModSettings.fThermalSoundVolume);

            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_UseEmission", 0f);
                block.SetColor("_EmissionColor", disabledColor);
                renderer.SetPropertyBlock(block);
            }
        }

        private void NoReturn()
        {
            safetyPosition = 2;
            Util.StopLoopedSound(audioSource);
            if (module.timedSound != null) Util.PlaySound(audioSource, module.timedSound, ModSettings.fThermalSoundVolume);
        }

        public void Detonate()
        {
            if (hasDetonated) return;
            hasDetonated = true;

            var pos = transform.position;

            foreach (var item1 in item.collisionHandlers)
            {
                item1.OnCollisionStartEvent -= OnItemCollision;
            }

            var hitMask = (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << 24) | (1 << 25) | (1 << 26) | (1 << 27) | (1 << 31);
            var ignoreMask = ~((1 << 10) | (1 << 13) | (1 << 26) | (1 << 27) | (1 << 31));

            List<RagdollPart> affectedLimbs = new List<RagdollPart>();

            float hitradius = module.fRadius * ModSettings.fThermalDetonateRadius;

            foreach (Collider collider in Physics.OverlapSphere(pos, hitradius, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (collider == null)
                {
                    DebugService.LogWarning("Collider is null, skipping.");
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
                        DebugService.LogInfo("Skipping invincible player.");
                        continue;
                    }

                    Item itemObj = collider.GetComponentInParent<Item>();
                    GolemCrystal crystal = collider.GetComponentInParent<GolemCrystal>();
                    Breakable breakable = collider.GetComponentInParent<Breakable>();

                    foreach (var type in module.mDamageTypes)
                    {
                        switch (type)
                        {
                            case "None":
                                break;
                            case "Fire":
                                if (crt)
                                {
                                    crt.Inflict("Burning", this, 2f);
                                }
                                break;
                            case "Electric":
                                if (crt)
                                {
                                    crt.Inflict("Electrocute", this, 2f);
                                }
                                break;
                            case "Gravity":
                                {
                                    crt?.Inflict("Floating", this, 6.7f);
                                    itemObj?.Inflict("Floating", this, 6.7f);
                                }
                                break;
                            case "Dismemberment":
                                var rp = collider.GetComponent<RagdollPart>() ?? collider.GetComponentInParent<RagdollPart>();
                                if (rp && rp.sliceAllowed && !rp.isSliced && !affectedLimbs.Contains(rp))
                                {
                                    float multiplier = Mathf.Clamp01((hitradius - dist) / hitradius);

                                    if (multiplier > 0.05f)
                                    {
                                        if (!module.bDismembermentNonFatal || !IsFatal(rp.type))
                                        {
                                            affectedLimbs.Add(rp);
                                        }
                                    }
                                }

                                if (crt != null && crt == Player.currentCreature)
                                    Player.currentCreature.Kill();

                                break;
                            case "Heal":
                                if (crt)
                                {
                                    crt.Heal(module.fHealthAmount);
                                }
                                break;
                            case "Damage":
                                if (crt)
                                {
                                    crt.Damage(module.fDamageAmount);
                                }
                                break;
                            case "Destabilize":
                                if (crt)
                                {
                                    crt.ragdoll.SetState(Ragdoll.State.Destabilized);
                                }
                                break;
                            case "Disorientate":
                                if (crt)
                                {
                                    GameManager.local.StartCoroutine(BlindNPC(crt, 3f));
                                }
                                break;
                            case "Rescale":
                                if (crt) crt.transform.localScale = module.bSetScale ? new Vector3(module.fRescale, module.fRescale, module.fRescale) : crt.transform.localScale * module.fRescale;
                                if (itemObj) itemObj.transform.localScale = module.bSetScale ? new Vector3(module.fRescale, module.fRescale, module.fRescale) : itemObj.transform.localScale * module.fRescale;
                                break;
                            case "Drop":
                                if (crt)
                                {
                                    if (crt.handLeft?.grabbedHandle != null)
                                        crt.handLeft.grabbedHandle.Release();
                                    if (crt.handRight?.grabbedHandle != null)
                                        crt.handRight.grabbedHandle.Release();

                                    if (module.bDropIncludeArmor)
                                    {
                                        crt.equipment?.UnequipAllWardrobes(updateParts: true);
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    if (breakable && module.bBreakBreakables)
                    {
                        breakable.Break();
                    }

                    if (crystal && module.bBreakCrystals)
                    {
                        if (crystal.shield != null && crystal.shield.activeSelf)
                        {
                            if (module.bBypassShield)
                                crystal.Break();
                            else
                                crystal.shield.SetActive(false);
                        }
                        else
                        {
                            crystal.Break();
                        }
                    }

                    float radius = ModSettings.fThermalDetonateRadius * module.fRadius;
                    float force = 10 * module.fForce;
                    body.AddExplosionForce(force * ((radius - dist) / radius), pos, radius, 1f);
                }
            }


            var soundSourceObj = new GameObject("GrenadeSoundSource");
            soundSourceObj.transform.position = item.transform.position;
            var sAudioSource = soundSourceObj.AddComponent<AudioSource>();
            var sAudioSourceBass = soundSourceObj.AddComponent<AudioSource>();

            sAudioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            sAudioSourceBass.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            sAudioSource.maxDistance = 75f;
            sAudioSourceBass.maxDistance = 150f;

            if (module.explosionSound != null)
            {
                Util.PlaySound(sAudioSource, module.explosionSound, ModSettings.fThermalSoundVolume);
            }
            else DebugService.LogWarning("No explosion sound assigned to the grenade module.");

            if (module.explosionBassSound)
            {
                Util.PlaySound(sAudioSourceBass, module.explosionBassSound, ModSettings.fThermalSoundVolume);
            }

            GameObject.Destroy(soundSourceObj, 2f);

            if (affectedLimbs.Count > 0)
            {
                GameManager.local.StartCoroutine(DismemberCreature(affectedLimbs));
            }
            else
            {
                item.Despawn();
            }

            return;
        }

        private IEnumerator BlindNPC(Creature crt, float duration)
        {
            crt.brain.currentTarget = null;
            crt.brain.SetState(Brain.State.Idle);
            crt.brain.navMeshAgent.isStopped = true;

            yield return new WaitForSeconds(duration);

            crt.brain.navMeshAgent.isStopped = false;
            crt.brain.ResetBrain();
            crt.brain.SetState(Brain.State.Investigate);
        }


        private IEnumerator DismemberCreature(List<RagdollPart> limbs)
        {
            float radius = module.fRadius * ModSettings.fThermalDetonateRadius;

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
                float mult = (radius - dist) / radius;

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

            item?.Despawn();
        }




        private bool IsFatal(RagdollPart.Type part)
        {
            return !(part == RagdollPart.Type.LeftFoot ||
                    part == RagdollPart.Type.RightFoot ||
                    part == RagdollPart.Type.LeftHand ||
                    part == RagdollPart.Type.RightHand);
        }

        private bool IsPartDismemberable(RagdollPart part)
        {
            if (part == null) return false;

            float distance = Vector3.Distance(part.transform.position, transform.position);
            float maxDistance = module.fRadius * ModSettings.fThermalDetonateRadius;

            return distance <= maxDistance;
        }
    }
}

