using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ThunderRoad;
using ThunderRoad.DebugViz;
using ThunderRoad.Skill.SpellPower;
using Unity.XR.CoreUtils;
using UnityEngine;
using static FadeMixerGroup;
using static ThunderRoad.HandleRagdollData;

namespace TheNomadRim
{
    public class ItemLightsaber : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update | ManagedLoops.FixedUpdate;

        protected Item m_item;
        protected ModuleLightsaber m_module;
        protected Rigidbody m_body;

        private List<string> s_kyber_crystals = new List<string>();

        private bool b_is_active;
        private LightsaberBlade[] m_blades;

        private bool b_thrown;
        private bool b_returning;

        private PlayerHand m_player_hand;
        private PlayerHand m_second_hand;

        private Creature m_current_creature;

        private SpellTelekinesis m_current_telekinesis;

        private GameObject m_joint_object;
        private Collider m_joint_collider;
        private Item m_joined_lightsaber;
        public bool b_is_connected;

        private float t_crystal_change_delay;
        private float t_deactivate;
        private float t_action_held;

        private bool b_spin_state;
        private bool b_last_spin;

        private bool b_is_saber_spinning;

        private Handle.SlideBehavior origSlide;
        
        private bool b_is_single_toggled;

        private float f_last_ignite_speed;

        private Animator m_animator;

        private Dictionary<LightsaberBlade, SwingSorcery> m_swing_sorceries = new Dictionary<LightsaberBlade, SwingSorcery>();

        private Vector3 v_spin_axis;
        private float f_angular_velocity;
        private bool b_spin_initialized = false;
        private Quaternion q_initial_rotation;
        private Vector3 v_initial_velocity;

        private float t_ignore_collider;

        private float f_angular_drag;
        private float f_angular_velocity_max;

        private bool b_animator_state;

        protected void Awake()
        {
            m_item = this.GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleLightsaber>();
            m_body = m_item.GetComponent<Rigidbody>();

            m_item.OnGrabEvent += LightsaberGrabbed;
            m_item.OnUngrabEvent += LightsaberDropped;
            m_item.OnHeldActionEvent += LightsaberAction;
            m_item.OnTelekinesisGrabEvent += LightsaberTeleGrabbed;
            m_item.OnTelekinesisReleaseEvent += LightsaberTeleDropped;

            origSlide = m_item.handles.FirstOrDefault().slideBehavior;

            f_angular_drag = m_item.data.angularDrag;
            f_angular_velocity_max = m_body.maxAngularVelocity;

            m_animator = m_item.gameObject.GetComponent<Animator>();

            m_blades = m_module.m_lightsaber_blades.Select(x => new LightsaberBlade
            {
                s_kyber_crystal_id = x.s_kyber_crystal_id,
                f_blade_length_multiplier = x.f_blade_length_multiplier,
                f_blade_width_multiplier = x.f_blade_width_multiplier
            }).ToArray();

            m_item.TryGetCustomData(out LightsaberSaveData lightsaberData);

            if (lightsaberData != null)
            {
                s_kyber_crystals = lightsaberData.s_kyber_crystals;
                DebugService.LogInfo($"Loading lightsaber ({m_item.name}) with saved data");
            }

            m_joint_object = m_item.gameObject.GetNamedChild("JoinPart");

            if (m_joint_object)
            {
                m_joint_collider = m_joint_object.GetComponent<Collider>();
                m_joint_collider.isTrigger = true;

                if (lightsaberData != null && lightsaberData.m_connected != null)
                {
                    ConnectedLightsaber ls = lightsaberData.m_connected;

                    Catalog.GetData<ItemData>(ls.s_id).SpawnAsync(item => {

                        item.OnSpawn(null, Item.Owner.Player);
                        item.SetOwner(Item.Owner.Player);

                        var euler = item.transform.rotation.eulerAngles;
                        item.transform.rotation = Quaternion.Euler(euler);

                        ConnectLightsaber(item);

                        ItemLightsaber lsItem = item.GetComponent<ItemLightsaber>();
                        if (lsItem == null) return;

                        for (int i = 0; i < Mathf.Min(lsItem.m_blades.Length, ls.s_kyber_crystals.Count); i++)
                        {
                            lsItem.m_blades[i].s_kyber_crystal_id = ls.s_kyber_crystals[i];
                            lsItem.m_blades[i].SetCrystal();
                            lsItem.m_blades[i].SetBladeLength(ls.f_lengths[i]);
                        }
                        lsItem.UpdateSaveData();
                    });
                    
                }

            }


            for (int i = 0; i < m_blades.Length; i++)
            {
                var currentBlade = m_blades[i];

                currentBlade.m_crystal_spawned += () =>
                {
                    SetUpSwingSorcery(currentBlade);
                };

                currentBlade.Init(m_item, i, s_kyber_crystals);

                if (lightsaberData != null)
                    if (!lightsaberData.f_lengths.IsNullOrEmpty())
                        currentBlade.SetBladeLength(lightsaberData.f_lengths[i]);

                Global.allBlades.Add(currentBlade);
            }
            f_last_ignite_speed = ModSettings.fLightsaberIgniteSpeed;

            DebugService.LogInfo($"Initialized {m_blades.Count()} blades");

            Extinguish(false);

            UpdateSaveData();
        }

        void OnTriggerEnter(Collider other)
        {
            if (ModSettings.bLightsaberJoining && m_item.IsHeldByPlayer &&
                m_joint_object != null &&
                other.name == "JoinPart" &&
                !b_is_connected && m_joined_lightsaber == null &&
                t_ignore_collider <= 0)
            {
                ItemLightsaber ls = other.GetComponentInParent<ItemLightsaber>();
                if (ls != null && ls != this &&
                    ls.m_joint_object != null && ls.m_joint_collider &&
                    ls.m_item.IsHeldByPlayer)
                {
                    ConnectLightsaber(ls.m_item);
                }
            }
        }

        private void DisconnectLightsaber(bool destroy = false)
        {
            if (m_joined_lightsaber == null)
                return;

            m_joined_lightsaber.transform.SetParent(null, true);

            Destroy(this.GetComponent<FixedJoint>());
            
            SetJointCollider(true);

            b_is_connected = false;
            m_joined_lightsaber.GetComponent<ItemLightsaber>().b_is_connected = false;

            m_joined_lightsaber = null;

            t_ignore_collider = 1f;

            if (!destroy)
                UpdateSaveData();
        }

        private void ConnectLightsaber(Item lightsaber)
        {
            if (lightsaber == null) return;

            var otherLs = lightsaber.GetComponent<ItemLightsaber>();
            if (otherLs == null || otherLs.m_joint_object == null) return;

            lightsaber.leftPlayerHand?.ragdollHand?.TryRelease();
            lightsaber.rightPlayerHand?.ragdollHand?.TryRelease();
            
            otherLs.transform.SetParent(otherLs.transform, true);

            Transform itemHolderPoint = otherLs.m_joint_object.transform;

            Transform alignmentPoint = m_joint_object.transform;

            Quaternion holderStartLocal = itemHolderPoint.localRotation;

            itemHolderPoint.localEulerAngles = Vector3.zero;

            Vector3 worldHolderPos = lightsaber.transform.TransformPoint(
                lightsaber.transform.InverseTransformPoint(itemHolderPoint.position));

            Vector3 resultPoint = alignmentPoint.TransformPoint(
                itemHolderPoint.InverseTransformPoint(worldHolderPos)
            );

            lightsaber.transform.MoveAlign(
                itemHolderPoint,
                resultPoint,
                alignmentPoint.rotation,
                alignmentPoint
            );

            itemHolderPoint.localRotation = holderStartLocal;

            if (Vector3.Dot(itemHolderPoint.up, alignmentPoint.up) > 0.5f)
            {
                lightsaber.transform.RotateAround(itemHolderPoint.position, itemHolderPoint.up, 180f);
            }

            FixedJoint joint = this.GetOrAddComponent<FixedJoint>();
            joint.connectedBody = otherLs.m_body;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            m_joined_lightsaber = lightsaber;
            b_is_connected = true;
            otherLs.b_is_connected = true;
            t_ignore_collider = 1f;
            SetJointCollider(false);

            UpdateSaveData();
        }

        public void SetJointCollider(bool toggle)
        {
            m_joint_collider.enabled = toggle;
            ItemLightsaber ls = m_joined_lightsaber?.GetComponent<ItemLightsaber>();
            ls.m_joint_collider.enabled = toggle;
        }


        private void SetUpSwingSorcery(LightsaberBlade blade)
        {
            if (blade == null || blade.m_accent_point == null) return;

            try
            {
                SwingSorcery whoosh;

                if (m_swing_sorceries.ContainsKey(blade) && m_swing_sorceries[blade] != null)
                {
                    whoosh = m_swing_sorceries[blade];
                }
                else
                {
                    var existing = blade.m_accent_point.GetComponent<SwingSorcery>();
                    if (existing != null) Destroy(existing);

                    whoosh = blade.m_accent_point.GetOrAddComponent<SwingSorcery>();
                }

                var swingSorcery = new SwingSorcery.SwingSorceryData();
                SetUpSwingSorceryEffects(swingSorcery, blade);

                whoosh.Initialize(blade, swingSorcery, m_body, m_item);
                m_swing_sorceries[blade] = whoosh;
            }
            catch (Exception e)
            {
                DebugService.Log($"Failed setup : {e}", "SwingSorcery Error");
            }
        }

        protected void OnDestroy()
        {
            for (int i = 0, b = m_blades.Count(); i < b; i++)
            {
                Global.allBlades.Remove(m_blades[i]);
            }

            DisconnectLightsaber(true);
        }

        protected override void ManagedFixedUpdate()
        {
            base.ManagedFixedUpdate();

            if (m_animator != null && b_is_saber_spinning && m_player_hand != null)
            {
                var hand = PlayerControl.GetHand(m_player_hand.side);
                var creature = Player.local.locomotion;

                float force = hand.useAxis;

                if (b_is_active)
                {
                    creature.physicBody.AddForce(-m_body.transform.right * Mathf.Lerp(0, 800, force), ForceMode.Force);
                }

                m_animator.speed = 1 + force;
            }
        }

        protected override void ManagedUpdate()
        {
            foreach (var blade in m_blades)
            {
                blade.UpdateSize();
                if (blade.f_current_length <= 0 && blade.lightsaberBlade.gameObject.activeInHierarchy && !blade.b_is_active)
                {
                    blade.m_idle_src.Stop();
                    blade.SetActive(false);
                }
            }

            if (m_player_hand && b_thrown && !m_item.IsHeld() && PlayerControl.GetHand(m_player_hand.side).gripPressed && m_current_telekinesis == null && Player.local.creature && !Player.local.creature.isKilled)
            {
                if (IsHandOccupied(m_player_hand))
                    m_player_hand = null;
                else
                {
                    if (!b_returning)
                    {
                        if (ModSettings.bActivateOnRecall)
                            Ignite(true);
                        UnpenetrateAll();
                    }

                    t_deactivate = -1;
                    b_returning = true;

                    m_item.isThrowed = true;

                    RotateLightsaber();
                    ReturnToHand(PlayerControl.GetHand(m_player_hand.side).gripAxis);

                }
            }

            if (b_thrown && !m_item.IsHeld() && m_current_telekinesis == null)
            {
                RotateLightsaber();
            }

            if (ModSettings.bDeactivateOnDrop && !m_item.IsHeld() && !m_item.isTelekinesisGrabbed)
            {
                if (t_deactivate <= -1)
                {
                    b_returning = false;
                    t_deactivate = ModSettings.fDeactivateDelay;
                }
            }

            if (m_item.IsHeld() && m_current_telekinesis == null)
            {
                b_thrown = false;
                b_returning = false;

                if (ModSettings.bBetterCollisions)
                {
                    bool isVelocityMin = m_body.velocity.magnitude > ModSettings.fBetterCollisionsVelocity;
                    m_body.collisionDetectionMode = isVelocityMin ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.ContinuousSpeculative;
                }
                else
                {
                    m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
            }
            else if (m_current_telekinesis != null && m_current_telekinesis.spinMode)
            {
                if (b_is_active)
                {
                    if (b_last_spin == true)
                    {
                        b_spin_state = false;
                    }
                    else
                    {
                        if (b_spin_state == false)
                        {
                            m_current_telekinesis.SetSpinMode(false);
                            Extinguish(true);
                        }
                    }
                }
                else
                {
                    m_current_telekinesis.SetSpinMode(false);
                    Ignite(true);
                    b_spin_state = true;
                    b_last_spin = true;
                }
            }

            if (m_current_telekinesis != null && b_last_spin != m_current_telekinesis.spinMode)
            {
                b_last_spin = m_current_telekinesis.spinMode;
            }

            if (f_last_ignite_speed != ModSettings.fLightsaberIgniteSpeed)
            {
                f_last_ignite_speed = ModSettings.fLightsaberIgniteSpeed;

                foreach (var blade in m_blades)
                {
                    blade.UpdatedIgnite();
                }
            }

            UpdateTimes();
        }

        public void UpdateSaveData()
        {
            var saveData = new LightsaberSaveData();

            if (m_blades != null)
            {
                foreach (var blade in m_blades)
                {
                    saveData.s_kyber_crystals.Add(blade.s_kyber_crystal_id);
                    saveData.f_lengths.Add(blade.f_max_length);
                    DebugService.LogInfo($"Adding {blade.s_kyber_crystal_id} with length {blade.f_max_length} to save data");
                }
            }

            if (m_joined_lightsaber != null)
            {
                var itemLightsaber = m_joined_lightsaber.GetComponent<ItemLightsaber>();

                itemLightsaber.UpdateSaveData();

                itemLightsaber.m_item.TryGetCustomData<LightsaberSaveData>(out LightsaberSaveData connectedData);

                saveData.m_connected = new ConnectedLightsaber
                {
                    s_id = m_joined_lightsaber.itemId,
                    f_lengths = connectedData.f_lengths,
                    s_kyber_crystals = connectedData.s_kyber_crystals
                };
            }

            if (m_item.HasCustomData<LightsaberSaveData>())
            {
                DebugService.LogInfo($"Custom Data of type {typeof(LightsaberSaveData).Name} found");

                m_item.RemoveCustomData<LightsaberSaveData>();

                if (m_item.TryGetCustomData(out LightsaberSaveData data))
                {

                    DebugService.LogInfo($"Custom Data still persists, forcing clean-up now ...");

                    m_item.contentCustomData.Remove(data);
                    m_item.OverrideCustomData(m_item.contentCustomData);
                }
            }

            m_item.AddCustomData(saveData);

            DebugService.LogInfo("Updated custom save data");
        }


        private void SetUpSwingSorceryEffects(SwingSorcery.SwingSorceryData swing, LightsaberBlade blade)
        {
            if (blade == null)
            {
                return;
            }

            if (blade.m_accent_point == null)
            {
                return;
            }

            if (blade.m_accent_effect == null || blade.m_smoothswing_effect_low == null || blade.m_smoothswing_effect_high == null)
            {
                DebugService.Log("Effect data is missing", "SwingSorcery Error");
                return;
            }

            try
            {
                if (swing.m_accent_swing == null)
                {
                    swing.m_accent_swing = blade.m_accent_effect.Spawn(
                        blade.m_accent_point.transform.position,
                        blade.m_accent_point.transform.rotation,
                        blade.m_accent_point,
                        pooled: false
                    );

                    if (swing.m_accent_swing != null)
                    {
                        swing.m_accent_swing.SetIntensity(0f);
                    }
                    else
                    {
                        DebugService.Log("Accent Swing failed to spawn", "SwingSorcery Warning");
                    }
                }

                if (swing.m_smooth_swing_l == null)
                {
                    swing.m_smooth_swing_l = blade.m_smoothswing_effect_low.Spawn(
                        blade.m_accent_point.transform.position,
                        blade.m_accent_point.transform.rotation,
                        blade.m_accent_point,
                        pooled: false
                    );

                    if (swing.m_smooth_swing_l != null)
                    {
                        swing.m_smooth_swing_l.SetIntensity(0f);
                    }
                    else
                    {
                        DebugService.Log("Smooth Swing Low failed to spawn", "SwingSorcery Warning");
                    }
                }

                if (swing.m_smooth_swing_h == null)
                {
                    swing.m_smooth_swing_h = blade.m_smoothswing_effect_high.Spawn(
                        blade.m_accent_point.transform.position,
                        blade.m_accent_point.transform.rotation,
                        blade.m_accent_point,
                        pooled: false
                    );

                    if (swing.m_smooth_swing_h != null)
                    {
                        swing.m_smooth_swing_h.SetIntensity(0f);
                    }
                    else
                    {
                        DebugService.Log("Smooth Swing High failed to spawn", "SwingSorcery Warning");
                    }
                }
            }
            catch (Exception e)
            {
                DebugService.Log($"Setup failed: {e.Message}", "SwingSorcery Error");
            }
        }

        // Lightsaber Blade Logic

        public void ToggleActivation(RagdollHand hand = null)
        {
            if (b_is_active)
                Extinguish(true);
            else
                Ignite(true);

            if (hand) Util.PlayHaptic(hand, 0.75f);
        }

        public void ToggleSpin(RagdollHand hand = null)
        {
            if (!m_animator)
                return;

            if (b_is_saber_spinning)
                DisableSpin(hand);
            else
                EnableSpin(hand);
        }

        public void EnableSpin(RagdollHand hand = null)
        {
            m_animator.SetBool("Spinning", true);
            m_animator.speed = 1f;
            m_animator.Play("Take 001", 0);
            if (m_animator.layerCount > 1)  m_animator.Play("SpinBlades", 1);

            m_item.handles.FirstOrDefault().slideBehavior = Handle.SlideBehavior.DisallowSlide;

            b_is_saber_spinning = true;
        }

        public void DisableSpin(RagdollHand hand = null)
        {
            m_animator.SetBool("Spinning", false);
            m_animator.speed = 1f;

            m_item.handles.FirstOrDefault().slideBehavior = origSlide;

            b_is_saber_spinning = false;
        }

        public void ToggleSingle(RagdollHand hand = null)
        {
            UnpenetrateAll();

            if (string.IsNullOrEmpty(m_blades[0].s_kyber_crystal_id))
            {
                m_blades[0].TurnOff(false);
                return;
            }

            if (m_blades[0].b_is_active)
            {
                m_blades[0].TurnOff(m_blades[0].b_is_active && !string.IsNullOrEmpty(m_blades[0].s_kyber_crystal_id));
                b_is_single_toggled = false;
                m_blades[0].m_on_disabled_blade?.Invoke();
            }
            else
            {
                m_blades[0].TurnOn(!m_blades[0].b_is_active);
                b_is_single_toggled = true;

                if (m_swing_sorceries.ContainsKey(m_blades[0]) && m_swing_sorceries[m_blades[0]] != null)
                {
                    m_swing_sorceries[m_blades[0]].RestartEffects();
                }
            }

            if (hand) Util.PlayHaptic(hand, 0.75f);
        }

        public void ToggleAnimation()
        {
            if (!m_animator) return;

            b_animator_state = !b_animator_state;

            if (b_animator_state == true)
            {
                m_animator.Play(m_module.s_toggle_animation);
            }
            else
            {
                m_animator.Play(m_module.s_close_animation);
            }
        }

        public void Ignite(bool playSound)
        {
            b_is_active = true;
            b_is_single_toggled = true;

            int enabledCount = 0;

            if (m_animator != null && m_module.b_animate_on_toggle)
            {
                m_animator.SetBool("Open", true);
                m_animator.speed = 1.2f;
                m_animator.Play("Open", 0);
                if (m_animator.layerCount > 1) m_animator.Play("Collisions Open", 1);
            }

            for (int i = 0; i < m_blades.Count(); i++)
            {
                if (string.IsNullOrEmpty(m_blades[i].s_kyber_crystal_id))
                {
                    m_blades[i].TurnOff(m_blades[i].b_is_active);

                    if (i == 0)
                        b_is_single_toggled = false;

                    continue;
                }

                m_blades[i].TurnOn(m_blades[i].b_is_active ? false : playSound);

                if (m_swing_sorceries.ContainsKey(m_blades[i]) && m_swing_sorceries[m_blades[i]] != null)
                {
                    m_swing_sorceries[m_blades[i]].RestartEffects();
                }

                enabledCount++;
            }

            if (enabledCount <= 0)
                b_is_active = false;
        }

        public void Extinguish(bool playSound)
        {
            b_is_active = false;
            b_is_single_toggled = false;

            UnpenetrateAll();

            if (m_animator != null && m_module.b_animate_on_toggle)
            {
                m_animator.SetBool("Open", false);
                m_animator.speed = 1f;
                m_animator.Play("Close", 0);
                if (m_animator.layerCount > 1) m_animator.Play("Collisions Close", 1);
            }

            foreach (var blade in m_blades)
            {
                blade.TurnOff(!string.IsNullOrEmpty(blade.s_kyber_crystal_id) && playSound);
            }
        }

        public void SetCrystal(ItemKyberCrystal crystal)
        {
            if (crystal && t_crystal_change_delay <= 0)
            {
                for (int i = 0; i < m_blades.Count(); i++)
                {
                    if (string.IsNullOrEmpty(m_blades[i].s_kyber_crystal_id))
                    {
                        m_blades[i].SetCrystal(crystal);
                        break;
                    }
                }
            }

            UpdateSaveData();
        }

        public void EjectCrystal()
        {

            for (int i = 0; i < m_blades.Count(); i++)
            {
                if (!string.IsNullOrEmpty(m_blades[i].s_kyber_crystal_id))
                {

                    Extinguish(b_is_active);
                    m_blades[i].RemoveCrystal();
                    t_crystal_change_delay = 0.6f;
                    break;
                }
            }

            UpdateSaveData();
        }

        void UnpenetrateAll()
        {
            foreach (var handler in m_item.collisionHandlers)
            {
                foreach (var collision in handler.collisions)
                {
                    if (collision.damageStruct.penetration != DamageStruct.Penetration.None)
                    {
                        collision.damageStruct.damager.UnPenetrateAll();
                    }
                }
            }
        }

        // Handle Events

        public void LightsaberGrabbed(Handle handle, RagdollHand hand)
        {
            ResetConstraint();
            b_spin_initialized = false;

            if (hand.creature != Player.currentCreature)
            {

                if (ModSettings.bAIAntiDualWielders)
                {

                    if (hand.side == Side.Left)
                    {
                        if (hand.otherHand.grabbedHandle == null || hand.otherHand.grabbedHandle?.item.GetComponent<ItemLightsaber>() == null)
                        {
                            goto proceed;
                        }

                        if (hand.creature.container.containerID == "Jedi" || hand.creature.container.containerID == "JediDual" ||
                            hand.creature.container.containerID == "Sith" || hand.creature.container.containerID == "SithDual")
                        {
                            hand.TryRelease();
                            m_item.Despawn();
                        }
                    }
                }

            proceed:

                m_item.RefreshCollision();

                if (ModSettings.bAIForce1Blade && m_blades.Length == 2)
                    ToggleSingle(null);
                else
                    Ignite(true);
            }


            m_player_hand = hand.playerHand;

            if (!m_current_creature)
                m_current_creature = hand.creature;

            b_thrown = false;
            b_returning = false;

            t_deactivate = -1;
            if (m_joined_lightsaber != null)
                m_joined_lightsaber.GetComponent<ItemLightsaber>().t_deactivate = -1;
        }


        public void LightsaberDropped(Handle handle, RagdollHand hand, bool throwing)
        {
            ResetConstraint();
            b_spin_initialized = false;

            if (hand.creature != Player.currentCreature)
                Extinguish(true);

            if (!b_thrown && m_body.velocity.magnitude > ModSettings.fSaberThrowVelocity)
            {
                m_player_hand = hand.playerHand;
                m_item.Throw(1, Item.FlyDetection.Forced);
                b_thrown = true;
            }

            if (!m_item.IsHeld() && (m_joined_lightsaber == null || (m_joined_lightsaber != null && !m_joined_lightsaber.IsHeld())))
            {
                if (b_is_connected && m_joined_lightsaber == null) return;

                t_deactivate = ModSettings.fDeactivateDelay;
                m_current_creature = null;

                if (m_joined_lightsaber != null)
                    m_joined_lightsaber.GetComponent<ItemLightsaber>().t_deactivate = ModSettings.fDeactivateDelay;
            }
        }


        public void LightsaberAction(RagdollHand hand, Handle handle, Interactable.Action action)
        {

            if (!string.IsNullOrEmpty(m_module.s_held_action) || b_is_connected)
            {
                if (action == Interactable.Action.AlternateUseStart)
                {
                    t_action_held = 0.4f;
                }
                else if (action == Interactable.Action.AlternateUseStop)
                {
                    if (t_action_held > 0.2f)
                    {
                        HandleAction(m_module.s_action, hand);
                    }
                    t_action_held = 0.0f;
                }

                return;
            }

            if (action == Interactable.Action.AlternateUseStart)
                HandleAction(m_module.s_action, hand);
        }

        public void LightsaberTeleGrabbed(Handle handle, SpellTelekinesis teleGrabber)
        {
            ResetConstraint();
            b_spin_initialized = false;

            m_current_telekinesis = teleGrabber;

            t_deactivate = -1;
            if (m_joined_lightsaber != null)
                m_joined_lightsaber.GetComponent<ItemLightsaber>().t_deactivate = -1;
        }


        public void LightsaberTeleDropped(Handle handle, SpellTelekinesis teleGrabber, bool tryThrow, bool isGrabbing)
        {
            ResetConstraint();
            b_spin_initialized = false;

            m_current_telekinesis = null;

            m_player_hand = teleGrabber.spellCaster.ragdollHand.playerHand;
            b_thrown = true;
            m_item.Throw(1, Item.FlyDetection.Forced);

            if (!m_item.IsHeld() && (m_joined_lightsaber == null || (m_joined_lightsaber != null && !(m_joined_lightsaber.IsHeld() || m_joined_lightsaber.isTelekinesisGrabbed))))
            {
                if (b_is_connected && m_joined_lightsaber == null) return;

                t_deactivate = ModSettings.fDeactivateDelay;

                if (m_joined_lightsaber != null)
                    m_joined_lightsaber.GetComponent<ItemLightsaber>().t_deactivate = ModSettings.fDeactivateDelay;
            }
        }


        // Helpers

        public void HandleAction(string action, RagdollHand hand = null)
        {
            switch (action)
            {
                case "actionToggle":
                    ToggleActivation(hand);
                    break;
                case "actionEject":
                    EjectCrystal();
                    break;
                case "toggleSingle":
                    ToggleSingle(hand);
                    break;
                case "toggleSpin":
                    ToggleSpin(hand);
                    break;
                case "toggleAnimation":
                    ToggleAnimation();
                    break;
            }
        }

        public bool IsHandOccupied(PlayerHand hand)
        {
            return hand.ragdollHand?.grabbedHandle || hand.ragdollHand?.caster.telekinesis?.catchedHandle;
        }

        void UpdateTimes()
        {
            if (t_crystal_change_delay > 0)
                t_crystal_change_delay -= Time.deltaTime;

            if (t_action_held > 0)
            {
                t_action_held -= Time.deltaTime;
                if (t_action_held <= 0)
                    if (!b_is_connected) HandleAction(m_module.s_held_action);
                    else DisconnectLightsaber();
            }

            if (ModSettings.bDeactivateOnDrop && t_deactivate > 0)
            {
                t_deactivate -= Time.deltaTime;
                if (t_deactivate <= 0 && (b_is_active || b_is_single_toggled))
                    Extinguish(true);
            }

            if (!b_is_connected && t_ignore_collider > 0)
            {
                t_ignore_collider -= Time.deltaTime;
            }
        }

        void ReturnToHand(float gripMult)
        {
            Vector3 handPosition = m_player_hand.transform.position;
            Vector3 itemPosition = transform.position;
            float distanceSqr = (itemPosition - handPosition).sqrMagnitude;

            if (distanceSqr < 0.09f)
            {
                var hand = m_player_hand.ragdollHand;
                if (hand.grabbedHandle)
                    hand.TryRelease();
                hand.Grab(m_player_hand.side == Side.Left ? m_item.mainHandleLeft : m_item.mainHandleRight);
                ResetConstraint();
                m_player_hand = null;
                m_item.isThrowed = false;
            }
            else
            {
                m_body.velocity = (handPosition - m_body.position) * (gripMult == 1f ? 10 * ModSettings.fRecallSpeedMult : (5f * gripMult * ModSettings.fRecallSpeedMult));
                m_item.Throw(1, Item.FlyDetection.Forced);
            }
        }

        private void RotateLightsaber()
        {
            if (!b_is_active || !ModSettings.bLightsaberSpinning || m_body.velocity.sqrMagnitude < 1.5f)
            {
                ResetConstraint();
                b_spin_initialized = false;
                return;
            }

            if (!b_spin_initialized)
            {
                m_item.IgnoreRagdollCollision(Player.currentCreature.ragdoll);

                v_initial_velocity = m_body.velocity;
                q_initial_rotation = transform.rotation;

                v_spin_axis = CalculateStableSpinAxis();

                f_angular_velocity = ModSettings.fLightsaberSpinningSpeed * v_initial_velocity.magnitude;

                m_body.maxAngularVelocity = 1000f;
                m_body.useGravity = false;
                m_body.angularDrag = 0f;
                m_body.angularVelocity = v_spin_axis * f_angular_velocity;

                b_spin_initialized = true;
            }
            else
            {
                RotateSpin();
            }
        }

        private Vector3 CalculateStableSpinAxis()
        {
            Vector3 velocityDir = v_initial_velocity.normalized;
            Vector3 bladeForward = transform.forward;

            Vector3 idealAxis = Vector3.Cross(bladeForward, velocityDir).normalized;

            if (idealAxis.sqrMagnitude < 0.1f)
            {
                Vector3 cameraForward = Camera.main.transform.forward;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocityDir, Vector3.up).normalized;
                idealAxis = Vector3.Cross(cameraForward, horizontalVelocity).normalized;
            }

            if (idealAxis.sqrMagnitude < 0.1f)
            {
                idealAxis = Vector3.Cross(Vector3.up, velocityDir).normalized;
            }

            if (idealAxis.sqrMagnitude < 0.1f)
            {
                idealAxis = transform.right;
            }

            return idealAxis;
        }

        private void RotateSpin()
        {
            m_body.angularVelocity = v_spin_axis * f_angular_velocity;

            Vector3 currentForward = transform.forward;

            Vector3 unwantedRotation = Vector3.Project(m_body.angularVelocity, currentForward);
            m_body.angularVelocity -= unwantedRotation * 10f * Time.deltaTime;

            Vector3 currentSpin = Vector3.ProjectOnPlane(m_body.angularVelocity, v_spin_axis);
            if (currentSpin.magnitude < f_angular_velocity * 0.8f)
            {
                m_body.AddTorque(v_spin_axis * f_angular_velocity * 5f * Time.deltaTime,
                                ForceMode.VelocityChange);
            }
        }

        private void ResetConstraint()
        {
            if (b_spin_initialized)
            {
                if (!m_item.IsHeld())
                {
                    m_item.ResetRagdollCollision();
                    m_item.ResetColliderCollision();
                }

                m_body.angularVelocity = Vector3.zero;
                m_body.useGravity = true;
                m_body.maxAngularVelocity = f_angular_velocity_max;
                m_body.angularDrag = f_angular_drag;
            }
        }


        // Lightsaber length adjuster

        public void IncreaseLenght()
        {
            foreach (var blade in m_blades)
            {
                blade.AddBladeLength(ModSettings.fLengthAdjusterAdjusted);
            }

            UpdateSaveData();
        }

        public void DecreaseLenght()
        {
            foreach (var blade in m_blades)
            {
                if (blade.f_max_length - ModSettings.fLengthAdjusterAdjusted > 0.05)
                    blade.AddBladeLength(-ModSettings.fLengthAdjusterAdjusted);
            }

            UpdateSaveData();
        }

        public void ResetLength()
        {
            foreach (var blade in m_blades)
            {
                blade.ResetBladeLength();
            }

            UpdateSaveData();
        }

    }


    //-------------------------------------------------------------------------------------------\\

    [Serializable]
    public class LightsaberBlade
    {

        public GameObject m_blade_collisions;
        public WhooshPoint m_whoosh_point;
        public Transform m_accent_point;
        public GameObject m_eject_point;

        public ModuleLightsaber module;

        public AudioSource m_toggle_src;
        public AudioSource m_idle_src;
        public float f_off_volume, f_on_volume, f_idle_volume;
        public float f_off_pitch, f_on_pitch, f_idle_pitch;

        public AudioContainer m_on_sound;
        public AudioContainer m_off_sound;
        public AudioContainer m_idle_sound;

        public MeshRenderer m_core_renderer;
        public MeshRenderer m_glow_renderer;

        public Light m_light;
        public Light m_lightTop;

        // Trail

        public GameObject m_tip;
        public GameObject m_bottom;
        public MeshRenderer m_trail_renderer;

        public LightsaberTrail m_trail;

        public EffectData m_accent_effect;
        public EffectData m_smoothswing_effect_low;
        public EffectData m_smoothswing_effect_high;

        public Item m_item;

        public float f_old_length;
        public float f_old_width;

        public float f_current_length;
        public float f_max_length;
        public float f_extend;
        public bool b_is_active;

        public float f_original_whoosh_max;
        public float f_original_whoosh_min;

        private float f_saber_distance;

        public string s_kyber_crystal_id;
        public float f_blade_width_multiplier = 1.0f;
        public float f_blade_length_multiplier = 1.0f;

        public Action m_on_disabled_blade;
        public Action m_crystal_spawned;

        public bool isGradient = false;
        public bool isSmoke = false;

        public static float gradientTiling;
        public static float smokeTiling;

        public int i_id;

        public bool b_has_updated_dist;

        public Transform lightsaberBlade = null;

        private Damager slashDamage = null;
        private Damager pierceDamage = null;

        private float originalSlashLength;
        private float originalPierceDepth;


        private void GetLightsaberBladeData()
        {
            Transform visuals = null;
            foreach (var obj in lightsaberBlade.GetComponentsInChildren<Transform>(true))
            {
                if (obj.name == "LightsaberVisuals")
                {
                    visuals = obj;
                    break;
                }
            }

            foreach (var dmg in lightsaberBlade.GetComponentsInChildren<Damager>())
            {
                if (dmg.direction == Damager.Direction.All || dmg.direction == Damager.Direction.ForwardAndBackward)
                    slashDamage = dmg;
                else if (dmg.direction == Damager.Direction.Forward)
                    pierceDamage = dmg;
            }

            originalPierceDepth = pierceDamage.penetrationDepth;
            originalSlashLength = slashDamage.penetrationLength;

            visuals.gameObject.SetActive(true);

            m_core_renderer = visuals?.gameObject.GetNamedChild("Core")?.GetComponent<MeshRenderer>();
            if (!m_core_renderer) DebugService.LogWarning($"Core Renderer not found (id:{i_id})");

            m_glow_renderer = visuals?.gameObject.GetNamedChild("OuterGlow")?.GetComponent<MeshRenderer>();


            m_light = visuals?.gameObject.GetNamedChild("GlowLight")?.GetComponent<Light>();
            if (!m_light) DebugService.LogWarning($"Glow Light not found (id:{i_id})");

            m_lightTop = visuals?.gameObject.GetNamedChild("GlowLightTop")?.GetComponent<Light>();
            if (!m_lightTop) DebugService.LogWarning($"Glow Light (TOP) not found (id:{i_id})");

            if (m_light) m_light.shadows = ModSettings.bLightsaberShadows ? LightShadows.Hard : LightShadows.None;

            m_idle_src = lightsaberBlade?.gameObject.GetNamedChild("IdleSound")?.GetComponent<AudioSource>();
            if (!m_idle_src) DebugService.LogWarning($"Idle Sound not found (id:{i_id})");
            else m_idle_src.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);


            m_bottom = lightsaberBlade?.gameObject.GetNamedChild("Bottom");
            if (!m_bottom) DebugService.LogWarning($"Bottom object not found (id:{i_id})");

            m_tip = lightsaberBlade?.gameObject.GetNamedChild("Tip");
            if (!m_tip) DebugService.LogWarning($"Tip object not found (id:{i_id})");


            var trail = lightsaberBlade?.gameObject.GetNamedChild("Trail");
            if (trail)
            {
                m_trail = trail.gameObject.AddComponent<LightsaberTrail>();
                m_trail_renderer = trail.gameObject.GetComponent<MeshRenderer>();
                m_trail_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!m_trail_renderer) DebugService.LogWarning($"Trail Renderer not found (id:{i_id})");
            }
            else
            {
                m_trail = null;
                m_trail_renderer = null;
            }

            m_blade_collisions = lightsaberBlade?.gameObject.GetNamedChild("BladeCollider");
            if (!m_blade_collisions) DebugService.LogWarning($"Blade Collisions not found (id:{i_id})");
            foreach (var col in m_blade_collisions.GetComponentsInChildren<Collider>(true))
                col.gameObject.SetActive(true);


            m_whoosh_point = lightsaberBlade?.gameObject.GetNamedChild("Whoosh" + i_id)?.GetComponent<WhooshPoint>();
            if (m_whoosh_point != null)
            {
                m_accent_point = m_whoosh_point.transform;
            }
            else
            {
                DebugService.LogWarning($"No WhooshPoint found");
            }

            m_eject_point = m_item.gameObject.GetNamedChild("EjectPoint" + i_id);
            if (!m_eject_point) DebugService.LogWarning($"Eject Point not found (id:{i_id})");
        }

        public void Init(Item item, int id, List<string> kyberCrystalsOverride = null)
        {
            DebugService.LogInfo("Initializing Blade: LightsaberBlade" + id);

            lightsaberBlade = item?.GetCustomReference("LightsaberBlade" + id);
            if (lightsaberBlade == null)
            {
                DebugService.LogError($"LightsaberBlade{id} reference not found in item or item is null.");
                return;
            }

            GameObject parent = item.gameObject;
            m_item = item;
            i_id = id;

            module = m_item.data.GetModule<ModuleLightsaber>();

            lightsaberBlade.gameObject.SetActive(true);
            GetLightsaberBladeData();
            lightsaberBlade.gameObject.SetActive(false);

            m_toggle_src = item?.gameObject.GetNamedChild("ToggleSound").GetComponent<AudioSource>();
            if (m_toggle_src == null) DebugService.LogWarning($"Toggle source not found");
            else m_toggle_src.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            InitializeProperties();

            if (kyberCrystalsOverride != null && !kyberCrystalsOverride.IsNullOrEmpty())
            {
                s_kyber_crystal_id = kyberCrystalsOverride[id];
                DebugService.LogWarning("Loading Kyber Crystal from saved data: " + s_kyber_crystal_id);

                if (string.IsNullOrEmpty(s_kyber_crystal_id))
                {
                    s_kyber_crystal_id = "";
                }
                else
                {
                    var kyberCrystalData = Catalog.GetData<ItemData>(s_kyber_crystal_id, true);
                    if (kyberCrystalData == null) return;
                    kyberCrystalData.SpawnAsync((Item crystal) =>
                    {
                        SetCrystal(crystal.GetComponent<ItemKyberCrystal>());
                    });
                }
            }
            else if (!string.IsNullOrEmpty(s_kyber_crystal_id))
            {
                SetCrystal();
            }

            if (m_lightTop) m_lightTop.enabled = f_max_length > 0.25f;

            TurnOff(false);
            SetActive(false);

            DebugService.LogInfo("Blade Initialized: LightsaberBlade" + id);
        }

        public void UpdateSize()
        {
            if (ShouldUpdateLength())
            {
                UpdateExtend();

                f_current_length = Mathf.Clamp(f_current_length + (f_extend * Time.deltaTime), 0f, f_max_length);

                if (lightsaberBlade != null)
                {
                    float newWidth = f_old_width * f_blade_width_multiplier * ModSettings.fGlobalLightsaberWidthMultiplier;
                    lightsaberBlade.localScale = new Vector3(newWidth, newWidth, f_current_length); 
                }
            }
        }

        public void TurnOn(bool sound)
        {
            if (string.IsNullOrEmpty(s_kyber_crystal_id))
                return;

            b_is_active = true;

            m_toggle_src.volume = f_on_volume * ModSettings.fLightsaberToggleVolumeMult;
            var percentage = f_on_pitch / 12.5f;
            m_toggle_src.pitch = f_on_pitch + UnityEngine.Random.Range(-percentage, percentage);


            if (sound)
            {
                m_toggle_src.Stop();
                Util.PlaySound(m_toggle_src, m_on_sound, f_on_volume * ModSettings.fLightsaberToggleVolumeMult);
            }

            SetActive(true);

            int mask = (1 << 13) | (1 << 26) | (1 << 27);
            if (Physics.Raycast(m_bottom.transform.position, lightsaberBlade.forward, f_saber_distance, mask, QueryTriggerInteraction.Ignore))
            {
                m_item.physicBody.rigidBody.AddForce(-lightsaberBlade.forward * 20f, ForceMode.Impulse);
            }

            if (!m_idle_src.isPlaying)
            {
                m_idle_src.volume = f_idle_volume * ModSettings.fLightsaberHumVolumeMult;
                m_idle_src.pitch = f_idle_pitch;

                Util.PlaySoundLooped(m_idle_src, m_idle_sound, m_idle_src.volume);
            }
        }

        public void TurnOff(bool sound)
        {
            m_on_disabled_blade?.Invoke();

            b_is_active = false;

            m_toggle_src.volume = f_off_volume * ModSettings.fLightsaberToggleVolumeMult;
            var percentage = f_off_pitch / 12.5f;
            m_toggle_src.pitch = f_off_pitch + UnityEngine.Random.Range(-percentage, percentage);

            if (sound)
            {
                m_toggle_src.Stop();
                Util.PlaySound(m_toggle_src, m_off_sound, f_off_volume * ModSettings.fLightsaberToggleVolumeMult);
            }
        }

        public void SetCrystal()
        {
            var kyberCrystalData = Catalog.GetData<ItemData>(s_kyber_crystal_id, true);
            if (kyberCrystalData == null) return;
            kyberCrystalData.SpawnAsync((Item crystal) =>
            {
                SetCrystal(crystal.GetComponent<ItemKyberCrystal>());
            });
        }

        public void SetCrystal(ItemKyberCrystal kyberCrystal)
        {
            if (kyberCrystal == null)
            {
                RemoveCrystal();
                return;
            }

            DebugService.LogInfo("Applying Kyber Crystal: " + kyberCrystal.item.data.id);

            if (lightsaberBlade == null)
            {
                return;
            }

            MaterialPropertyBlock coreBlock = new MaterialPropertyBlock();
            MaterialPropertyBlock glowBlock = new MaterialPropertyBlock();

            if (m_core_renderer != null)
            {
                m_core_renderer.GetPropertyBlock(coreBlock);

                var coreColor = Global.globalUsePP ? kyberCrystal.coreColorGO : kyberCrystal.coreColor;

                coreBlock.SetColor("_CoreColor", coreColor);
                coreBlock.SetColor("_GlowColor", kyberCrystal.glowColorGO);
                coreBlock.SetFloat("_LookMultiplier", kyberCrystal.module.lookMultiplier);

                coreBlock.SetFloat("_UseGradient", kyberCrystal.module.useGradient? 1f : 0f);

                if (kyberCrystal.module.useGradient)
                {
                    coreBlock.SetTexture("_GradientTex", kyberCrystal.gradient);
                    coreBlock.SetFloat("_GradientTilingY", kyberCrystal.module.gradientTilingGO);
                    coreBlock.SetFloat("_GradientIntensity", kyberCrystal.module.gradientIntensityGO);
                    coreBlock.SetFloat("_GradientSpeed", kyberCrystal.module.gradientSpeedGO);
                }
                

                coreBlock.SetFloat("_IntensePosition", kyberCrystal.module.intensePosition);
                coreBlock.SetFloat("_IntenseIntensity", kyberCrystal.module.intenseIntensity);
                coreBlock.SetFloat("_IntenseConvergence", kyberCrystal.module.intenseConvergence);
                coreBlock.SetFloat("_IntenseFallOff", kyberCrystal.module.intenseFalloff);

                coreBlock.SetFloat("_Flicker", kyberCrystal.module.flickerEnabled ? 1f : 0f);
                coreBlock.SetFloat("_FlickerSpeed", kyberCrystal.module.flickerSpeed);

                if (kyberCrystal.module.flickerRange.Length >= 2) 
                    coreBlock.SetVector("_FlickerRange", new Vector2(kyberCrystal.module.flickerRange[0], kyberCrystal.module.flickerRange[1]));

                coreBlock.SetFloat("_UseCorrupted", kyberCrystal.module.isCorrupted ? 1f : 0f);

                //coreBlock.SetFloat("_GO_ENABLED", Global.globalUsePP? 1f : 0f);

                m_core_renderer.SetPropertyBlock(coreBlock);

                if (m_trail_renderer)
                {
                    if ((kyberCrystal.coreColor == Color.black && !Global.globalUsePP )|| (kyberCrystal.coreColorGO == Color.black && Global.globalUsePP))
                    {
                        coreBlock.SetColor("_CoreColor", Global.globalUsePP ? kyberCrystal.glowColorGO : kyberCrystal.glowColor);
                    }
                    coreBlock.SetFloat("_Cull", 2);

                    m_trail_renderer.SetPropertyBlock(coreBlock);
                }
            }

            if (m_glow_renderer)
            {
                m_glow_renderer.enabled = !Global.globalUsePP || kyberCrystal.module.useSmoke;
                m_glow_renderer.GetPropertyBlock(glowBlock);

                glowBlock.SetColor("_Color", kyberCrystal.glowColor);
                glowBlock.SetFloat("_JitterAmount", kyberCrystal.module.jitterAmount);
                glowBlock.SetInt("_IsCorrupted", 1);
                glowBlock.SetFloat("_LineWidth", ModSettings.fSaberGlowWidthMultiplier);

                glowBlock.SetColor("_AltColor", kyberCrystal.altGlowColor);
                glowBlock.SetFloat("_FadeSpeed", kyberCrystal.module.fadeSpeed);

                glowBlock.SetFloat("_UseGradient", kyberCrystal.module.glowMode);

                if (kyberCrystal.module.glowMode > 0.5f)
                {
                    glowBlock.SetFloat("_ScrollSpeed", kyberCrystal.module.scrollSpeed);
                    glowBlock.SetTexture("_GradientTex", kyberCrystal.gradient);
                }

                glowBlock.SetInt("_EnableSmoke", kyberCrystal.module.useSmoke ? 1 : 0);
                glowBlock.SetVector("_GradientTiling", new Vector2(1, kyberCrystal.module.smokeTiling * f_current_length));
                glowBlock.SetFloat("_SmokeScrollSpeed", kyberCrystal.module.smokeScrollSpeed);
                glowBlock.SetColor("_SmokeColor", kyberCrystal.smokeColor);

                m_glow_renderer.SetPropertyBlock(glowBlock);
            }

            if (m_light != null)
            {
                m_light.color = kyberCrystal.lightColor;
                m_light.intensity = kyberCrystal.module.lightIntensity * ModSettings.fSaberLightIntensityMultiplier;
                m_light.range = kyberCrystal.module.lightRange * ModSettings.fSaberLightRangeMultiplier;

                m_light.enabled = ModSettings.bDualLights;
            }

            if (m_lightTop != null)
            {
                m_lightTop.color = kyberCrystal.lightColor;
                m_lightTop.intensity = kyberCrystal.module.lightIntensity * ModSettings.fSaberLightIntensityMultiplier;
                m_lightTop.range = kyberCrystal.module.lightRange * ModSettings.fSaberLightRangeMultiplier;

                if (!ModSettings.bDualLights)
                    m_lightTop.transform.localPosition = new Vector3(0, 0, f_saber_distance * 0.5f);
                else
                    m_lightTop.transform.localPosition = m_tip.transform.localPosition;
            }

            m_accent_effect = kyberCrystal.module.accentSwing;
            m_smoothswing_effect_low = kyberCrystal.module.smoothSwingLow;
            m_smoothswing_effect_high = kyberCrystal.module.smoothSwingHigh;

            if (m_idle_src)
            {
                m_idle_sound = kyberCrystal.module.idleContainer;
                f_idle_volume = kyberCrystal.module.idleVolume;
                f_idle_pitch = kyberCrystal.module.idlePitch;
            }

            if (m_toggle_src)
            {
                m_on_sound = kyberCrystal.module.actiavtionContainer;
                f_on_volume = kyberCrystal.module.activationVolume;
                f_on_pitch = kyberCrystal.module.activationPitch;

                m_off_sound = kyberCrystal.module.deactivationContainer;
                f_off_volume = kyberCrystal.module.deactivationVolume;
                f_off_pitch = kyberCrystal.module.deactivationPitch;
            }

            s_kyber_crystal_id = kyberCrystal.item.data.id;

            m_crystal_spawned?.Invoke();

            DespawnKyberCrystal(kyberCrystal);
        }

        public void RemoveCrystal()
        {
            DebugService.LogInfo("Removing Kyber Crystal: " + s_kyber_crystal_id);

            if (string.IsNullOrEmpty(s_kyber_crystal_id))
                return;

            SpawnKyberCrystal(s_kyber_crystal_id, m_eject_point);

            s_kyber_crystal_id = "";
        }


        // Helpers - Components

        public void UpdateBladeDimensions()
        {
            float newWidth = f_old_width * f_blade_width_multiplier * ModSettings.fGlobalLightsaberWidthMultiplier;
            f_max_length = f_old_length * f_blade_length_multiplier * ModSettings.fGlobalLightsaberLenghtMultiplier;
            lightsaberBlade.transform.localScale = new Vector3(newWidth, newWidth, f_current_length);
            UpdateDamagerScales();
            m_item.GetComponent<ItemLightsaber>().UpdateSaveData();
        }

        public void AddBladeLength(float add)
        {
            SetBladeLength(f_max_length + add);
        }

        public void ResetBladeLength()
        {
            SetBladeLength(f_old_length);
        }

        public void SetBladeLength(float bladeLength)
        {
            f_max_length = bladeLength;
            f_saber_distance = Vector3.Distance(m_tip.transform.position, m_bottom.transform.position);
            UpdateDamagerScales();

            if (!isGradient || !isSmoke) return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            MaterialPropertyBlock block2 = new MaterialPropertyBlock();
            if (m_glow_renderer != null)
            {
                m_glow_renderer.GetPropertyBlock(block);
                block.SetVector("_GradientTiling", new Vector2(1, gradientTiling * f_current_length));
                block.SetVector("_SmokeTiling", new Vector2(1, smokeTiling * f_current_length));
                m_glow_renderer.SetPropertyBlock(block);
            }

            if (m_light)
                m_light.enabled = f_max_length > 0.25f;
        }

        public void UpdateDamagerScales()
        {
            float ratio = f_max_length / f_old_length;
            if (slashDamage != null)
            {
                slashDamage.penetrationLength = originalSlashLength * ratio;
            }
            if (pierceDamage != null)
            {
                pierceDamage.penetrationDepth = originalPierceDepth * ratio;
            }
        }

        private void InitializeProperties()
        {
            m_trail.m_tip = m_tip.transform.localPosition;
            m_trail.m_bottom = m_bottom.transform.localPosition;

            f_original_whoosh_max = m_whoosh_point.maxVelocity;
            f_original_whoosh_min = m_whoosh_point.minVelocity;

            f_max_length = lightsaberBlade.transform.localScale.z * f_blade_length_multiplier * ModSettings.fGlobalLightsaberLenghtMultiplier;
            f_extend = f_max_length / ModSettings.fLightsaberIgniteSpeed;

            f_old_length = f_max_length;
            f_old_width = lightsaberBlade.transform.localScale.x; // both axis (x,y) should be the same

            UpdateDamagerScales();

            f_saber_distance = Vector3.Distance(m_tip.transform.position, m_bottom.transform.position);

            float newWidth = f_old_width * f_blade_width_multiplier * ModSettings.fGlobalLightsaberWidthMultiplier;
            lightsaberBlade.transform.localScale = new Vector3(newWidth, newWidth, 0f);

            f_current_length = 0f;
        }

        public void UpdatedIgnite()
        {
            f_extend = f_max_length / ModSettings.fLightsaberIgniteSpeed;
        }

        public void SetActive(bool state)
        {
            if (m_whoosh_point)
            {
                if (ModSettings.bAccentSwings)
                {
                    m_whoosh_point.maxVelocity = float.MaxValue;
                    m_whoosh_point.minVelocity = float.MaxValue;
                }
                else
                {
                    m_whoosh_point.maxVelocity = state ? f_original_whoosh_max : float.MaxValue;
                    m_whoosh_point.minVelocity = state ? f_original_whoosh_min : float.MaxValue;
                }
            }

            if (lightsaberBlade)
            {
                lightsaberBlade.gameObject.SetActive(state);
            }

            if (m_trail)
            {
                m_trail.enabled = state;
                m_trail_renderer.enabled = state;
            }
        }


        // Helpers - Blade Length

        public void UpdateExtend()
        {
            f_extend = UpdateExtendDirection(f_extend);
        }

        private float UpdateExtendDirection(float extend)
        {
            return (!b_is_active) ? -Mathf.Abs(extend) : Mathf.Abs(extend);
        }

        private bool ShouldUpdateLength()
        {
            bool isExtending = f_current_length != f_max_length;
            bool isRetracting = f_current_length > 0f;

            return b_is_active ? isExtending : isRetracting;
        }

        // Helpers - Kyber Crystal

        public void SpawnKyberCrystal(string id, GameObject spawnPoint)
        {
            ItemData kyberData = Catalog.GetData<ItemData>(id);
            if (kyberData == null)
                return;
            kyberData.SpawnAsync(item => { item.SetOwner(Item.Owner.Player); }, spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        public void DespawnKyberCrystal(ItemKyberCrystal kyberCrystal)
        {

            Item despawn = kyberCrystal.GetComponent<Item>();
            if (despawn.IsHeld())
                despawn.mainHandler?.TryRelease();
            despawn.Despawn();
        }
    }
}