using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    enum BlasterModes
    {
        SingleShot = 0,
        Burst = 1,
        RapidFire = 2,
        Stun = 3
    }

    public class ItemBlaster : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.LateUpdate;

        private Item item;
        private ModuleBlaster module;
        private Rigidbody body;

        private SpellTelekinesis currentTelekinesis;

        private AudioSource fireSource;
        private AudioSource bassSource;
        private AudioSource hifiSource;
        private AudioSource mainSource;
        private AudioSource spinSource;
        private AudioSource heatSource;

        private ParticleSystem muzzleFlash;
        private Color muzzleFlashMainColor;
        private Color muzzleFlashStunColor;
        private Color MuzzleFlashChargeColor;

        private ParticleSystem overheatSmoke;

        private Transform[] bulletSpawns;

        private Handle grip, foregrip, scopegrip;

        private Animator animator;

        private MeshRenderer scopeRenderer;
        private Camera scopeCamera;
        private RenderTexture scopeTexture;

        private ItemData projectile;
        private ItemData stunProjectile;

        private Creature currentAI;

        // States

        private bool isAI = false;
        private bool isHoldingFire = false;
        private bool isCharging = false;
        private bool isSpinning = false;
        private bool isOverheated = false;
        private bool isReloading = false;

        private bool holdingGripLeft, holdingGripRight, holdingForegripLeft, holdingForegripRight, holdingScopegripLeft, holdingScopegripRight;

        private float chargeProgress = 0;
        private float spinSpeed = 0;
        private float shotsInBurst = 0;


        // Blaster config
        private int fireMode = 0;
        private int fmIndex = 0;
        private int scopeMode = 0;

        private int ammo;
        private float currentHeat;

        private float currentInaccuracy = 0;
        private int oldScopeRes;
        private string blasterBoltID;
        private string blasterBoltOverride;


        // Timers
        private float fireTime;
        private float aiFireSwitchTime;
        private float gripPrimaryHoldTime;
        private float gripAltHoldTime;
        private float foregripPrimaryHoldTime;
        private float foregripAltHoldTime;
        private float scopegripPrimaryHoldTime;
        private float scopegripAltHoldTime;
        private float reloadTime;
        private float chargeTime;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleBlaster>();
            body = GetComponent<Rigidbody>();
            mainSource = GetComponent<AudioSource>();
            animator = GetComponent<Animator>();

            GameObject parent = item.gameObject;

            // Grips

            grip = parent.GetNamedChild("MainHandle")?.GetComponent<Handle>();
            foregrip = parent.GetNamedChild("ForegripHandle")?.GetComponent<Handle>();
            scopegrip = parent.GetNamedChild("ScopegripHandle")?.GetComponent<Handle>();

            item.OnGrabEvent += GrabEvent;
            item.OnUngrabEvent += DropEvent;
            item.OnHeldActionEvent += ActionEvent;
            item.OnTelekinesisGrabEvent += TelekinesisGrabEvent;
            item.OnTelekinesisReleaseEvent += TelekinesisDropEvent;

            if (grip)
            {
                grip.Grabbed += GripGrabEvent;
                grip.UnGrabbed += GripDropEvent;
            }

            if (scopegrip)
            {
                scopegrip.Grabbed += ScopegripGrabEvent;
                scopegrip.UnGrabbed += ScopegripDropEvent;
            }

            if (foregrip)
            {
                foregrip.Grabbed += ForegripGrabEvent;
                foregrip.UnGrabbed += ForegripDropEvent;
            }

            item.OnSnapEvent += HolsterEvent;

            // Scope

            scopeCamera = parent.GetNamedChild("ScopeCamera")?.GetComponent<Camera>();
            scopeRenderer = parent.GetNamedChild("ScopeRender")?.GetComponent<MeshRenderer>();

            // Shooting

            if (module.spawnPoints > 0)
            {
                bulletSpawns = new Transform[module.spawnPoints];
                for (int i = 0; i < module.spawnPoints; i++)
                {
                    var spwn = parent.GetNamedChild("BulletSpawn" + i)?.transform;
                    if (spwn == null)
                        bulletSpawns[i] = i == 0 ? bulletSpawns[0] : bulletSpawns[i - 1];
                    else
                        bulletSpawns[i] = spwn;
                }
            }

            muzzleFlash = parent.GetNamedChild("MuzzleFlashTest")?.GetComponent<ParticleSystem>();
            overheatSmoke = parent.GetNamedChild("OverheatSmoke")?.GetComponent<ParticleSystem>();

            // Audio

            fireSource = parent.GetNamedChild("FireSource")?.GetComponent<AudioSource>();
            bassSource = parent.GetNamedChild("BassSource")?.GetComponent<AudioSource>();
            hifiSource = parent.GetNamedChild("HiFiSource")?.GetComponent<AudioSource>();
            spinSource = parent.GetNamedChild("SpinSource")?.GetComponent<AudioSource>();
            heatSource = parent.GetNamedChild("HeatSource")?.GetComponent<AudioSource>();

            if (fireSource) fireSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (bassSource) bassSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (hifiSource) hifiSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (mainSource) mainSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (spinSource) spinSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (heatSource) heatSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            // Blaster AI

            AIFireable aiFireable = item.GetComponent<AIFireable>();
            aiFireable?.fireEvent.AddListener(AIFireEvent);
            aiFireable?.reloadEvent.AddListener(AIReloadEvent);

            // Initial states & load save data
            LoadSaveData();
            SetupScope();

        }

        protected void OnDestroy()
        {
            if (scopeCamera?.targetTexture)
            {
                scopeCamera.targetTexture = null;
            }

            if (scopeTexture != null)
            {
                if (scopeTexture.IsCreated()) scopeTexture.Release();
                Destroy(scopeTexture);
            }

            if (item != null)
            {
                item.OnGrabEvent -= GrabEvent;
                item.OnUngrabEvent -= DropEvent;
                item.OnHeldActionEvent -= ActionEvent;
                item.OnTelekinesisGrabEvent -= TelekinesisGrabEvent;
                item.OnTelekinesisReleaseEvent -= TelekinesisDropEvent;
            }

            if (grip != null)
            {
                grip.UnGrabbed -= GripDropEvent;
                grip.Grabbed -= GripGrabEvent;
            }

            if (foregrip != null)
            {
                foregrip.UnGrabbed -= ForegripDropEvent;
                foregrip.Grabbed -= ForegripGrabEvent;
            }

            if (scopegrip != null)
            {
                scopegrip.UnGrabbed -= ScopegripDropEvent;
                scopegrip.Grabbed -= ScopegripGrabEvent;
            }
        }

        protected override void ManagedLateUpdate()
        {
            UpdateTimes();
            UpdateScope();

            UpdateActionHeld(module.primaryActionHold, holdingGripLeft, holdingGripRight, ref gripPrimaryHoldTime);
            UpdateActionHeld(module.altActionHold, holdingGripLeft, holdingGripRight, ref gripAltHoldTime);

            UpdateActionHeld(module.primaryForegripActionHold, holdingForegripLeft, holdingForegripRight, ref foregripPrimaryHoldTime);
            UpdateActionHeld(module.altForegripActionHold, holdingForegripLeft, holdingForegripRight, ref foregripAltHoldTime);

            UpdateActionHeld(module.primaryScopeActionHold, holdingScopegripLeft, holdingScopegripRight, ref scopegripPrimaryHoldTime);
            UpdateActionHeld(module.altScopeActionHold, holdingScopegripLeft, holdingScopegripRight, ref scopegripAltHoldTime);

            UpdateHeat();

            if (currentTelekinesis != null && currentTelekinesis.spinMode && fireTime <= 0)
            {
                currentTelekinesis.SetSpinMode(false);
                FireBolt();
            }

            if (isHoldingFire && ((module.requiresSpin && spinSpeed > 0.35) || !module.requiresSpin)) shotsInBurst = 1;

            if (fireTime <= 0 && !isReloading)
            {
                if (module.requiresSpin && spinSpeed > 0.35)
                {
                    if (shotsInBurst > 0)
                    {
                        FireBolt();
                    }
                }
                else
                {
                    if (shotsInBurst > 0)
                    {
                        FireBolt();
                    }
                }
            }

            if (isCharging)
            {
                chargeProgress += Time.deltaTime;
                float chargeNormalized = Mathf.Clamp01(chargeProgress / Mathf.Clamp(module.chargeTime, 0.01f, float.MaxValue));

                if (muzzleFlash)
                {
                    var effect = muzzleFlash.main;
                    var effectChild = muzzleFlash.subEmitters.GetSubEmitterSystem(0).main;
                    var effectChildTransform = muzzleFlash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                    effect.startColor = MuzzleFlashChargeColor;

                    effect.loop = true;
                    muzzleFlash.transform.localScale = Vector3.one * chargeNormalized;

                    effectChild.loop = true;
                    effectChildTransform.localScale = Vector3.one * chargeNormalized;
                }

                spinSource.volume = chargeNormalized;

                if (chargeTime <= 0)
                {
                    Util.PlayHaptic(holdingGripLeft, holdingGripRight, 0.8f);
                    if (!fireSource.isPlaying) Util.PlaySoundLooped(fireSource, module.chargeStopContainer);
                    spinSource.volume = 1.0f;
                }
            }

            if (module.requiresSpin)
            {
                if (isSpinning)
                {
                    spinSpeed = Mathf.MoveTowards(spinSpeed, 1f, Time.deltaTime * 2);
                }
                else
                {
                    spinSpeed = Mathf.MoveTowards(spinSpeed, 0f, Time.deltaTime);
                }

                if (animator)
                {
                    animator.SetBool("Rotating", true);
                    animator.speed = spinSpeed;
                }

                spinSource.pitch = spinSpeed;

                if (spinSpeed <= 0 && !isSpinning)
                {
                    spinSource.Stop();
                    spinSource.pitch = 1.0f;

                    if (animator)
                    {
                        animator.SetBool("Rotating", false);
                        animator.speed = 0;
                        spinSpeed = 0;
                    }
                }

                if (isAI)
                {
                    if (currentAI.brain.state == Brain.State.Combat || currentAI.brain.state == Brain.State.Alert)
                    {
                        SpinStart();
                    }
                    else
                        SpinStop();
                }
            }

            if (isReloading)
            {
                if (reloadTime <= 0)
                    ReloadFinish();
            }
        }

        private void UpdateHeat()
        {
            if (isOverheated)
            {
                if (heatSource != null)
                {
                    heatSource.volume = Mathf.Clamp01(currentHeat / module.heatThreshold);

                    if (!heatSource.isPlaying && module.overheatLoopSoundContainer != null)
                    {
                        Util.PlaySoundLooped(heatSource, module.overheatLoopSoundContainer);
                    }   
                }

                if (currentHeat <= 0)
                {
                    isOverheated = false;
                    overheatSmoke?.Stop();
                    heatSource?.Stop();
                }
            }
            else if (currentHeat > module.heatThreshold)
            {
                isOverheated = true;

                if (fireSource != null && module.overheatSoundContainer != null)
                {
                    Util.PlaySound(fireSource, module.overheatSoundContainer);
                }
                if (heatSource != null && module.overheatLoopSoundContainer != null)
                {
                    Util.PlaySoundLooped(heatSource, module.overheatLoopSoundContainer);
                    heatSource.volume = 1.0f;
                }
                overheatSmoke?.Play();
            }

            if (currentHeat <= 0 && heatSource != null && heatSource.isPlaying)
            {
                heatSource.Stop();
            }
        }

        private void UpdateTimes()
        {
            if (fireTime > 0) fireTime -= Time.deltaTime;
            if (aiFireSwitchTime > 0) aiFireSwitchTime -= Time.deltaTime;
            if (reloadTime > 0) reloadTime -= Time.deltaTime;
            if (chargeTime > 0) chargeTime -= Time.deltaTime;

            if (currentInaccuracy > 0)
                currentInaccuracy -= Time.deltaTime * module.inaccuracyRecoverRate;

            if (currentHeat > 0)
                currentHeat -= Time.deltaTime * (module.heatRecoveryRate * (isOverheated ? 0.5f : 1f));
        }

        private void UpdateActionHeld(string action, bool left, bool right, ref float time)
        {
            if (string.IsNullOrEmpty(action)) return;

            if (time > 0)
            {
                time -= Time.deltaTime;
                if (time <= 0)
                {
                    RagdollHand hand = null;
                    if (left) hand = Player.local.handLeft.ragdollHand;
                    if (right) hand = Player.local.handRight.ragdollHand;
                    HandleAction(action, hand);
                }
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------- \\

        // Events

        private void GrabEvent(Handle handle, RagdollHand hand)
        {
            if (module.hasScope)
            {
                SetScopeRendererState(hand.playerHand != null);
            }
        }

        private void DropEvent(Handle handle, RagdollHand hand, bool throwing)
        {
            if (module.hasScope)
            {
                if (!item.IsHeld() || !item.IsHeldByPlayer)
                    SetScopeRendererState(false);
            }

            StopHeldEvents();
        }

        private void HolsterEvent(Holder holder) => UpdateSaveData();

        private void GripGrabEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;

            holdingGripLeft = hand.playerHand == Player.local.handLeft;
            holdingGripRight = hand.playerHand == Player.local.handRight;

            if (!hand.playerHand)
            {
                isAI = true;
                aiFireSwitchTime = 7.5f;
                currentAI = hand.creature;
            }
        }

        private void GripDropEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;

            if (hand.playerHand == Player.local.handLeft) holdingGripLeft = false;
            else if (hand.playerHand == Player.local.handRight) holdingGripRight = false;

            if (currentAI) currentAI = null;
            isAI = false;

            StopHeldEvents();
        }

        private void ForegripGrabEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            holdingForegripLeft = hand.playerHand == Player.local.handLeft;
            holdingForegripRight = hand.playerHand == Player.local.handRight;
        }

        private void ForegripDropEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            if (hand.playerHand == Player.local.handLeft) holdingForegripLeft = false;
            else if (hand.playerHand == Player.local.handRight) holdingForegripRight = false;
        }

        private void ScopegripGrabEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            holdingScopegripLeft = hand.playerHand == Player.local.handLeft;
            holdingScopegripRight = hand.playerHand == Player.local.handRight;
        }

        private void ScopegripDropEvent(RagdollHand hand, Handle handle, EventTime eventTime)
        {
            if (hand.playerHand == Player.local.handLeft) holdingScopegripLeft = false;
            else if (hand.playerHand == Player.local.handRight) holdingScopegripRight = false;
        }

        private void TelekinesisGrabEvent(Handle handle, SpellTelekinesis telekinesis)
        {
            currentTelekinesis = telekinesis;
        }

        private void TelekinesisDropEvent(Handle handle, SpellTelekinesis telekinesis, bool tryThrow, bool isGrabbing)
        {
            currentTelekinesis = null;
        }

        private void ActionEvent(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (handle == grip)
            {
                HandleHeld(hand, action,
                    module.primaryAction, module.altAction,
                    module.primaryActionHold, module.altActionHold,
                    ref gripPrimaryHoldTime, ref gripAltHoldTime);
            }
            else if (handle == foregrip)
            {
                HandleHeld(hand, action,
                    module.primaryForegripAction, module.altForegripAction,
                    module.primaryForegripActionHold, module.altForegripActionHold,
                    ref foregripPrimaryHoldTime, ref foregripAltHoldTime);
            }
            else if (handle == scopegrip)
            {
                HandleHeld(hand, action,
                    module.primaryScopeAction, module.altScopeAction,
                    module.primaryScopeActionHold, module.altScopeActionHold,
                    ref scopegripPrimaryHoldTime, ref scopegripAltHoldTime);
            }
        }

        private void HandleHeld(RagdollHand hand, Interactable.Action action, string primaryAction, string altAction, string primaryHeld, string altHeld, ref float time, ref float altTime)
        {
            if (action == Interactable.Action.UseStop)
            {
                if (primaryHeld == "spin")
                    SpinStop();

                if (primaryAction == "shoot")
                {
                    isHoldingFire = false;
                }
            }

            if (action == Interactable.Action.AlternateUseStop)
            {
                if (altHeld == "spin")
                    SpinStop();

                if (altAction == "shoot")
                {
                    isHoldingFire = false;
                }
            }

            if (fireMode == 2)
            {
                if (action == Interactable.Action.UseStart)
                {
                    if (primaryAction == "shoot")
                    {
                        isHoldingFire = true;
                        return;
                    }
                }

                if (action == Interactable.Action.AlternateUseStart)
                {
                    if (altAction == "shoot")
                    {
                        isHoldingFire = true;
                        return;
                    }
                }
            }

            if (action == Interactable.Action.UseStop)
            {
                if (primaryHeld == "chargeShot")
                {
                    ChargeStop();
                }
                else if (primaryHeld == "spin")
                {
                    SpinStop();
                }
            }

            if (!string.IsNullOrEmpty(primaryHeld))
            {
                if (action == Interactable.Action.UseStart)
                {
                    time = 0.35f;
                }
                else if (action == Interactable.Action.UseStop)
                {
                    if (time > 0.2f)
                        HandleAction(primaryAction, hand);
                    time = 0.0f;
                }
            }
            else
            {
                if (action == Interactable.Action.UseStart) HandleAction(primaryAction, hand);
            }

            if (action == Interactable.Action.AlternateUseStop)
            {
                if (altHeld == "chargeShot")
                {
                    ChargeStop();
                }
                else if (altHeld == "spin")
                {
                    SpinStop();
                }
            }

            if (!string.IsNullOrEmpty(altHeld))
            {
                if (action == Interactable.Action.AlternateUseStart)
                {
                    altTime = 0.35f;
                }
                else if (action == Interactable.Action.AlternateUseStop)
                {
                    if (altTime > 0.2f)
                        HandleAction(altAction, hand);
                    altTime = 0.0f;
                }
            }
            else
            {
                if (action == Interactable.Action.AlternateUseStart) HandleAction(altAction, hand);
            }
        }

        private void HandleAction(string action, RagdollHand hand)
        {
            switch (action)
            {
                case "switchFiremode": SwitchFireMode(hand); break;
                case "switchZoom": SwitchScopeZoom(hand); break;
                case "reload": Reload(hand); break;
                case "shoot": ShootByMode(); break;
                case "chargeShot": ChargeStart(hand); break;
                case "spin": SpinStart(hand); break;
            }
        }

        private void AIFireEvent()
        {
            if (ModSettings.bAIFiremode && module.fireModes.Length > 1)
            {
                if (aiFireSwitchTime <= 0)
                {
                    SwitchFireMode();
                    if (!ModSettings.bAIStunMode && fireMode == (int)BlasterModes.Stun)
                        SwitchFireMode();
                    aiFireSwitchTime = Random.Range(5, 15);
                }
            }
            else
            {
                fireMode = module.fireModes[0];
            }

            isAI = true;

            switch (fireMode)
            {
                case (int)BlasterModes.SingleShot: FireBolt(); break;
                case (int)BlasterModes.Burst: StartBurst(); break;
                case (int)BlasterModes.RapidFire:
                    shotsInBurst = Random.Range(1, module.aiMaxShotsAmount);
                    break;
                case (int)BlasterModes.Stun: FireBolt(); SwitchFireMode(); break;
            }
        }

        private void AIReloadEvent()
        {
            Reload();
        }

        // ----------------------------------------------------------------------------------------------------------------------------- \\

        // Reload

        private void Reload(RagdollHand hand = null)
        {
            if (isReloading || (ModSettings.bBatteryRecharg && !isAI)) return;

            ammo = 0;

            Util.PlaySound(mainSource, module.reloadStartContainer);
            Util.PlaySound(mainSource, module.reloadStartContainer2);
            if (hand != null) Util.PlayHaptic(hand, 0.67f);

            isReloading = true;
            shotsInBurst = 0;
            reloadTime = module.reloadTime;
        }

        private void ReloadFinish(bool isSilent = false)
        {
            isReloading = false;
            reloadTime = 0;
            ammo = module.magazineCapacity;
            shotsInBurst = 0;
            if (!isSilent)
            {
                Util.PlaySound(mainSource, module.reloadFinishedContainer);
                Util.PlayHaptic(holdingGripLeft, holdingGripRight, 0.8f);
            }
            UpdateSaveData();
        }

        // Shooting

        private void SpinStart(RagdollHand hand = null)
        {
            isSpinning = true;
            Util.PlaySound(mainSource, module.chargeStartContainer);
            spinSpeed = 0;

            Util.PlaySoundLooped(spinSource, module.chargeLoopContainer, ModSettings.fBlasterSoundVolume);

            if (animator)
            {
                animator.SetBool("Rotating", true);
                animator.speed = spinSpeed;
            }
        }

        private void SpinStop()
        {
            if (!isSpinning) return;
            Util.PlaySound(mainSource, module.chargeStopContainer);
            isSpinning = false;
        }

        private void ChargeStart(RagdollHand hand = null)
        {
            if (ammo == 0 || isOverheated)
            {
                Util.PlaySound(fireSource, module.emptySoundContainer);
                return;
            }

            if (isCharging || fireMode == (int)BlasterModes.Stun) return;

            chargeTime = module.chargeTime;
            isCharging = true;
            chargeProgress = 0;

            if (muzzleFlash)
            {
                var effect = muzzleFlash.main;
                var effectChild = muzzleFlash.subEmitters.GetSubEmitterSystem(0).main;
                var effectChildTransform = muzzleFlash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                effect.startColor = MuzzleFlashChargeColor;

                effect.loop = true;
                muzzleFlash.transform.localScale = Vector3.zero;

                effectChild.loop = true;
                effectChildTransform.localScale = Vector3.zero;

                muzzleFlash.Play();
            }

            Util.PlaySound(fireSource, module.chargeStartContainer);
            Util.PlaySoundLooped(spinSource, module.chargeLoopContainer, 0);
        }

        private void ChargeStop()
        {
            if (!isCharging) return;

            if (muzzleFlash)
            {
                var effect = muzzleFlash.main;
                var effectChild = muzzleFlash.subEmitters.GetSubEmitterSystem(0).main;
                var effectChildTransform = muzzleFlash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                effect.loop = false;
                muzzleFlash.transform.localScale = Vector3.one;

                effectChild.loop = false;
                effectChildTransform.localScale = Vector3.one;

                muzzleFlash.Stop();
            }

            fireSource?.Stop();
            spinSource?.Stop();
            if (spinSource != null)
                spinSource.volume = 1.0f;

            if (chargeProgress >= 1)
            {
                FireBolt();
            }
            isCharging = false;
            chargeTime = 0;
            chargeProgress = 0;
        }

        private void ShootByMode()
        {
            if (fireTime > 0) return;

            switch (fireMode)
            {
                case (int)BlasterModes.SingleShot:
                    FireBolt();
                    fireTime = module.timeBetweenShots;
                    break;
                case (int)BlasterModes.Burst:
                    StartBurst();
                    fireTime = module.timeBetweenBursts;
                    break;
                case (int)BlasterModes.Stun:
                    FireBolt();
                    fireTime = module.timeBetweenStunShots;
                    break;
            }
        }

        private void StartBurst()
        {
            if (fireTime > 0)
                return;

            shotsInBurst = module.burstSize;
        }

        private void FireBolt()
        {
            if ((!ModSettings.bInfiniteAmmo && ammo <= 0) || isOverheated)
            {
                Util.PlaySound(fireSource, module.emptySoundContainer);
                shotsInBurst = 0;
                isHoldingFire = false;
                return;
            }

            RagdollHand hand = null;
            if (!isAI)
            {
                hand = grip.handlers.FirstOrDefault() ?? grip.telekinesisHandlers.FirstOrDefault()?.ragdollHand;
            }

            ItemData currentProjectile = projectile;
            string currentOverride = blasterBoltOverride;
            if (fireMode == (int)BlasterModes.Stun)
            {
                currentProjectile = stunProjectile;
                currentOverride = "";
            }
            else if (isCharging)
            {
                currentOverride = module.chargedBoltOverride;
            }

            foreach (var spawn in bulletSpawns)
            {
                currentProjectile.SpawnAsync(projectile =>
                {
                    projectile.OnSpawn(null, Item.Owner.None);

                    var boltComponent = projectile.GetComponent<ProjectileBlasterBolt>();
                    boltComponent.UpdateBoltData(currentOverride);

                    Collider[] weaponColliders = item.GetComponentsInChildren<Collider>();
                    Collider projectileCollider = projectile.GetComponentInChildren<Collider>();

                    if (projectileCollider)
                    {
                        foreach (var weaponCollider in weaponColliders)
                        {
                            Physics.IgnoreCollision(projectileCollider, weaponCollider);
                        }
                    }

                    projectile.transform.position = spawn.transform.position;
                    projectile.transform.rotation = Quaternion.Euler(CalculateInaccuracy(spawn.transform.rotation.eulerAngles));
                    projectile.Throw(1, Item.FlyDetection.Forced);
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    rb.AddForce(rb.transform.forward * (module.boltForce * ModSettings.fBoltSpeedMultiplier));

                }, owner: Item.Owner.None);

                if (!module.multishot && !(isCharging && module.chargedMultishot))
                {
                    break;
                }
            }

            if (holdingGripLeft || holdingGripRight)
                Util.PlayHaptic((holdingGripLeft ? Player.local.handLeft : Player.local.handRight).ragdollHand, 0.65f);

            if (!ModSettings.bNoRecoil)
            {
                body.AddRelativeTorque(new Vector3(
                    Random.Range(module.recoilMinTorque, module.recoilTorque),
                    Random.Range(-module.recoilTorqueSideways, module.recoilTorqueSideways),
                    Random.Range(-module.recoilTorqueSideways, module.recoilTorqueSideways)
                    ), ForceMode.VelocityChange);

                body.AddRelativeForce(new Vector3(
                    Random.Range(-module.recoilForceSideways, module.recoilForceSideways),
                    Random.Range(-module.recoilForceSideways, module.recoilForceSideways),
                    Random.Range(module.recoilMinForce, module.recoilForce)));

            }

            if (!ModSettings.bNoSpread)
            {
                var handlers = item.handlers.Count;
                if (handlers <= 0) handlers = 1;

                var inaccuracyGain = Mathf.Clamp(module.inaccuracyGain / (handlers * 0.85f), 0.85f, float.MaxValue);
                currentInaccuracy = Mathf.Clamp(currentInaccuracy + inaccuracyGain, 1.5f, module.maxInaccuracy);
            }
        

            if (!ModSettings.bInfiniteAmmo && module.magazineCapacity > 0 && ammo > 0) ammo--;
            currentHeat += module.heatGain * ModSettings.fBlasterOverheatMultiplier;

            if (shotsInBurst > 0) shotsInBurst--;

            PlayMuzzleFlash(fireMode == (int)BlasterModes.Stun);

            if (fireMode == (int)BlasterModes.Stun)
            {
                Util.PlaySound(fireSource, module.stunSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(bassSource, module.corebassStunSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(hifiSource, module.hifiSoundContainer, ModSettings.fBlasterSoundVolume);
            }
            else if (isCharging && chargeProgress >= 1)
            {
                Util.PlaySound(fireSource, module.chargedFireSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(bassSource, module.corebassChargedFireSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(hifiSource, module.hifiChargedFireSoundContainer, ModSettings.fBlasterSoundVolume);
            }
            else
            {
                Util.PlaySound(fireSource, module.fireSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(bassSource, module.corebassSoundContainer, ModSettings.fBlasterSoundVolume);
                Util.PlaySound(hifiSource, module.hifiSoundContainer, ModSettings.fBlasterSoundVolume);
            }

            if (fireMode == (int)BlasterModes.Burst)
            {
                if (shotsInBurst > 0) fireTime = module.timeBetweenShotsBurst;
                else fireTime = module.timeBetweenBursts;
            }
            else if (fireMode == (int)BlasterModes.Stun)
            {
                fireTime = module.timeBetweenStunShots;
            }
            else if (fireMode == (int)BlasterModes.RapidFire)
            {
                fireTime = module.timeBetweenShotsRapidFire;
            }
            else
            {
                fireTime = module.timeBetweenShots;
            }

        }

        private Vector3 CalculateInaccuracy(Vector3 rotation)
        {
            if (ModSettings.bNoRecoil && !isAI) { return rotation; }

            var inaccuracy = currentInaccuracy;
            if (isAI) inaccuracy /= Mathf.Clamp(ModSettings.fAIAccuracy, 0.01f, float.MaxValue);

            Vector2 spread = Random.insideUnitCircle * currentInaccuracy;

            Quaternion spreadRotation = Quaternion.Euler(spread.x, spread.y, 0);
            Quaternion finalRotation = Quaternion.Euler(rotation) * spreadRotation;

            return finalRotation.eulerAngles;
        }

        // ----------------------------------------------------------------------------------------------------------------------------- \\

        // Scope Stuff

        private void SetupScope()
        {
            if (!module.hasScope) return;

            if (scopeCamera && scopeRenderer)
            {
                CreateRenderTexture();
                scopeCamera.fieldOfView = module.scopeFOVs[scopeMode];
            }
        }

        private void CreateRenderTexture()
        {
            if (scopeTexture != null)
            {
                scopeCamera.targetTexture = null;
                scopeTexture.Release();
                Destroy(scopeTexture);
            }

            oldScopeRes = ModSettings.iBlasterScopeResolution;
            scopeTexture = new RenderTexture(oldScopeRes, oldScopeRes, 24, RenderTextureFormat.ARGB32);
            scopeTexture.Create();

            scopeCamera.targetTexture = scopeTexture;
            scopeCamera.enabled = false;

            ApplyScopeMaterial();
        }

        private void ApplyScopeMaterial()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            scopeRenderer.GetPropertyBlock(block);
            block.SetTexture("_RenderTexture", scopeTexture);
            if (module.reticleTexture != null) block.SetTexture("_Crosshair", module.reticleTexture);
            scopeRenderer.SetPropertyBlock(block);
        }


        private void SetScopeRendererState(bool state)
        {
            if (scopeRenderer) scopeRenderer.enabled = state;

            if (scopeCamera)
            {
                if (state)
                {
                    scopeCamera.targetTexture = scopeTexture;
                    scopeCamera.enabled = true;
                }
                else
                {
                    scopeCamera.enabled = false;
                }
            }
        }


        private void SwitchScopeZoom(RagdollHand hand)
        {
            if (scopeMode >= module.scopeFOVs.Length - 1)
                scopeMode = 0;
            else
                scopeMode++;

            scopeCamera.fieldOfView = module.scopeFOVs[scopeMode];

            if (hand != null) Util.PlayHaptic(hand, 0.1f);

            UpdateSaveData();
        }

        private void UpdateScope()
        {
            if (!module.hasScope || scopeTexture == null) return;

            if (ModSettings.iBlasterScopeResolution != scopeTexture.width)
            {
                CreateRenderTexture();
            }
        }

        // ----------------------------------------------------------------------------------------------------------------------------- \\

        // Save Data

        private void LoadSaveData()
        {
            if (item.TryGetCustomData(out BlasterSaveData blasterData) && blasterData != null)
            {
                fmIndex = blasterData.fireMode;
                scopeMode = blasterData.scopeZoom;
                blasterBoltID = blasterData.blasterBoltID;
                blasterBoltOverride = blasterData.blasterBoltOverride;
                ammo = blasterData.ammo;

                if (fmIndex >= module.fireModes.Length) fmIndex = 0;

                fireMode = module.fireModes[fmIndex];
                UpdateBolts(blasterBoltID, blasterBoltOverride);
            }
            else
            {
                blasterBoltID = module.boltProjectile;
                blasterBoltOverride = module.boltOverride;

                fmIndex = 0;
                fireMode = module.fireModes[0];
                ammo = module.magazineCapacity;

                UpdateBolts(blasterBoltID, blasterBoltOverride);
            }
        }

        private void UpdateSaveData()
        {
            var saveData = new BlasterSaveData();
            saveData.blasterBoltID = blasterBoltID;
            saveData.blasterBoltOverride = blasterBoltOverride;
            saveData.fireMode = fireMode;
            saveData.scopeZoom = scopeMode;
            saveData.ammo = ammo;

            Util.CleanCustomBlasterDataProperly(item);
            item.AddCustomData(saveData);
        }

        public void UpdateBolts(string projectile, string boltOverride)
        {
            stunProjectile = null;
            this.projectile = null;

            if (!string.IsNullOrEmpty(projectile))
                this.projectile = Catalog.GetData<ItemData>(projectile);

            if (this.projectile == null) this.projectile = Catalog.GetData<ItemData>(module.boltProjectile);
            if (!string.IsNullOrEmpty(module.stunProjectile)) stunProjectile = Catalog.GetData<ItemData>(module.stunProjectile);

            var boltData = this.projectile.data.GetModule<ModuleBlasterBolt>();
            var color = boltData.color;
            Color boltColor = new Color(color[0], color[1], color[2], color[3]);

            if (muzzleFlash)
            {
                if (boltData != null)
                {
                    var muzzleFlashMain = muzzleFlash.main;
                    muzzleFlashMain.startColor = boltColor;
                    muzzleFlashMainColor = boltColor;
                }

                var stundData = Catalog.GetData<ItemData>(module.stunProjectile);
                var stunBoltData = stundData?.data.GetModule<ModuleBlasterBolt>();
                if (stundData != null && stunBoltData != null)
                {
                    var stunColor = stunBoltData.color;
                    Color stunBoltColor = new Color(stunColor[0], stunColor[1], stunColor[2], stunColor[3]);
                    muzzleFlashStunColor = stunBoltColor;
                }

                var chargeData = Catalog.GetData<BlasterBoltData>(module.chargedBoltOverride);
                if (chargeData != null && chargeData.overrideColorData)
                {
                    var chargeColor = chargeData.color;
                    Color chargeBoltColor = new Color(chargeColor[0], chargeColor[1], chargeColor[2], chargeColor[3]);
                    MuzzleFlashChargeColor = chargeBoltColor;
                }
                else
                {
                    MuzzleFlashChargeColor = muzzleFlashMainColor;
                }
            }

            if (scopeRenderer)
            {
                MaterialPropertyBlock scopeBlock = new MaterialPropertyBlock();
                scopeRenderer.GetPropertyBlock(scopeBlock);
                scopeBlock.SetColor("_CrosshairColor", new Color(boltData.color[0], boltData.color[1], boltData.color[2], boltData.color[3]));
                scopeRenderer.SetPropertyBlock(scopeBlock);
            }

            blasterBoltID = projectile;
            if (!string.IsNullOrEmpty(boltOverride)) blasterBoltOverride = boltOverride;

            ReloadFinish(true);

            UpdateSaveData();
        }

        public void PlayMuzzleFlash(bool isStunShot = false)
        {
            if (muzzleFlash == null) return;

            var effect = muzzleFlash.main;

            effect.startColor = muzzleFlashMainColor;
            if (isStunShot) effect.startColor = muzzleFlashStunColor;
            else if (chargeProgress >= 1) effect.startColor = MuzzleFlashChargeColor;

            if (muzzleFlash.isPlaying) muzzleFlash.Stop();
            muzzleFlash.Play();
        }

        public void UpdateBoltOverride(string boltOverride)
        {
            blasterBoltOverride = boltOverride;
            UpdateSaveData();
        }

        private void SwitchFireMode(RagdollHand hand = null)
        {
            if (fmIndex >= module.fireModes.Length - 1)
                fmIndex = 0;
            else fmIndex++;

            StopHeldEvents();

            if (hand) Util.PlayHaptic(hand, 0.1f);

            Util.PlaySound(mainSource, module.switchSoundContainer, 0.8f);

            fireMode = module.fireModes[fmIndex];

            UpdateSaveData();
        }

        private void StopHeldEvents()
        {
            isCharging = false;
            isHoldingFire = false;

            SpinStop();
            ChargeStop();
        }

        // ----------------------------------------------------------------------------------------------------------------------------- \\
    }
}