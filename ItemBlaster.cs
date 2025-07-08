using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemBlaster : ThunderBehaviour
    {
        private Item m_item;
        private ModuleBlaster m_module;
        private Rigidbody m_body;
        private Transform[] m_bullet_spawn_points;
        private Handle m_grip, m_foregrip, m_scopegrip;
        private PlayerHand m_player_hand_grip, m_player_hand_foregrip, m_player_hand_scopegrip;
        private Animator m_animator;
        private MeshRenderer m_scope_renderer;
        private Camera m_scope_camera;
        private RenderTexture m_scope_texture;
        private AudioSource m_shoot_source;
        private SpellTelekinesis m_current_telekinesis;
        private ParticleSystem m_muzzle_flash;

        // Projectiles
        private ItemData m_projectile;
        private ItemData m_projectileStun;
        private ItemData m_charged_projectile;

        // States
        private bool b_is_ai;
        private bool b_is_holding;
        private bool b_is_holding_secondary;
        private bool b_is_bursting;
        private bool b_is_charging;
        private bool b_is_spinning;
        private float f_charge;
        private float f_spin_speed;

        // Fire Configuration
        private int i_fire_mode;
        private int i_fm_index;
        private int i_scope_mode;
        private int i_batch;
        private float f_accuracy;
        private int i_old_scope_res;
        private string s_blaster_bolt;

        // Timers
        private float t_fire_time;
        private float t_held_time;
        private float t_held_secondary_time;
        private float t_ai_fire_time_switch;
        private float t_burst_delay;

        //-------------------------------------------------------------------------------------------\\

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleBlaster>();
            m_body = GetComponent<Rigidbody>();

            GameObject parent = m_item.gameObject;

            // Handles
            m_grip = parent.GetNamedChild("MainHandle")?.GetComponent<Handle>();
            m_foregrip = parent.GetNamedChild("ForegripHandle")?.GetComponent<Handle>();
            m_scopegrip = parent.GetNamedChild("ScopegripHandle")?.GetComponent<Handle>();

            m_item.OnGrabEvent += OnBlasterGrabbed;
            m_item.OnUngrabEvent += OnBlasterDropped;
            m_item.OnHeldActionEvent += OnBlasterAction;
            m_item.OnTelekinesisGrabEvent += BlasterTeleGrabbed;
            m_item.OnTelekinesisReleaseEvent += BlasterTeleDropped;

            if (m_grip != null)
            {
                m_grip.UnGrabbed += GripUngrabbed;
                m_grip.Grabbed += GripGrabbed;
            }

            if (m_foregrip != null)
            {
                m_foregrip.UnGrabbed += ForegripUngrabbed;
            }

            // Scope
            m_scope_camera = parent.GetNamedChild("ScopeCamera")?.GetComponent<Camera>();
            m_scope_renderer = parent.GetNamedChild("ScopeRender")?.GetComponent<MeshRenderer>();

            // Spawn points
            if (m_module.i_num_spawn_points > 0)
            {
                m_bullet_spawn_points = new Transform[m_module.i_num_spawn_points];
                for (int i = 0; i < m_module.i_num_spawn_points; i++)
                {
                    m_bullet_spawn_points[i] = parent.GetNamedChild($"BulletSpawn{i}")?.transform;
                }
            }

            m_muzzle_flash = parent.GetNamedChild("MuzzleFlashTest")?.GetComponent<ParticleSystem>();

            // Animator
            m_animator = parent.GetComponent<Animator>();

            m_shoot_source = m_item.gameObject.GetNamedChild("ShootSource")?.GetComponent<AudioSource>();

            // Blaster AI support
            AIFireable aiFireable = m_item.GetComponent<AIFireable>();
            aiFireable?.fireEvent.AddListener(AIFire);

            f_accuracy = m_module.f_accuracy;

            LoadSaveData();
            SetupScope();
        }

        protected void LateUpdate()
        {

            if (m_module.b_has_scope && ModSettings.iBlasterScopeResolution != i_old_scope_res)
            {
                SetupScope();
                i_old_scope_res = ModSettings.iBlasterScopeResolution;
            }

            UpdateTimes();
            UpdateCharge();
            UpdateSpin();
            HandleFiring();
            UpdateScope();
        }

        protected void OnDestroy()
        {
            if (m_scope_camera?.targetTexture)
            {
                m_scope_camera.targetTexture.Release();
                m_scope_camera.targetTexture = null;
            }

            if (m_scope_texture != null)
            {
                if (m_scope_texture.IsCreated())
                    m_scope_texture.Release();
                Destroy(m_scope_texture);
            }

            if (m_item != null)
            {
                m_item.OnGrabEvent -= OnBlasterGrabbed;
                m_item.OnUngrabEvent -= OnBlasterDropped;
                m_item.OnHeldActionEvent -= OnBlasterAction;
                m_item.OnTelekinesisGrabEvent -= BlasterTeleGrabbed;
                m_item.OnTelekinesisReleaseEvent -= BlasterTeleDropped;
            }

            if (m_grip != null)
            {
                m_grip.UnGrabbed -= GripUngrabbed;
                m_grip.Grabbed -= GripGrabbed;
            }
        }


        //-------------------------------------------------------------------------------------------\\

        // Update helpers

        private void UpdateCharge()
        {
            if (!b_is_charging)
            {
                if (m_module.s_action_held == "actionCharge" && t_held_time <= 0.2f && t_held_time > 0)
                {
                    ChargeStart();
                }
                return;
            }

            f_charge += Time.deltaTime;
            float chargeNormalized = Mathf.Clamp01(f_charge / m_module.f_charge_time);

            if (m_muzzle_flash && m_module.s_action != "actionSpin")
            {
                var effect = m_muzzle_flash.main;
                var effectChild = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).main;
                var effectChildTransform = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                effect.loop = true;
                m_muzzle_flash.transform.localScale = Vector3.one * chargeNormalized;

                effectChild.loop = true;
                effectChildTransform.localScale = Vector3.one * chargeNormalized;
            }

            if (m_player_hand_grip != null && m_player_hand_grip.ragdollHand != null)
            {
                Util.PlayHaptic(m_player_hand_grip.ragdollHand, chargeNormalized);
            }

            if (m_animator)
            {
                m_animator.SetBool("Rotating", true);
                m_animator.speed = chargeNormalized;
            }
        }

        private void UpdateSpin()
        {
            if (b_is_spinning)
            {
                f_spin_speed = Mathf.MoveTowards(f_spin_speed, 1f, Time.deltaTime * 2f);
            }
            else
            {
                f_spin_speed = Mathf.MoveTowards(f_spin_speed, 0f, Time.deltaTime * 2f);
            }

            if (m_animator)
            {
                m_animator.speed = f_spin_speed;

                if (f_spin_speed < 0.01f)
                {
                    m_animator.SetBool("Rotating", false);
                }
            }
        }

        private void UpdateTimes()
        {
            t_fire_time = Mathf.Max(0, t_fire_time - Time.deltaTime);
            t_held_time = Mathf.Max(0, t_held_time - Time.deltaTime);
            t_held_secondary_time = Mathf.Max(0, t_held_secondary_time - Time.deltaTime);
            t_ai_fire_time_switch = Mathf.Max(0, t_ai_fire_time_switch - Time.deltaTime);
            t_burst_delay = Mathf.Max(0, t_burst_delay - Time.deltaTime);
        }

        private void HandleFiring()
        {
            if (m_current_telekinesis != null && m_current_telekinesis.spinMode && t_fire_time <= 0)
            {
                m_current_telekinesis.SetSpinMode(false);
                FireBolt();
            }

            if (b_is_holding && i_fire_mode == 2 && t_fire_time <= 0)
            {
                if (m_module.s_action == "actionSpin" && !b_is_spinning)
                    return;

                FireBolt(m_module.i_bolts_per_shot);
                t_fire_time = m_module.f_shoot_delay;
            }

            if (b_is_bursting && t_burst_delay <= 0)
            {
                FireBolt(m_module.i_bolts_per_shot);
                i_batch--;
                t_burst_delay = m_module.f_burst_delay;

                if (i_batch <= 0)
                {
                    b_is_bursting = false;
                    t_fire_time = m_module.f_shoot_delay * 2;
                    t_burst_delay = m_module.f_burst_delay * 2;
                }
            }
        }

        //-------------------------------------------------------------------------------------------\\

        // Firing

        private void FireChargedMultiShot()
        {
            if (t_fire_time > 0 || m_bullet_spawn_points.IsNullOrEmpty())
                return;

            Transform spawn = m_bullet_spawn_points.FirstOrDefault();

            float angleStep = m_module.i_charged_multi_shot > 1 ? m_module.f_charged_spread / (m_module.i_charged_multi_shot - 1) : 0f;

            float currentAngle = -m_module.f_charged_spread;

            for (int i = 0; i < m_module.i_charged_multi_shot; i++)
            {
                FireChargedBolt(spawn, currentAngle, 0f);
                currentAngle += angleStep;
            }

            if (m_player_hand_grip != null && !b_is_ai && m_player_hand_grip.ragdollHand != null)
            {
                Util.PlayHaptic(m_player_hand_grip.ragdollHand, 1.0f);
            }

            t_fire_time = m_module.f_shoot_delay;
        }

        private void FireChargedBolt(Transform spawn, float yawAngle, float pitchAngle)
        {
            Quaternion rotation = spawn.rotation * Quaternion.Euler(pitchAngle, yawAngle, 0f) * Quaternion.Euler(0, 0, 90);
            SpawnProjectile(m_charged_projectile, spawn.position, rotation);

            if (!b_is_ai && !ModSettings.bPerfectAccuracy && m_body != null)
            {
                m_body.AddRelativeForce(new Vector3(0, 0, -m_module.f_recoil), ForceMode.Impulse);

                float recoilModifier = 5f;

                float recoilTorque = Random.Range(-10 + f_accuracy, 10 - f_accuracy) * recoilModifier;
                m_body.AddRelativeTorque(new Vector3(recoilTorque, Random.Range(-1f, 1f), 0), ForceMode.Impulse);
            }

            if (m_muzzle_flash && i_fire_mode != 3)
            {
                if (m_muzzle_flash.isPlaying)
                    m_muzzle_flash.Stop();
                m_muzzle_flash.Play();
            }
        }

        public void AIFire()
        {
            if (b_is_bursting) return;

            if (ModSettings.bAIFiremode && m_module.f_blaster_modes.Length > 1)
            {
                if (t_ai_fire_time_switch <= 0)
                {
                    SwitchFireMode();
                    if (!ModSettings.bAIStunMode && i_fire_mode == 3)
                        SwitchFireMode();
                    t_ai_fire_time_switch = Random.Range(5, 10);
                }
            }
            else
            {
                i_fire_mode = m_module.f_blaster_modes[0];
            }

            b_is_ai = true;

            switch (i_fire_mode)
            {
                case 0: FireBolt(m_module.i_bolts_per_shot); break;
                case 1: StartBurst(); break;
                case 2: FireBolt(m_module.i_bolts_per_shot); break;
                case 3: FireBolt(m_module.i_bolts_per_shot); break;
            }
        }

        private void FireBolt(int boltsPerShot = 1)
        {
            if (!b_is_bursting && t_fire_time > 0) return;

            if (boltsPerShot > 1)
            {
                f_accuracy -= boltsPerShot * 2;
            }

            RagdollHand hand = null;
            if (!b_is_ai)
            {
                if (m_player_hand_grip != null)
                    hand = m_player_hand_grip.ragdollHand;
                else if (m_grip?.telekinesisHandlers.FirstOrDefault())
                    hand = m_grip.telekinesisHandlers.FirstOrDefault().ragdollHand;
            }

            if (m_shoot_source != null)
            {
                if (i_fire_mode != 3)
                {
                    if (m_module.b_play_batch_sound_once && b_is_bursting && i_batch == m_module.i_burst_bolts)
                    {
                        Util.PlaySound(m_shoot_source, m_module.m_shoot_sounds, ModSettings.fBlasterSoundVolume);
                    }
                    else if (!b_is_bursting || !m_module.b_play_batch_sound_once)
                    {
                        Util.PlaySound(m_shoot_source, m_module.m_shoot_sounds, ModSettings.fBlasterSoundVolume);
                    }
                }
                else // Stun shot
                {
                    Util.PlaySound(m_shoot_source, m_module.m_stun_sounds, ModSettings.fBlasterSoundVolume);
                }
            }

            float spreadAngle = 0f;
            if (i_fire_mode == 1)
            {
                spreadAngle = m_module.f_burst_spread;
            }
            else
            {
                spreadAngle = m_module.f_batch_spread;
            }

            if (ModSettings.bPerfectAccuracy)
                spreadAngle = 0;

            foreach (var spawn in m_bullet_spawn_points)
            {
                if (spawn == null) continue;

                for (int i = 0; i < boltsPerShot; i++)
                {
                    Vector2 circlePoint = Vector2.zero;
                    float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

                    if (boltsPerShot > 1)
                    {
                        float angle = i * (2f * Mathf.PI / boltsPerShot);
                        circlePoint = new Vector2(Mathf.Cos(angle) * spreadRadius, Mathf.Sin(angle) * spreadRadius);
                    }
                    else
                    {
                        circlePoint = Random.insideUnitCircle * spreadRadius;
                    }

                    Vector3 spreadDirection = spawn.forward + spawn.right * circlePoint.x + spawn.up * circlePoint.y;

                    Quaternion finalRotation = Quaternion.LookRotation(spreadDirection);

                    if (i_fire_mode == 3) // Stun bolt
                    {
                        SpawnProjectile(m_projectileStun, spawn.position, finalRotation);
                    }
                    else // Regular bolt
                    {
                        SpawnProjectile(m_projectile, spawn.position, finalRotation);
                    }
                }
            }

            if (!b_is_ai && !ModSettings.bPerfectAccuracy && m_body != null)
            {
                m_body.AddRelativeForce(new Vector3(0, 0, -m_module.f_recoil), ForceMode.Impulse);
                float recoilTorque = Random.Range(-10 + f_accuracy, 10 - f_accuracy);
                m_body.AddRelativeTorque(new Vector3(recoilTorque, Random.Range(-1f, 1f), 0), ForceMode.Impulse);
            }

            if (hand != null && !b_is_ai)
                Util.PlayHaptic(hand, 0.8f);

            if (m_muzzle_flash && i_fire_mode != 3)
            {
                if (m_muzzle_flash.isPlaying)
                    m_muzzle_flash.Stop();
                m_muzzle_flash.Play();
            }
        }

        private void SpawnProjectile(ItemData projectileData, Vector3 position, Quaternion rotation)
        {
            if (projectileData == null) return;

            projectileData.SpawnAsync(projectile =>
            {
                if (projectile == null) return;

                projectile.OnSpawn(null, Item.Owner.None);

                if (m_item)
                {
                    Collider[] weaponColliders = m_item.GetComponentsInChildren<Collider>();
                    Collider projectileCollider = projectile.GetComponentInChildren<Collider>();

                    if (projectileCollider)
                    {
                        foreach (var weaponCollider in weaponColliders)
                        {
                            Physics.IgnoreCollision(projectileCollider, weaponCollider);
                        }
                    }
                }

                projectile.Throw(1, Item.FlyDetection.Forced);

                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.freezeRotation = true;
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    rb.useGravity = false;
                    rb.velocity = rotation * Vector3.forward * (m_module.f_bullet_velocity * ModSettings.fBoltSpeedMultiplier);
                }
            }, position, rotation, null, false, null, Item.Owner.None);
        }

        // Firing Starters

        private void StartBurst()
        {
            if (t_fire_time > 0)
                return;

            i_batch = m_module.i_burst_bolts;
            b_is_bursting = true;
            t_burst_delay = 0;
        }

        private void StartRapidFire()
        {
            if (t_fire_time <= 0)
            {
                if (m_module.s_action == "actionSpin" && f_spin_speed < 1 && !b_is_spinning)
                    return;

                FireBolt(m_module.i_bolts_per_shot);
                t_fire_time = m_module.f_shoot_delay;
            }
        }

        private void ChargeStart()
        {
            if (b_is_charging) return;

            if (m_muzzle_flash)
            {
                var effect = m_muzzle_flash.main;
                var effectChild = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).main;
                var effectChildTransform = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                effect.loop = true;
                m_muzzle_flash.transform.localScale = Vector3.zero;

                effectChild.loop = true;
                effectChildTransform.localScale = Vector3.zero;

                m_muzzle_flash.Play();
            }

            b_is_charging = true;
            f_charge = 0f;

            if (m_animator)
            {
                m_animator.SetBool("Rotating", true);
                m_animator.speed = 0;
            }

            if (m_shoot_source != null && m_module.m_charge_sounds != null)
            {
                Util.PlaySound(m_shoot_source, m_module.m_charge_sounds, 1f);
            }
        }

        private void SpinStart()
        {
            if (b_is_spinning) return;

            b_is_spinning = true;
            f_spin_speed = 0f;

            Util.PlaySoundLooped(m_item.GetComponent<AudioSource>(), m_module.m_charge_sounds, 1);

            if (m_animator)
            {
                m_animator.SetBool("Rotating", true);
            }
        }

        // Firing Stop

        private void ChargeStop()
        {
            if (!b_is_charging) return;

            b_is_charging = false;

            if (m_muzzle_flash && m_module.s_action != "actionSpin")
            {
                var effect = m_muzzle_flash.main;
                var effectChild = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).main;
                var effectChildTransform = m_muzzle_flash.subEmitters.GetSubEmitterSystem(0).gameObject.transform;

                effect.loop = false;
                m_muzzle_flash.transform.localScale = Vector3.one;

                effectChild.loop = false;
                effectChildTransform.localScale = Vector3.one;

                m_muzzle_flash.Stop();
            }

            if (m_animator)
            {
                m_animator.SetBool("Rotating", false);
                m_animator.speed = 0;
            }

            if (f_charge >= m_module.f_charge_time * 0.9f)
            {
                FireChargedMultiShot();

                Util.PlaySound(m_shoot_source, m_module.m_charged_shots, m_module.f_shoot_volume * ModSettings.fBlasterSoundVolume);
            }

            f_charge = 0f;
        }

        private void SpinStop()
        {
            if (!b_is_spinning) return;

            Util.StopLoopedSound(m_item.GetComponent<AudioSource>());

            b_is_spinning = false;
        }

        //-------------------------------------------------------------------------------------------\\

        // Events

        public void GripUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (ragdollHand.playerHand)
                m_player_hand_grip = null;
        }

        public void GripGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (ragdollHand.playerHand)
                m_player_hand_grip = ragdollHand.playerHand;
        }

        public void ForegripUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (ragdollHand.playerHand)
                m_player_hand_grip = null;

            SpinStop();
        }

        public void OnBlasterGrabbed(Handle handle, RagdollHand ragdollHand)
        {
            if (ragdollHand.playerHand)
            {
                b_is_ai = false;
                if (m_module.b_has_scope)
                {
                    SetScopeRenderer(true);
                }
            }
        }

        public void BlasterTeleGrabbed(Handle handle, SpellTelekinesis teleGrabber)
        {
            m_current_telekinesis = teleGrabber;
        }

        public void BlasterTeleDropped(Handle handle, SpellTelekinesis teleGrabber, bool tryThrow, bool isGrabbing)
        {
            m_current_telekinesis = null;
        }

        public void OnBlasterDropped(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (ragdollHand.playerHand && !m_item.IsHeld() && m_module.b_has_scope)
            {
                SetScopeRenderer(false);
            }

            b_is_holding = false;
            b_is_holding_secondary = false;
            ChargeStop();
            SpinStop();
        }

        public void OnBlasterAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == m_grip)
            {
                HandleGripActions(ragdollHand, action);
            }
            else if (m_module.b_has_scope && m_scopegrip && handle == m_scopegrip && action == Interactable.Action.UseStart)
            {
                SwitchScopeZoom();
            }
            else if (m_foregrip && handle == m_foregrip)
            {
                HandleForegripActions(ragdollHand, action);
            }
        }

        private void HandleForegripActions(RagdollHand ragdollHand, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                if (m_module.s_action == "actionSpin")
                {
                    SpinStart();
                }
            }
            else if (action == Interactable.Action.UseStop)
            {
                if (m_module.s_action == "actionSpin")
                {
                    SpinStop();
                }
            }
        }

        private void HandleGripActions(RagdollHand ragdollHand, Interactable.Action action)
        {
            if (!string.IsNullOrEmpty(m_module.s_action_held))
            {
                if (action == Interactable.Action.UseStart)
                {
                    t_held_time = 0.4f;
                }
                else if (action == Interactable.Action.UseStop)
                {
                    if (t_held_time > 0.2f)
                    {
                        HandleAction("actionShoot", ragdollHand);
                    }
                    else
                    {
                        if (m_module.s_action_held == "actionCharge")
                            ChargeStop();
                    }
                    t_held_time = 0.0f;
                }
            }
            else if (i_fire_mode == 2) // Automatic fire
            {
                if (action == Interactable.Action.UseStart)
                {
                    b_is_holding = true;
                    StartRapidFire();
                }
                else if (action == Interactable.Action.UseStop)
                {
                    b_is_holding = false;
                }
            }
            else if (action == Interactable.Action.UseStart)
            {
                HandleAction("actionShoot", ragdollHand);
            }

            if (!string.IsNullOrEmpty(m_module.s_action_secondary) && action == Interactable.Action.AlternateUseStart)
            {
                HandleAction(m_module.s_action_secondary, ragdollHand);
            }
        }

        public void HandleAction(string action, RagdollHand ragdollHand)
        {
            switch (action)
            {
                case "actionSwitchFireMode":
                    SwitchFireMode();
                    break;
                case "actionShoot":
                    if (t_fire_time > 0) break;
                    switch (i_fire_mode)
                    {
                        case 0: // Single shot
                            FireBolt(m_module.i_bolts_per_shot);
                            t_fire_time = m_module.f_shoot_delay;
                            break;
                        case 1: // Burst
                            StartBurst();
                            break;
                        case 3: // Stun
                            FireBolt(m_module.i_bolts_per_shot);
                            t_fire_time = m_module.f_shoot_delay;
                            break;
                    }
                    break;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        // Scope

        private void SetupScope()
        {
            if (!m_module.b_has_scope) return;

            if (m_scope_camera && m_scope_renderer)
            {
                CreateRenderTexture();
                m_scope_camera.fieldOfView = m_module.f_scope_fovs[i_scope_mode];
            }
        }

        public void SetScopeRenderer(bool state)
        {
            if (m_scope_renderer != null)
                m_scope_renderer.enabled = state;

            if (m_scope_camera != null)
                m_scope_camera.enabled = state;

            if (m_scope_texture != null)
            {
                if (state)
                {
                    if (!m_scope_texture.IsCreated())
                        m_scope_texture.Create();

                    if (m_scope_camera != null)
                        m_scope_camera.targetTexture = m_scope_texture;
                }
                else
                {
                    if (m_scope_camera != null)
                        m_scope_camera.targetTexture = null;

                    if (m_scope_texture.IsCreated())
                        m_scope_texture.Release();
                }
            }
        }

        public void CreateRenderTexture()
        {
            if (!m_scope_camera || !m_scope_renderer)
                return;

            m_scope_camera.enabled = false;

            if (m_scope_texture != null)
            {
                m_scope_texture.Release();
            }

            i_old_scope_res = ModSettings.iBlasterScopeResolution;
            m_scope_texture = new RenderTexture(i_old_scope_res, i_old_scope_res, 24, RenderTextureFormat.ARGB32);
            m_scope_texture.Create();
            m_scope_camera.targetTexture = m_scope_texture;

            MaterialPropertyBlock scopeBlock = new MaterialPropertyBlock();
            m_scope_renderer.GetPropertyBlock(scopeBlock);
            scopeBlock.SetTexture("_RenderTexture", m_scope_texture);
            m_scope_renderer.SetPropertyBlock(scopeBlock);
        }

        public void SwitchScopeZoom()
        {
            i_scope_mode = (i_scope_mode + 1) % m_module.f_scope_fovs.Length;
            m_scope_camera.fieldOfView = m_module.f_scope_fovs[i_scope_mode];
            UpdateSaveData();
        }

        private void UpdateScope()
        {
            if (!m_module.b_has_scope || m_scope_texture == null) return;

            if (ModSettings.iBlasterScopeResolution != m_scope_texture.width)
            {
                m_scope_texture.Release();
                m_scope_texture.width = ModSettings.iBlasterScopeResolution;
                m_scope_texture.height = ModSettings.iBlasterScopeResolution;
                m_scope_texture.Create();
                m_scope_camera.targetTexture = m_scope_texture;
            }
        }

        //-------------------------------------------------------------------------------------------\\

        // Save Data

        private void LoadSaveData()
        {
            if (m_item.TryGetCustomData(out BlasterSaveData blasterData))
            {
                i_fm_index = blasterData.i_fire_mode;
                i_scope_mode = blasterData.i_scope_zoom;
                s_blaster_bolt = blasterData.s_blaster_bolt;
                i_fire_mode = m_module.f_blaster_modes[i_fm_index];
                SetColors(s_blaster_bolt);
            }
            else
            {
                s_blaster_bolt = m_module.s_shoot_bolt;
                SetColors(m_module.s_shoot_bolt);
                i_fire_mode = m_module.f_blaster_modes[0];
            }
        }

        public void UpdateSaveData()
        {
            var saveData = new BlasterSaveData();
            saveData.s_blaster_bolt = s_blaster_bolt;
            saveData.i_fire_mode = i_fm_index;
            saveData.i_scope_zoom = i_scope_mode;

            Util.CleanCustomDataProperly<BlasterSaveData>(m_item);
            m_item.AddCustomData(saveData);
        }

        public void SetColors(string projectile)
        {
            if (!string.IsNullOrEmpty(projectile))
            {
                m_projectile = Catalog.GetData<ItemData>(projectile + m_module.s_bolt_override);
            }
            else
            {
                m_projectile = Catalog.GetData<ItemData>(m_module.s_shoot_bolt + m_module.s_bolt_override);
            }

            if (m_projectile == null)
                m_projectile = Catalog.GetData<ItemData>(m_module.s_shoot_bolt + m_module.s_bolt_override);

            if (!string.IsNullOrEmpty(m_module.s_charged_projectile))
            {
                m_charged_projectile = Catalog.GetData<ItemData>(projectile + m_module.s_charged_override);
            }
            else
            {
                m_charged_projectile = m_projectile;
            }

            if (m_muzzle_flash && m_projectile.data.GetModule<ModuleBlasterBolt>() != null)
            {
                var muzzle = m_muzzle_flash.main;

                var fColor = m_projectile.data.GetModule<ModuleBlasterBolt>().f_color;

                Color color = new Color(fColor[0], fColor[1], fColor[2], fColor[3]);

                muzzle.startColor = color;
            }

            m_projectileStun = Catalog.GetData<ItemData>("BlasterBoltStun");
            s_blaster_bolt = projectile;
            UpdateSaveData();
        }

        private void SwitchFireMode()
        {
            if (i_fm_index >= m_module.f_blaster_modes.Length - 1)
            {
                i_fm_index = 0;
            }
            else
            {
                i_fm_index += 1;
            }

            i_fire_mode = m_module.f_blaster_modes[i_fm_index];
            b_is_holding = false;
            UpdateSaveData();
        }

    }
}